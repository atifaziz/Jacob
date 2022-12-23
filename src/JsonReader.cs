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
}

public static class IncompleteJsonReadError
{
    public static readonly string Value = "(incomplete)";
}

public record struct JsonReadError(string Message)
{
    public static readonly JsonReadError Incomplete = new(IncompleteJsonReadError.Value);

    public bool IsIncomplete => this == Incomplete;

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

    internal JsonReadError? TryGetValue(out T? item)
    {
        switch (this)
        {
            case { Error: { } error }:
                item = default;
                return new(error);
            case { Value: var value }:
                item = value;
                return null;
        }
    }
}

public interface IJsonReader<T>
{
    JsonReadResult<T> TryRead(ref Utf8JsonReader reader);
}

public interface IJsonProperty<T>
{
    bool IsMatch(in Utf8JsonReader reader);
    IJsonReader<T> Reader { get; }
    bool HasDefaultValue { get; }
    T DefaultValue { get; }
}

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
                        switch (ar.TryReadItem(reader, ref rdr))
                        {
                            case { Incomplete: true }:
                            {
                                break;
                            }
                            case { Error: { } error }:
                            {
                                throw new JsonException(error);
                            }
                            case { Value: var value }:
                            {
                                item = value;
                                readResult = ArrayReadStateMachine.ReadResult.Item;
                                goto exit;
                            }
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

    public static IAsyncEnumerator<KeyValuePair<string, T>>
        GetObjectAsyncEnumerator<T>(this IJsonReader<T> reader,
                                    Stream stream, int initialBufferSize) =>
        GetObjectAsyncEnumerator(reader, stream, initialBufferSize, CancellationToken.None);

    public static IAsyncEnumerator<KeyValuePair<string, T>>
        GetObjectAsyncEnumerator<T>(this IJsonReader<T> reader,
                                    Stream stream, int initialBufferSize,
                                    CancellationToken cancellationToken)
    {
        if (reader is null) throw new ArgumentNullException(nameof(reader));
        if (stream is null) throw new ArgumentNullException(nameof(stream));

        return GetObjectAsyncEnumeratorCore(reader, stream, initialBufferSize, cancellationToken);
    }

    static async IAsyncEnumerator<KeyValuePair<string, T>>
        GetObjectAsyncEnumeratorCore<T>(IJsonReader<T> reader,
                                        Stream stream, int initialBufferSize,
                                        CancellationToken cancellationToken)
    {
        using var scr = new StreamChunkReader(stream, initialBufferSize);

        var state = new JsonReaderState();
        var sm = new ObjectReadStateMachine();
        ObjectReadStateMachine.ReadResult readResult;
        string? name = null;

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
                Debug.Assert(name is not null);
                Debug.Assert(item is not null);
                yield return KeyValuePair.Create(name, item);
            }
        }
        while (readResult is not ObjectReadStateMachine.ReadResult.Done);

        bool TryReadItem(ReadOnlySpan<byte> span,
                         out int bytesConsumed,
                         out ObjectReadStateMachine.ReadResult readResult,
                         [NotNullWhen(true)] out T? item)
        {
            var rdr = new Utf8JsonReader(span, scr.Eof, state);
            while (true)
            {
                switch (readResult = sm.Read(ref rdr))
                {
                    case ObjectReadStateMachine.ReadResult.Error:
                    {
                        throw new JsonException("Invalid JSON value where a JSON object was expected.");
                    }
                    case ObjectReadStateMachine.ReadResult.PropertyName:
                    {
                        name = rdr.GetString();
                        sm.OnPropertyNameRead();
                        break;
                    }
                    case ObjectReadStateMachine.ReadResult.PropertyValue:
                    {
                        switch (reader.TryRead(ref rdr))
                        {
                            case { Incomplete: true }:
                                item = default;
                                readResult = ObjectReadStateMachine.ReadResult.Incomplete;
                                goto exit;
                            case { Error: { } error }:
                                throw new JsonException(error);
                            case { Value: { } value }:
                                sm.OnPropertyValueRead();
                                item = value;
                                goto exit;
                        }

                        break;
                    }
                    case ObjectReadStateMachine.ReadResult.Incomplete:
                    case ObjectReadStateMachine.ReadResult.Done:
                    {
                        item = default;
                        goto exit;
                    }
                }
            }

        exit:
            bytesConsumed = (int)rdr.BytesConsumed;
            state = rdr.CurrentState;
            return readResult is ObjectReadStateMachine.ReadResult.PropertyValue;
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
            var (leftStack, rightStack, leftError, rightError) = rdr.ResumeOrDefault<(Stack<object>?, Stack<object>?, bool, bool)>();

            var leftRdr = rdr;

            if (!leftError)
            {
                _ = leftRdr.SwapStack(leftStack);

                switch (reader1.TryRead(ref leftRdr))
                {
                    case { Incomplete: true }:
                        leftStack = leftRdr.CurrentState.Stack;
                        break;
                    case { Error: not null }:
                        leftError = true;
                        break;
                    case var some:
                        _ = leftRdr.SwapStack(rdr.CurrentState.Stack);
                        rdr = leftRdr;
                        return some;
                }
            }

            var rightRead = false;

            if (!rightError)
            {
                var rdrStack = rdr.SwapStack(rightStack);

                switch (reader2.TryRead(ref rdr))
                {
                    case { Incomplete: true }:
                        rightStack = rdr.CurrentState.Stack;
                        break;
                    case { Error: not null }:
                        rightError = true;
                        break;
                    case var some:
                        _ = rdr.SwapStack(rdrStack);
                        return some;
                }

                _ = rdr.SwapStack(rdrStack);
                rightRead = true;
            }

            switch (leftError, rightRead, rightError)
            {
                case (_   , true, false):
                case (true, true, true ):
                    break;
                default:
                {
                    _ = leftRdr.SwapStack(rdr.CurrentState.Stack);
                    rdr = leftRdr;
                    break;
                }
            };

            if (leftError && rightError)
                return Error(errorMessage ?? "Invalid JSON value.");

            return rdr.Suspend((leftStack, rightStack, leftError, rightError));
        });

    static readonly object BoxedBufferFrame = new Unit();

    public static IJsonReader<T> Buffer<T>(this IJsonReader<T> reader) =>
        Create((ref Utf8JsonReader rdr) =>
        {
            _ = rdr.ResumeOrDefault<Unit>();

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
                        return rdr.Suspend(BoxedBufferFrame);
                    }

                    bool read;
                    do
                    {
                        read = rdr.Read();
                    }
                    while (read && depth < rdr.CurrentDepth);
                    rdr = bookmark;
                    return read ? reader.TryRead(ref rdr) : rdr.Suspend(BoxedBufferFrame);
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

        public bool IsMatch(in Utf8JsonReader reader) =>
            reader.TokenType != JsonTokenType.PropertyName
                ? throw new ArgumentException(null, nameof(reader))
                : reader.ValueTextEquals(this.name);

        public IJsonReader<T> Reader { get; }
        public bool HasDefaultValue { get; }
        public T DefaultValue { get; }
    }

    public static IJsonProperty<T> Property<T>(string name, IJsonReader<T> reader, (bool, T) @default = default) =>
        new JsonProperty<T>(name, reader, @default);

    sealed class NonProperty : IJsonProperty<Unit>
    {
        public static readonly NonProperty Instance = new();

        NonProperty() { }

        public bool IsMatch(in Utf8JsonReader reader) => false;
        public IJsonReader<Unit> Reader => throw new NotSupportedException();
        public bool HasDefaultValue => true;
        public Unit DefaultValue => default;
    }

    public static IJsonReader<TResult> Object<T, TResult>(IJsonReader<T> reader,
                                                          Func<List<KeyValuePair<string, T>>, TResult> resultSelector) =>
        CreatePure((ref Utf8JsonReader rdr) =>
        {
            var (sm, currentPropertyName, acc) = rdr.ResumeOrDefault<(ObjectReadStateMachine, string?, List<KeyValuePair<string, T>>?)>();

            while (true)
            {
                switch (sm.Read(ref rdr))
                {
                    case ObjectReadStateMachine.ReadResult.Error:
                    {
                        return Error("Invalid JSON value where a JSON object was expected.");
                    }
                    case ObjectReadStateMachine.ReadResult.Incomplete:
                    {
                        return rdr.Suspend((sm, currentPropertyName, acc));
                    }
                    case ObjectReadStateMachine.ReadResult.PropertyName:
                    {
                        currentPropertyName = rdr.GetString();
                        sm.OnPropertyNameRead();
                        break;
                    }
                    case ObjectReadStateMachine.ReadResult.PropertyValue:
                    {
                        switch (reader.TryRead(ref rdr))
                        {
                            case { Incomplete: true }:
                                return rdr.Suspend((sm, currentPropertyName, acc));
                            case { Error: { } err }:
                                return new JsonReadError(err);
                            case { Value: var val }:
                                Debug.Assert(currentPropertyName is not null);
                                acc ??= new();
                                acc.Add(KeyValuePair.Create(currentPropertyName, val));
                                break;
                        }

                        sm.OnPropertyValueRead();
                        currentPropertyName = null;

                        break;
                    }
                    case ObjectReadStateMachine.ReadResult.Done:
                    {
                        return Value(resultSelector(acc ?? new()));
                    }
                }
            }
        });

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// the reader to return an error result.
    /// </remarks>
    public static IJsonReader<T> Object<T>(IJsonProperty<T> property) =>
        Object(property, NonProperty.Instance, NonProperty.Instance,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               NonProperty.Instance,
               (v, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _) => v);

    struct ObjectReadState<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
    {
        public (bool, T1) Value1;
        public (bool, T2) Value2;
        public (bool, T3) Value3;
        public (bool, T4) Value4;
        public (bool, T5) Value5;
        public (bool, T6) Value6;
        public (bool, T7) Value7;
        public (bool, T8) Value8;
        public (bool, T9) Value9;
        public (bool, T10) Value10;
        public (bool, T11) Value11;
        public (bool, T12) Value12;
        public (bool, T13) Value13;
        public (bool, T14) Value14;
        public (bool, T15) Value15;
        public (bool, T16) Value16;

        public int? CurrentPropertyIndex;
    };

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// the reader to return an error result.
    /// </remarks>
    public static IJsonReader<TResult>
        Object<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(
            IJsonProperty<T1> property1, IJsonProperty<T2> property2,
            IJsonProperty<T3> property3, IJsonProperty<T4> property4,
            IJsonProperty<T5> property5, IJsonProperty<T6> property6,
            IJsonProperty<T7> property7, IJsonProperty<T8> property8,
            IJsonProperty<T9> property9, IJsonProperty<T10> property10,
            IJsonProperty<T11> property11, IJsonProperty<T12> property12,
            IJsonProperty<T13> property13, IJsonProperty<T14> property14,
            IJsonProperty<T15> property15, IJsonProperty<T16> property16,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> projector) =>
        Create((ref Utf8JsonReader reader) =>
        {
            var (sm, state) = reader.ResumeOrDefault<(ObjectReadStateMachine, ObjectReadState<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>)>();

            return Read(ref reader, sm, ref state);

            JsonReadResult<TResult> Read(ref Utf8JsonReader reader, ObjectReadStateMachine sm,
                                         ref ObjectReadState<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> state)
            {
                while (true)
                {
                    switch (sm.Read(ref reader))
                    {
                        case ObjectReadStateMachine.ReadResult.Error:
                        {
                            return Error("Invalid JSON value where a JSON object was expected.");
                        }
                        case ObjectReadStateMachine.ReadResult.Incomplete:
                        {
                            return reader.Suspend((sm, state));
                        }
                        case ObjectReadStateMachine.ReadResult.PropertyName:
                        {
                            static bool TrySetPropertyIndex<T>(int index,
                                                               IJsonProperty<T> property,
                                                               ref Utf8JsonReader reader,
                                                               ref int? currentIndex)
                            {
                                if (!property.IsMatch(reader))
                                    return false;

                                currentIndex = index;
                                return true;
                            }

                            ref var currentPropertyIndex = ref state.CurrentPropertyIndex;

                            _ =    TrySetPropertyIndex(1,  property1,  ref reader, ref currentPropertyIndex)
                                || TrySetPropertyIndex(2,  property2,  ref reader, ref currentPropertyIndex)
                                || TrySetPropertyIndex(3,  property3,  ref reader, ref currentPropertyIndex)
                                || TrySetPropertyIndex(4,  property4,  ref reader, ref currentPropertyIndex)
                                || TrySetPropertyIndex(5,  property5,  ref reader, ref currentPropertyIndex)
                                || TrySetPropertyIndex(6,  property6,  ref reader, ref currentPropertyIndex)
                                || TrySetPropertyIndex(7,  property7,  ref reader, ref currentPropertyIndex)
                                || TrySetPropertyIndex(8,  property8,  ref reader, ref currentPropertyIndex)
                                || TrySetPropertyIndex(9,  property9,  ref reader, ref currentPropertyIndex)
                                || TrySetPropertyIndex(10, property10, ref reader, ref currentPropertyIndex)
                                || TrySetPropertyIndex(11, property11, ref reader, ref currentPropertyIndex)
                                || TrySetPropertyIndex(12, property12, ref reader, ref currentPropertyIndex)
                                || TrySetPropertyIndex(13, property13, ref reader, ref currentPropertyIndex)
                                || TrySetPropertyIndex(14, property14, ref reader, ref currentPropertyIndex)
                                || TrySetPropertyIndex(15, property15, ref reader, ref currentPropertyIndex)
                                || TrySetPropertyIndex(16, property16, ref reader, ref currentPropertyIndex);

                            sm.OnPropertyNameRead();

                            break;
                        }
                        case ObjectReadStateMachine.ReadResult.PropertyValue:
                        {
                            static JsonReadResult<TResult>? ReadPropertyValue<T>(ref Utf8JsonReader reader,
                                                                                 IJsonProperty<T> property,
                                                                                 ref (bool, T) value,
                                                                                 in ObjectReadStateMachine sm,
                                                                                 in ObjectReadState<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> state)
                            {
                                switch (property.Reader.TryRead(ref reader))
                                {
                                    case { Incomplete: true }:
                                        return reader.Suspend((sm, state));
                                    case { Error: { } err }:
                                        return new JsonReadError(err);
                                    case { Value: var val }:
                                        value = (true, val);
                                        return null;
                                }
                            }

                            if (state.CurrentPropertyIndex is { } propertyIndex)
                            {
                                var result = propertyIndex switch
                                {
                                    1  => ReadPropertyValue(ref reader, property1,  ref state.Value1,  sm, state),
                                    2  => ReadPropertyValue(ref reader, property2,  ref state.Value2,  sm, state),
                                    3  => ReadPropertyValue(ref reader, property3,  ref state.Value3,  sm, state),
                                    4  => ReadPropertyValue(ref reader, property4,  ref state.Value4,  sm, state),
                                    5  => ReadPropertyValue(ref reader, property5,  ref state.Value5,  sm, state),
                                    6  => ReadPropertyValue(ref reader, property6,  ref state.Value6,  sm, state),
                                    7  => ReadPropertyValue(ref reader, property7,  ref state.Value7,  sm, state),
                                    8  => ReadPropertyValue(ref reader, property8,  ref state.Value8,  sm, state),
                                    9  => ReadPropertyValue(ref reader, property9,  ref state.Value9,  sm, state),
                                    10 => ReadPropertyValue(ref reader, property10, ref state.Value10, sm, state),
                                    11 => ReadPropertyValue(ref reader, property11, ref state.Value11, sm, state),
                                    12 => ReadPropertyValue(ref reader, property12, ref state.Value12, sm, state),
                                    13 => ReadPropertyValue(ref reader, property13, ref state.Value13, sm, state),
                                    14 => ReadPropertyValue(ref reader, property14, ref state.Value14, sm, state),
                                    15 => ReadPropertyValue(ref reader, property15, ref state.Value15, sm, state),
                                    16 => ReadPropertyValue(ref reader, property16, ref state.Value16, sm, state),
                                    var i => throw new SwitchExpressionException(i)
                                };

                                if (result is { } someResult)
                                    return someResult;
                            }
                            else
                            {
                                if (reader.IsFinalBlock)
                                    reader.Skip();
                                else if (!reader.TrySkip())
                                    return reader.Suspend((sm, state));
                            }

                            sm.OnPropertyValueRead();
                            state.CurrentPropertyIndex = null;

                            break;
                        }
                        case ObjectReadStateMachine.ReadResult.Done:
                        {
                            static void DefaultUnassigned<T>(IJsonProperty<T> property, ref (bool, T) v)
                            {
                                if (v is (false, _) && property.HasDefaultValue)
                                    v = (true, property.DefaultValue);
                            }

                            DefaultUnassigned(property1, ref state.Value1);
                            DefaultUnassigned(property2, ref state.Value2);
                            DefaultUnassigned(property3, ref state.Value3);
                            DefaultUnassigned(property4, ref state.Value4);
                            DefaultUnassigned(property5, ref state.Value5);
                            DefaultUnassigned(property6, ref state.Value6);
                            DefaultUnassigned(property7, ref state.Value7);
                            DefaultUnassigned(property8, ref state.Value8);
                            DefaultUnassigned(property9, ref state.Value9);
                            DefaultUnassigned(property10, ref state.Value10);
                            DefaultUnassigned(property11, ref state.Value11);
                            DefaultUnassigned(property12, ref state.Value12);
                            DefaultUnassigned(property13, ref state.Value13);
                            DefaultUnassigned(property14, ref state.Value14);
                            DefaultUnassigned(property15, ref state.Value15);
                            DefaultUnassigned(property16, ref state.Value16);

                            return (state.Value1, state.Value2, state.Value3,
                                    state.Value4, state.Value5, state.Value6,
                                    state.Value7, state.Value8, state.Value9,
                                    state.Value10, state.Value11, state.Value12,
                                    state.Value13, state.Value14, state.Value15,
                                    state.Value16) is ((true, var v1), (true, var v2), (true, var v3),
                                                    (true, var v4), (true, var v5), (true, var v6),
                                                    (true, var v7), (true, var v8), (true, var v9),
                                                    (true, var v10), (true, var v11), (true, var v12),
                                                    (true, var v13), (true, var v14), (true, var v15),
                                                    (true, var v16))
                                 ? Value(projector(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16))
                                 : Error("Invalid JSON object.");
                        }
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
            var (sm, list) = rdr.ResumeOrDefault<(ArrayReadStateMachine, List<T>?)>();

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
                            switch (sm.TryReadItem(itemReader, ref rdr))
                            {
                                case { Incomplete: true }:
                                    return rdr.Suspend((sm, list));
                                case { Error: { } error }:
                                    return Error(error);
                                case { Value: var item }:
                                    list ??= new List<T>();
                                    list.Add(item);
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

        public JsonReadResult<T> TryRead(ref Utf8JsonReader reader) =>
            !this.pure && reader.TokenType is JsonTokenType.None
                       && !reader.Read()
            ? JsonReadError.Incomplete
            : this.handler(ref reader) switch
            {
                (_, { } error) => new JsonReadError(error),
                var (value, _) => JsonReadResult.Value(value)
            };
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
