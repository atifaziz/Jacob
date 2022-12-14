// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Jacob;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using Unit = System.ValueTuple;

#pragma warning disable CA1716 // Identifiers should not match keywords

public interface IJsonReadResult<out T>
{
    string? Error { get; }
    T Value { get; }
}

public static class JsonReadResult
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonReadResult<T> Value<T>(T value) => new(value, null);

    public static bool IsIncomplete<T>(this IJsonReadResult<T> result) =>
        result is null
            ? throw new ArgumentNullException(nameof(result))
            : ReferenceEquals(result.Error, IncompleteJsonReadError.Value);
}

public static class IncompleteJsonReadError
{
    public static readonly string Value = "(incomplete)";
}

public record struct JsonReadError(string Message)
{
    public static readonly JsonReadError Incomplete = new(IncompleteJsonReadError.Value);

    public override string ToString() => Message;
}

public record struct JsonReadResult<T>(T Value, string? Error) : IJsonReadResult<T>
{
    public bool Incomplete => ReferenceEquals(Error, IncompleteJsonReadError.Value);

    public override string ToString() =>
        Error is { } someError ? $"Error: {someError}" : $"Value: {Value}";

#pragma warning disable CA2225 // Operator overloads have named alternates
    public static implicit operator JsonReadResult<T>(JsonReadError error) => new(default!, error.Message);
#pragma warning restore CA2225 // Operator overloads have named alternates
}

public interface IJsonReader<out T, out TReadResult>
    where TReadResult : IJsonReadResult<T>
{
    TReadResult TryRead(ref Utf8JsonReader reader);
}

public interface IJsonReader<T> : IJsonReader<T, JsonReadResult<T>> { }

public interface IJsonProperty<out T, out TReadResult>
    where TReadResult : IJsonReadResult<T>
{
    bool IsMatch(ref Utf8JsonReader reader);
    IJsonReader<T, TReadResult> Reader { get; }
    bool HasDefaultValue { get; }
    T DefaultValue { get; }
}

public interface IJsonProperty<T> : IJsonProperty<T, JsonReadResult<T>> { }

#pragma warning disable CA1720 // Identifier contains type name (by design)

public static partial class JsonReader
{
    public static T Read<T>(this IJsonReader<T> reader, string json) =>
        reader.Read(Encoding.UTF8.GetBytes(json));

    public static T Read<T>(this IJsonReader<T> reader, ReadOnlySpan<byte> utf8JsonTextBytes)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        var rdr = new Utf8JsonReader(utf8JsonTextBytes);
        return reader.Read(ref rdr);
    }

    public static JsonReadResult<T> TryRead<T>(this IJsonReader<T> reader, ref Utf8JsonReader utf8Reader) =>
        reader?.TryRead(ref utf8Reader) ?? throw new ArgumentNullException(nameof(reader));

    public static T Read<T>(this IJsonReader<T> reader, ref Utf8JsonReader utf8Reader)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));

        return reader.TryRead(ref utf8Reader) switch
        {
            { Error: { } message } => throw new JsonException($@"{message} See token ""{utf8Reader.TokenType}"" at offset {utf8Reader.TokenStartIndex}."),
            { Value: var value } => value,
        };
    }

    public static JsonReadResult<T> TryRead<T>(this IJsonReader<T> reader, string json) =>
        reader.TryRead(Encoding.UTF8.GetBytes(json));

    public static JsonReadResult<T> TryRead<T>(this IJsonReader<T> reader, ReadOnlySpan<byte> utf8JsonTextBytes)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        var rdr = new Utf8JsonReader(utf8JsonTextBytes);
        return reader.TryRead(ref rdr);
    }

    public static IAsyncEnumerator<T> GetAsyncEnumerator<T>(this IJsonReader<T> reader,
                                                            Stream stream, int initialBufferSize) =>
        GetAsyncEnumerator(reader, stream, initialBufferSize, CancellationToken.None);

    public static IAsyncEnumerator<T> GetAsyncEnumerator<T>(this IJsonReader<T> reader,
                                                            Stream stream, int initialBufferSize,
                                                            CancellationToken cancellationToken)
    {
        if (reader is null) throw new ArgumentNullException(nameof(reader));
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (initialBufferSize < 0) throw new ArgumentOutOfRangeException(nameof(initialBufferSize), initialBufferSize, null);

        return GetAsyncEnumeratorCore(reader, stream, initialBufferSize, cancellationToken);
    }

    static async IAsyncEnumerator<T> GetAsyncEnumeratorCore<T>(IJsonReader<T> reader,
                                                               Stream stream, int initialBufferSize,
                                                               CancellationToken cancellationToken)
    {
        using var scr = new StreamChunkReader(stream, initialBufferSize);

        var state = new JsonReaderState();
        var ar = new ArrayReadStateMachine();
        ArrayReadStateMachine.ReadResult readResult;

        do
        {
            var readTask = scr.ReadAsync(cancellationToken);
            if (!readTask.IsCompleted)
                await readTask.AsTask().ConfigureAwait(false);

            while (true)
            {
                var read = TryReadItem(scr.RemainingChunkSpan, out var bytesConsumed, out readResult, out var item);
                scr.ConsumeChunkBy(bytesConsumed);
                if (!read)
                    break;
                cancellationToken.ThrowIfCancellationRequested();
                yield return item!;
            }
        }
        while (readResult is not ArrayReadStateMachine.ReadResult.Done);

        bool TryReadItem(ReadOnlySpan<byte> span,
                         out int bytesConsumed,
                         out ArrayReadStateMachine.ReadResult readResult,
                         [NotNullWhen(true)] out T? item)
        {
            var rdr = new Utf8JsonReader(span, scr.Eof, state);
            while (true)
            {
                switch (ar.Read(ref rdr))
                {
                    case ArrayReadStateMachine.ReadResult.Error:
                    {
                        throw new JsonException("Invalid JSON value where a JSON array was expected.");
                    }
                    case ArrayReadStateMachine.ReadResult.Item:
                    {
                        if (ar.CurrentItemLoopCount is 0)
                        {
                            var read = rdr.Read();
                            Debug.Assert(read);
                        }

                        switch (reader.TryRead(ref rdr))
                        {
                            case var r when r.IsIncomplete():
                                break;
                            case { Error: { } error }:
                                throw new JsonException(error);
                            case { Value: { } value }:
                                ar.OnItemRead();
                                item = value;
                                readResult = ArrayReadStateMachine.ReadResult.Item;
                                goto exit;
                        }
                        goto case ArrayReadStateMachine.ReadResult.Incomplete;
                    }
                    case ArrayReadStateMachine.ReadResult.Done:
                    {
                        item = default;
                        readResult = ArrayReadStateMachine.ReadResult.Done;
                        goto exit;
                    }
                    case ArrayReadStateMachine.ReadResult.Incomplete:
                    {
                        item = default;
                        readResult = ArrayReadStateMachine.ReadResult.Incomplete;
                        goto exit;
                    }
                }
            }

            exit:
            bytesConsumed = (int)rdr.BytesConsumed;
            state = rdr.CurrentState;
            return readResult is ArrayReadStateMachine.ReadResult.Item;
        }
    }

    static IJsonReader<JsonElement>? jsonElementReader;

    public static IJsonReader<JsonElement> Element() =>
        jsonElementReader ??=
            Create(static (ref Utf8JsonReader rdr) =>
            {
                ref var inner = ref Utf8JsonReader.GetInnerReader(ref rdr);

#if NET6_0_OR_GREATER
                return JsonElement.TryParseValue(ref inner, out var element)
                     ? JsonReadResult.Value(element.Value)
                     : JsonReadError.Incomplete;
#elif NETCOREAPP3_1_OR_GREATER
                if (!JsonDocument.TryParseValue(ref inner, out var doc))
                    return JsonReadError.Incomplete;

                using var _ = doc;
                return JsonReadResult.Value(doc.RootElement.Clone());
#else
#error Unsupported platform.
#endif
            });


    public static IJsonReader<T> Error<T>(string message) =>
        CreatePure<T>((ref Utf8JsonReader _) => Error(message));

    static IJsonReader<string>? stringReader;

    public static IJsonReader<string> String() =>
        stringReader ??=
            Create(static (ref Utf8JsonReader rdr) =>
                rdr.TokenType is JsonTokenType.String
                ? Value(rdr.GetString()!)
                : Error("Invalid JSON value where a JSON string was expected."));

    static IJsonReader<Guid>? guidReader;

    public static IJsonReader<Guid> Guid() =>
        guidReader ??=
            Create(static (ref Utf8JsonReader rdr) =>
                rdr.TokenType is JsonTokenType.String && rdr.TryGetGuid(out var value)
                ? Value(value)
                : Error("Invalid JSON value where a Guid was expected in the 'D' format (hyphen-separated)."));

    static IJsonReader<bool>? booleanReader;

    public static IJsonReader<bool> Boolean() =>
        booleanReader ??=
            Create(static (ref Utf8JsonReader rdr) =>
                rdr.TokenType switch
                {
                    JsonTokenType.True => Value(true),
                    JsonTokenType.False => Value(false),
                    _ => Error("Invalid JSON value where a JSON Boolean was expected.")
                });

    static IJsonReader<DateTime>? dateTimeReader;

    public static IJsonReader<DateTime> DateTime() =>
        dateTimeReader ??=
            Create(static (ref Utf8JsonReader rdr) =>
                rdr.TokenType is JsonTokenType.String && rdr.TryGetDateTime(out var value)
                ? Value(value)
                : Error("JSON value cannot be interpreted as a date and time in ISO 8601-1 extended format."));

    public static IJsonReader<DateTime> DateTime(string format, IFormatProvider? provider) =>
        DateTime(format, provider, DateTimeStyles.None);

    public static IJsonReader<DateTime> DateTime(string format, IFormatProvider? provider, DateTimeStyles styles) =>
        String().TryMap(s => System.DateTime.TryParseExact(s, format, provider, styles, out var value)
                             ? Value(value)
                             : Error("JSON value cannot interpreted as date/time."));

    static IJsonReader<DateTimeOffset>? dateTimeOffsetReader;

    public static IJsonReader<DateTimeOffset> DateTimeOffset() =>
        dateTimeOffsetReader ??=
            Create(static (ref Utf8JsonReader rdr) =>
                rdr.TokenType is JsonTokenType.String && rdr.TryGetDateTimeOffset(out var value)
                ? Value(value)
                : Error("JSON value cannot be interpreted as a date and time offset in ISO 8601-1 extended format."));

    public static IJsonReader<T> Null<T>(T @null) =>
        Create((ref Utf8JsonReader rdr) =>
            rdr.TokenType is JsonTokenType.Null
            ? Value(@null)
            : Error("Invalid JSON value where a JSON null was expected."));

    static bool IsEnumDefined<T>(T value) where T : struct, Enum =>
#if NET5_0_OR_GREATER
        Enum.IsDefined(value);
#else
        Enum.IsDefined(typeof(T), value);
#endif

    static bool TryParseEnum<T>(string input, bool ignoreCase, out T result) where T : struct, Enum
    {
#if NET5_0_OR_GREATER
        return Enum.TryParse(input, ignoreCase, out result);
#else
        if (Enum.TryParse(typeof(T), input, ignoreCase, out var obj))
        {
            result = (T)obj!;
            return true;
        }
        else
        {
            result = default;
            return false;
        }
#endif
    }

    public static IJsonReader<T> AsEnum<T>(this IJsonReader<string> reader)
        where T : struct, Enum =>
        AsEnum<T>(reader, ignoreCase: false);

    public static IJsonReader<T> AsEnum<T>(this IJsonReader<string> reader, bool ignoreCase)
        where T : struct, Enum =>
        reader.TryMap(s => TryParseEnum(s, ignoreCase, out T value) ? Value(value) : Error($"Invalid member for {typeof(T)}."));

    public static IJsonReader<TEnum> AsEnum<TSource, TEnum>(this IJsonReader<TSource> reader, Func<TSource, TEnum> selector)
        where TEnum : struct, Enum =>
        reader.Select(selector).Validate($"Invalid member for {typeof(TEnum)}.", IsEnumDefined);

    public static IJsonReader<T?> OrNull<T>(this IJsonReader<T> reader, T? @null = default)
        where T : struct =>
        Null(@null).Or(from v in reader select (T?)v);

    public static IJsonReader<T?> OrNull<T>(this IJsonReader<T> reader, T? @null = default)
        where T : class =>
        Null(@null).Or(from v in reader select (T?)v);

    public static IJsonReader<T> Validate<T>(this IJsonReader<T> reader, Func<T, bool> predicate) =>
        reader.Validate(errorMessage: null, predicate);

    public static IJsonReader<T> Validate<T>(this IJsonReader<T> reader, string? errorMessage, Func<T, bool> predicate) =>
        reader.TryMap(v => predicate(v) ? Value(v) : Error(errorMessage ?? "Invalid JSON value."));

    public static IJsonReader<object> AsObject<T>(this IJsonReader<T> reader)
        where T : notnull =>
        from v in reader select (object)v;

    public static IJsonReader<TResult> Let<T, TResult>(this IJsonReader<T> reader,
                                                       Func<IJsonReader<T>, IJsonReader<TResult>> selector)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        if (selector == null) throw new ArgumentNullException(nameof(selector));

        return selector(reader);
    }

    public static IJsonReader<T> Or<T>(this IJsonReader<T> reader1, IJsonReader<T> reader2) =>
        Either(reader1, reader2, null);

    public static IJsonReader<T> Or<T>(this IJsonReader<T> reader1, IJsonReader<T> reader2,
                                       string? errorMessage) =>
        Either(reader1, reader2, errorMessage);

    public static IJsonReader<T> Either<T>(IJsonReader<T> reader1, IJsonReader<T> reader2) =>
        Either(reader1, reader2, null);

    public static IJsonReader<T> Either<T>(IJsonReader<T> reader1, IJsonReader<T> reader2,
                                           string? errorMessage) =>
        Create((ref Utf8JsonReader rdr) =>
        {
            var irdr = rdr;
            switch (reader1.TryRead(ref irdr))
            {
                case { Incomplete: true }:
                    throw PartialJsonNotSupportedException();
                case { Error: not null }:
                    return reader2.TryRead(ref rdr) switch
                    {
                        { Incomplete: true } => throw PartialJsonNotSupportedException(),
                        { Error: not null } => Error(errorMessage ?? "Invalid JSON value."),
                        var some => some
                    };
                case var some:
                    rdr = irdr;
                    return some;
            }
        });

    static readonly object BufferFrame = new();

    public static IJsonReader<T> Buffer<T>(this IJsonReader<T> reader) =>
        Create((ref Utf8JsonReader rdr) =>
        {
            if (rdr.IsResuming)
            {
                var frame = rdr.Pop();
                Debug.Assert(frame == BufferFrame);
            }

            switch (rdr.TokenType)
            {
                case JsonTokenType.Null or JsonTokenType.False or JsonTokenType.True
                    or JsonTokenType.Number or JsonTokenType.String:
                {
                    return reader.TryRead(ref rdr);
                }
                case JsonTokenType.StartObject or JsonTokenType.StartArray:
                {
                    var depth = rdr.CurrentDepth;
                    var bookmark = rdr;
                    if (!rdr.Read())
                    {
                        rdr = bookmark;
                        return rdr.Suspend(BufferFrame);
                    }

                    bool read;
                    do
                    {
                        read = rdr.Read();
                    }
                    while (read && depth < rdr.CurrentDepth);
                    rdr = bookmark;
                    return read ? reader.TryRead(ref rdr) : rdr.Suspend(BufferFrame);
                }
                case var tokenType:
                    throw new
                        InvalidOperationException($"Operation is not valid when reader is on token: {tokenType}");
            }
        });

    [DebuggerDisplay("{" + nameof(name) + "}")]
    sealed class JsonProperty<T> : IJsonProperty<T>
    {
        readonly string name;

        public JsonProperty(string name, IJsonReader<T> reader, (bool, T) @default = default) =>
            (this.name, Reader, (HasDefaultValue, DefaultValue)) = (name, reader, @default);

        public bool IsMatch(ref Utf8JsonReader reader) =>
            reader.TokenType != JsonTokenType.PropertyName
                ? throw new ArgumentException(null, nameof(reader))
                : reader.ValueTextEquals(this.name);

        public IJsonReader<T, JsonReadResult<T>> Reader { get; }
        public bool HasDefaultValue { get; }
        public T DefaultValue { get; }
    }

    public static IJsonProperty<T> Property<T>(string name, IJsonReader<T> reader, (bool, T) @default = default) =>
        new JsonProperty<T>(name, reader, @default);

    sealed class NonProperty : IJsonProperty<Unit, JsonReadResult<Unit>>
    {
        public static readonly NonProperty Instance = new();

        NonProperty() { }

        public bool IsMatch(ref Utf8JsonReader reader) => false;
        public IJsonReader<Unit, JsonReadResult<Unit>> Reader => throw new NotSupportedException();
        public bool HasDefaultValue => true;
        public Unit DefaultValue => default;
    }

    public static IJsonReader<TResult> Object<T, TResult>(IJsonReader<T> reader,
                                                          Func<List<KeyValuePair<string, T>>, TResult> resultSelector) =>
        CreatePure((ref Utf8JsonReader rdr) =>
        {
            if (rdr.TokenType is JsonTokenType.None && !rdr.Read())
                throw PartialJsonNotSupportedException();

            var tokenType = rdr.TokenType;
            if (tokenType is not JsonTokenType.StartObject)
                return Error("Invalid JSON value where a JSON object was expected.");

            var properties = new List<KeyValuePair<string, T>>();

            while (true)
            {
                if (!rdr.TryReadToken(out tokenType))
                    throw PartialJsonNotSupportedException();

                if (tokenType is JsonTokenType.EndObject)
                    break;

                Debug.Assert(rdr.TokenType is JsonTokenType.PropertyName);
                var name = rdr.GetString()!;

                if (!rdr.Read())
                    throw PartialJsonNotSupportedException();

                switch (reader.TryRead(ref rdr))
                {
                    case { Incomplete: true }:
                        throw PartialJsonNotSupportedException();
                    case { Error: { } error }:
                        return Error(error);
                    case { Value: var value }:
                        properties.Add(KeyValuePair.Create(name, value));
                        break;
                }
            }

            return Value(resultSelector(properties));
        });

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// the reader to return an error result.
    /// </remarks>
    public static IJsonReader<T> Object<T>(IJsonProperty<T, JsonReadResult<T>> property) =>
        Object(property, NonProperty.Instance, NonProperty.Instance,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               NonProperty.Instance,
               (v, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _) => v);

    struct Values<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
    {
        public (bool, T1) P1;
        public (bool, T2) P2;
        public (bool, T3) P3;
        public (bool, T4) P4;
        public (bool, T5) P5;
        public (bool, T6) P6;
        public (bool, T7) P7;
        public (bool, T8) P8;
        public (bool, T9) P9;
        public (bool, T10) P10;
        public (bool, T11) P11;
        public (bool, T12) P12;
        public (bool, T13) P13;
        public (bool, T14) P14;
        public (bool, T15) P15;
        public (bool, T16) P16;

        public int? NextPropertyIndex;
    };

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// the reader to return an error result.
    /// </remarks>
    public static IJsonReader<TResult>
        Object<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(
            IJsonProperty<T1, JsonReadResult<T1>> property1, IJsonProperty<T2, JsonReadResult<T2>> property2,
            IJsonProperty<T3, JsonReadResult<T3>> property3, IJsonProperty<T4, JsonReadResult<T4>> property4,
            IJsonProperty<T5, JsonReadResult<T5>> property5, IJsonProperty<T6, JsonReadResult<T6>> property6,
            IJsonProperty<T7, JsonReadResult<T7>> property7, IJsonProperty<T8, JsonReadResult<T8>> property8,
            IJsonProperty<T9, JsonReadResult<T9>> property9, IJsonProperty<T10, JsonReadResult<T10>> property10,
            IJsonProperty<T11, JsonReadResult<T11>> property11, IJsonProperty<T12, JsonReadResult<T12>> property12,
            IJsonProperty<T13, JsonReadResult<T13>> property13, IJsonProperty<T14, JsonReadResult<T14>> property14,
            IJsonProperty<T15, JsonReadResult<T15>> property15, IJsonProperty<T16, JsonReadResult<T16>> property16,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> projector) =>
        Create((ref Utf8JsonReader reader) =>
        {
            var (sm, values) =
                reader.IsResuming && ((ObjectReadStateMachine, Values<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>))reader.Pop() is var ps
                    ? ps
                    : default;

            return Read(ref reader, sm, ref values);

            JsonReadResult<TResult> Read(ref Utf8JsonReader reader, ObjectReadStateMachine sm, ref Values<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> values)
            {
                while (true)
                {
                    switch (sm.Read(ref reader))
                    {
                        case ObjectReadStateMachine.ReadResult.Error:
                            return Error("Invalid JSON value where a JSON object was expected.");

                        case ObjectReadStateMachine.ReadResult.Incomplete:
                            return reader.Suspend((sm, values));

                        case ObjectReadStateMachine.ReadResult.Done:
                            static void DefaultUnassigned<T>(IJsonProperty<T, JsonReadResult<T>> property, ref (bool, T) v)
                            {
                                if (v is (false, _) && property.HasDefaultValue)
                                    v = (true, property.DefaultValue);
                            }

                            DefaultUnassigned(property1, ref values.P1);
                            DefaultUnassigned(property2, ref values.P2);
                            DefaultUnassigned(property3, ref values.P3);
                            DefaultUnassigned(property4, ref values.P4);
                            DefaultUnassigned(property5, ref values.P5);
                            DefaultUnassigned(property6, ref values.P6);
                            DefaultUnassigned(property7, ref values.P7);
                            DefaultUnassigned(property8, ref values.P8);
                            DefaultUnassigned(property9, ref values.P9);
                            DefaultUnassigned(property10, ref values.P10);
                            DefaultUnassigned(property11, ref values.P11);
                            DefaultUnassigned(property12, ref values.P12);
                            DefaultUnassigned(property13, ref values.P13);
                            DefaultUnassigned(property14, ref values.P14);
                            DefaultUnassigned(property15, ref values.P15);
                            DefaultUnassigned(property16, ref values.P16);

                            return (values.P1, values.P2, values.P3,
                                    values.P4, values.P5, values.P6,
                                    values.P7, values.P8, values.P9,
                                    values.P10, values.P11, values.P12,
                                    values.P13, values.P14, values.P15,
                                    values.P16) is ((true, var v1), (true, var v2), (true, var v3),
                                                    (true, var v4), (true, var v5), (true, var v6),
                                                    (true, var v7), (true, var v8), (true, var v9),
                                                    (true, var v10), (true, var v11), (true, var v12),
                                                    (true, var v13), (true, var v14), (true, var v15),
                                                    (true, var v16))
                                 ? Value(projector(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16))
                                 : Error("Invalid JSON object.");

                        case ObjectReadStateMachine.ReadResult.PropertyName:
                            static bool TrySetPropertyIndex<TValue>(int index,
                                                                    IJsonProperty<TValue, JsonReadResult<TValue>> property,
                                                                    ref Utf8JsonReader reader,
                                                                    ref Values<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> values)
                            {
                                if (!property.IsMatch(ref reader))
                                    return false;

                                values.NextPropertyIndex = index;
                                return true;
                            }

                            if (sm.CurrentPropertyLoopCount is 0)
                            {
                                var read = reader.Read();
                                Debug.Assert(read);
                            }

                                 if (TrySetPropertyIndex(1,  property1,  ref reader, ref values)) { }
                            else if (TrySetPropertyIndex(2,  property2,  ref reader, ref values)) { }
                            else if (TrySetPropertyIndex(3,  property3,  ref reader, ref values)) { }
                            else if (TrySetPropertyIndex(4,  property4,  ref reader, ref values)) { }
                            else if (TrySetPropertyIndex(5,  property5,  ref reader, ref values)) { }
                            else if (TrySetPropertyIndex(6,  property6,  ref reader, ref values)) { }
                            else if (TrySetPropertyIndex(7,  property7,  ref reader, ref values)) { }
                            else if (TrySetPropertyIndex(8,  property8,  ref reader, ref values)) { }
                            else if (TrySetPropertyIndex(9,  property9,  ref reader, ref values)) { }
                            else if (TrySetPropertyIndex(10, property10, ref reader, ref values)) { }
                            else if (TrySetPropertyIndex(11, property11, ref reader, ref values)) { }
                            else if (TrySetPropertyIndex(12, property12, ref reader, ref values)) { }
                            else if (TrySetPropertyIndex(13, property13, ref reader, ref values)) { }
                            else if (TrySetPropertyIndex(14, property14, ref reader, ref values)) { }
                            else if (TrySetPropertyIndex(15, property15, ref reader, ref values)) { }
                            else if (TrySetPropertyIndex(16, property16, ref reader, ref values)) { }

                            if (!reader.Read())
                                return reader.Suspend((sm, values));

                            sm.OnPropertyNameRead();

                            break;

                        case ObjectReadStateMachine.ReadResult.PropertyValue:
                            static JsonReadResult<TResult>? ReadPropertyValue<TValue>(IJsonProperty<TValue, JsonReadResult<TValue>> property,
                                                                                      ref Utf8JsonReader reader,
                                                                                      ref (bool, TValue) value,
                                                                                      ref ObjectReadStateMachine sm,
                                                                                      ref Values<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> values)
                            {
                                switch (property.Reader.TryRead(ref reader))
                                {
                                    case { Incomplete: true }:
                                        return reader.Suspend((sm, values));
                                    case { Error: { } err }:
                                        return new JsonReadError(err);
                                    case { Value: var val }:
                                        value = (true, val);
                                        return null;
                                }
                            }

                            if (values.NextPropertyIndex is null)
                            {
                                if (reader.IsFinalBlock)
                                    reader.Skip();
                                else if (!reader.TrySkip())
                                    return reader.Suspend((sm, values));
                            }

                            var error = values.NextPropertyIndex switch
                            {
                                1  => ReadPropertyValue(property1,  ref reader, ref values.P1,  ref sm, ref values),
                                2  => ReadPropertyValue(property2,  ref reader, ref values.P2,  ref sm, ref values),
                                3  => ReadPropertyValue(property3,  ref reader, ref values.P3,  ref sm, ref values),
                                4  => ReadPropertyValue(property4,  ref reader, ref values.P4,  ref sm, ref values),
                                5  => ReadPropertyValue(property5,  ref reader, ref values.P5,  ref sm, ref values),
                                6  => ReadPropertyValue(property6,  ref reader, ref values.P6,  ref sm, ref values),
                                7  => ReadPropertyValue(property7,  ref reader, ref values.P7,  ref sm, ref values),
                                8  => ReadPropertyValue(property8,  ref reader, ref values.P8,  ref sm, ref values),
                                9  => ReadPropertyValue(property9,  ref reader, ref values.P9,  ref sm, ref values),
                                10 => ReadPropertyValue(property10, ref reader, ref values.P10, ref sm, ref values),
                                11 => ReadPropertyValue(property11, ref reader, ref values.P11, ref sm, ref values),
                                12 => ReadPropertyValue(property12, ref reader, ref values.P12, ref sm, ref values),
                                13 => ReadPropertyValue(property13, ref reader, ref values.P13, ref sm, ref values),
                                14 => ReadPropertyValue(property14, ref reader, ref values.P14, ref sm, ref values),
                                15 => ReadPropertyValue(property15, ref reader, ref values.P15, ref sm, ref values),
                                16 => ReadPropertyValue(property16, ref reader, ref values.P16, ref sm, ref values),
                                null => null,
                                _  => throw new InvalidOperationException()
                            };

                            if (error is not null)
                                return error.Value;

                            sm.OnPropertyValueRead();
                            values.NextPropertyIndex = null;

                            break;
                    }
                }
            }
        });

    public static IJsonReader<T[]> Array<T>(IJsonReader<T> itemReader) =>
        Array(itemReader, list => list.ToArray());

    public static IJsonReader<TResult> Array<T, TResult>(IJsonReader<T> itemReader,
                                                         Func<List<T>, TResult> resultSelector) =>
        Create((ref Utf8JsonReader rdr) =>
        {
            var (sm, list) =
                rdr.IsResuming && ((ArrayReadStateMachine, List<T>))rdr.Pop() is var ps
                    ? ps
                    : default;

            return Read(ref rdr, sm, list);

            JsonReadResult<TResult> Read(ref Utf8JsonReader rdr, ArrayReadStateMachine sm, List<T>? list)
            {
                while (true)
                {
                    switch (sm.Read(ref rdr))
                    {
                        case ArrayReadStateMachine.ReadResult.Error:
                            return Error("Invalid JSON value where a JSON array was expected.");

                        case ArrayReadStateMachine.ReadResult.Incomplete:
                            return rdr.Suspend((sm, list));

                        case ArrayReadStateMachine.ReadResult.Done:
                            return Value(resultSelector(list ?? new List<T>()));

                        case ArrayReadStateMachine.ReadResult.Item:
                        {
                            if (sm.CurrentItemLoopCount is 0)
                            {
                                var read = rdr.Read();
                                Debug.Assert(read);
                            }

                            switch (itemReader.TryRead(ref rdr))
                            {
                                case var r when r.IsIncomplete():
                                    return rdr.Suspend((sm, list));
                                case { Error: { } error }:
                                    return Error(error);
                                case { Value: var item }:
                                    list ??= new List<T>();
                                    list.Add(item);
                                    sm.OnItemRead();
                                    break;
                            }
                            break;
                        }
                    }
                }
            }
        });

    public static IJsonReader<TResult> Select<T, TResult>(this IJsonReader<T> reader, Func<T, TResult> selector) =>
        Create((ref Utf8JsonReader rdr) =>
            reader.TryRead(ref rdr) switch
            {
                { Error: { } error } => Error(error),
                { Value: var value } => Value(selector(value)),
            });

    public static IJsonReader<TResult> TryMap<T, TResult>(this IJsonReader<T> reader, Func<T, JsonReadResult<TResult>> selector) =>
        Create((ref Utf8JsonReader rdr) =>
            reader.TryRead(ref rdr) switch
            {
                { Error: { } error } => Error(error),
                { Value: var value } => selector(value)
            });

    public static IJsonReader<T> Recursive<T>(Func<IJsonReader<T>, IJsonReader<T>> readerFunction)
    {
        if (readerFunction == null) throw new ArgumentNullException(nameof(readerFunction));
        IJsonReader<T>? reader = null;
        var recReader = Create((ref Utf8JsonReader rdr) => reader!.TryRead(ref rdr));
        reader = readerFunction(recReader);
        return recReader;
    }

    static NotSupportedException PartialJsonNotSupportedException() =>
        new($"Partial JSON reading is not supported. Combine with {nameof(Buffer)}.");

    static IJsonReader<T> Create<T>(Handler<T> handler) =>
        new DelegatingJsonReader<T>(handler);

    static IJsonReader<T> CreatePure<T>(Handler<T> handler) =>
        new DelegatingJsonReader<T>(handler, pure: true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static JsonReadResult<T> Value<T>(T value) => JsonReadResult.Value(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static JsonReadError Error(string message) => new(message);

    public delegate JsonReadResult<T> Handler<T>(ref Utf8JsonReader reader);

    sealed class DelegatingJsonReader<T> : IJsonReader<T>
    {
        readonly Handler<T> handler;
        readonly bool pure;

        public DelegatingJsonReader(Handler<T> handler, bool pure = false)
        {
            this.handler = handler;
            this.pure = pure;
        }

        public JsonReadResult<T> TryRead(ref Utf8JsonReader reader)
        {
            if (!this.pure && reader.TokenType is JsonTokenType.None && !reader.Read())
                return JsonReadError.Incomplete;

            var (value, error) = this.handler(ref reader);
            if (error is not null)
                return new JsonReadError(error);
            return JsonReadResult.Value(value);
        }
    }
}

public sealed class JsonReaderRef<T> : IJsonReader<T>
{
    IJsonReader<T>? reader;

#pragma warning disable CA1044 // Properties should not be write only
    public IJsonReader<T> Reader { set => this.reader = value; }
#pragma warning restore CA1044 // Properties should not be write only

    public JsonReadResult<T> TryRead(ref Utf8JsonReader reader) =>
        this.reader is { } someReader ? someReader.TryRead(ref reader) : throw new InvalidOperationException();
}
