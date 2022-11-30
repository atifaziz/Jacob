// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Jacob;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
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

public record struct JsonReadError(string Message)
{
    public override string ToString() => Message;
}

public record struct JsonReadResult<T>(T Value, string? Error) : IJsonReadResult<T>
{
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
        _ = rdr.Read();
        return reader.Read(ref rdr);
    }

    public static T Read<T>(this IJsonReader<T> reader, ref Utf8JsonReader utf8Reader)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));

        return reader.TryRead(ref utf8Reader) switch
        {
            (_, { } message) => throw new JsonException($@"{message} See token ""{utf8Reader.TokenType}"" at offset {utf8Reader.TokenStartIndex}."),
            var (value, _) => value,
        };
    }

    public static JsonReadResult<T> TryRead<T>(this IJsonReader<T> reader, string json) =>
        reader.TryRead(Encoding.UTF8.GetBytes(json));

    public static JsonReadResult<T> TryRead<T>(this IJsonReader<T> reader, ReadOnlySpan<byte> utf8JsonTextBytes)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        var rdr = new Utf8JsonReader(utf8JsonTextBytes);
        _ = rdr.Read();
        return reader.TryRead(ref rdr);
    }

    public static IJsonReader<T> Error<T>(string message) =>
        Create<T>((ref Utf8JsonReader _) => Error(message));

    static IJsonReader<string>? stringReader;

    public static IJsonReader<string> String() =>
        stringReader ??=
            Create(static (ref Utf8JsonReader rdr) =>
                rdr.TokenType == JsonTokenType.String
                ? Value(rdr.GetString()!)
                : Error("Invalid JSON value where a JSON string was expected."));

    static IJsonReader<Guid>? guidReader;

    public static IJsonReader<Guid> Guid() =>
        guidReader ??=
            Create(static (ref Utf8JsonReader rdr) =>
                rdr.TokenType == JsonTokenType.String && rdr.TryGetGuid(out var value)
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
                rdr.TokenType == JsonTokenType.String && rdr.TryGetDateTime(out var value)
                ? Value(value)
                : Error("JSON value cannot be interpreted as a date and time in ISO 8601-1 extended format."));

    public static IJsonReader<DateTime> DateTime(string format, IFormatProvider? provider) =>
        DateTime(format, provider, DateTimeStyles.None);

    public static IJsonReader<DateTime> DateTime(string format, IFormatProvider? provider, DateTimeStyles styles) =>
        String().TryMap(s => System.DateTime.TryParseExact(s, format, provider, styles, out var value) ? Value(value) : Error(""));

    static IJsonReader<DateTimeOffset>? dateTimeOffsetReader;

    public static IJsonReader<DateTimeOffset> DateTimeOffset() =>
        dateTimeOffsetReader ??=
            Create(static (ref Utf8JsonReader rdr) =>
                rdr.TokenType == JsonTokenType.String && rdr.TryGetDateTimeOffset(out var value)
                ? Value(value)
                : Error("JSON value cannot be interpreted as a date and time offset in ISO 8601-1 extended format."));

    public static IJsonReader<T> Null<T>(T @null) =>
        Create((ref Utf8JsonReader rdr) =>
            rdr.TokenType == JsonTokenType.Null
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

    public static IJsonReader<object> AsObject<T>(this IJsonReader<T> reader) =>
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
        CreatePure((ref Utf8JsonReader rdr) =>
        {
            var irdr = rdr;
            switch (reader1.TryRead(ref irdr))
            {
                case (_, { }):
                    return reader2.TryRead(ref rdr) switch
                    {
                        (_, { }) => Error(errorMessage ?? "Invalid JSON value."),
                        var some => some
                    };
                case var some:
                    rdr = irdr;
                    return some;
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
        Create((ref Utf8JsonReader rdr) =>
        {
            if (rdr.TokenType != JsonTokenType.StartObject)
                return Error("Invalid JSON value where a JSON object was expected.");

            _ = rdr.Read(); // "{"

            var properties = new List<KeyValuePair<string, T>>();

            while (rdr.TokenType != JsonTokenType.EndObject)
            {
                var name = rdr.GetString()!;
                _ = rdr.Read();

                switch (reader.TryRead(ref rdr))
                {
                    case (_, { } error):
                        return Error(error);
                    case var (value, _):
                        properties.Add(KeyValuePair.Create(name, value));
                        break;
                }
            }

            // Implementation of "Create" will effectively do the following:
            // _ = rdr.Read(); // "}"

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
            if (reader.TokenType != JsonTokenType.StartObject)
                return Error("Invalid JSON value where a JSON object was expected.");

            _ = reader.Read(); // "{"

            (bool, T1) value1 = default;
            (bool, T2) value2 = default;
            (bool, T3) value3 = default;
            (bool, T4) value4 = default;
            (bool, T5) value5 = default;
            (bool, T6) value6 = default;
            (bool, T7) value7 = default;
            (bool, T8) value8 = default;
            (bool, T9) value9 = default;
            (bool, T10) value10 = default;
            (bool, T11) value11 = default;
            (bool, T12) value12 = default;
            (bool, T13) value13 = default;
            (bool, T14) value14 = default;
            (bool, T15) value15 = default;
            (bool, T16) value16 = default;

            while (reader.TokenType != JsonTokenType.EndObject)
            {
                string? error = null;

                if (ReadPropertyValue(property1, ref reader, ref value1, ref error)) continue; if (error is not null) return Error(error);
                if (ReadPropertyValue(property2, ref reader, ref value2, ref error)) continue; if (error is not null) return Error(error);
                if (ReadPropertyValue(property3, ref reader, ref value3, ref error)) continue; if (error is not null) return Error(error);
                if (ReadPropertyValue(property4, ref reader, ref value4, ref error)) continue; if (error is not null) return Error(error);
                if (ReadPropertyValue(property5, ref reader, ref value5, ref error)) continue; if (error is not null) return Error(error);
                if (ReadPropertyValue(property6, ref reader, ref value6, ref error)) continue; if (error is not null) return Error(error);
                if (ReadPropertyValue(property7, ref reader, ref value7, ref error)) continue; if (error is not null) return Error(error);
                if (ReadPropertyValue(property8, ref reader, ref value8, ref error)) continue; if (error is not null) return Error(error);
                if (ReadPropertyValue(property9, ref reader, ref value9, ref error)) continue; if (error is not null) return Error(error);
                if (ReadPropertyValue(property10, ref reader, ref value10, ref error)) continue; if (error is not null) return Error(error);
                if (ReadPropertyValue(property11, ref reader, ref value11, ref error)) continue; if (error is not null) return Error(error);
                if (ReadPropertyValue(property12, ref reader, ref value12, ref error)) continue; if (error is not null) return Error(error);
                if (ReadPropertyValue(property13, ref reader, ref value13, ref error)) continue; if (error is not null) return Error(error);
                if (ReadPropertyValue(property14, ref reader, ref value14, ref error)) continue; if (error is not null) return Error(error);
                if (ReadPropertyValue(property15, ref reader, ref value15, ref error)) continue; if (error is not null) return Error(error);
                if (ReadPropertyValue(property16, ref reader, ref value16, ref error)) continue; if (error is not null) return Error(error);

                _ = reader.Read();
                reader.Skip();
                _ = reader.Read();

                static bool ReadPropertyValue<TValue>(IJsonProperty<TValue, JsonReadResult<TValue>> property,
                                                      ref Utf8JsonReader reader,
                                                      ref (bool, TValue) value,
                                                      ref string? error)
                {
                    if (!property.IsMatch(ref reader))
                        return false;

                    _ = reader.Read();

                    switch (property.Reader.TryRead(ref reader))
                    {
                        case (_, { } err):
                            error = err;
                            return false;
                        case var (val, _):
                            value = (true, val);
                            return true;
                    }
                }
            }

            // Implementation of "Create" will effectively do the following:
            // _ = rdr.Read(); // "}"

            static void DefaultUnassigned<T>(IJsonProperty<T, JsonReadResult<T>> property, ref (bool, T) v)
            {
                if (v is (false, _) && property.HasDefaultValue)
                    v = (true, property.DefaultValue);
            }

            DefaultUnassigned(property1, ref value1);
            DefaultUnassigned(property2, ref value2);
            DefaultUnassigned(property3, ref value3);
            DefaultUnassigned(property4, ref value4);
            DefaultUnassigned(property5, ref value5);
            DefaultUnassigned(property6, ref value6);
            DefaultUnassigned(property7, ref value7);
            DefaultUnassigned(property8, ref value8);
            DefaultUnassigned(property9, ref value9);
            DefaultUnassigned(property10, ref value10);
            DefaultUnassigned(property11, ref value11);
            DefaultUnassigned(property12, ref value12);
            DefaultUnassigned(property13, ref value13);
            DefaultUnassigned(property14, ref value14);
            DefaultUnassigned(property15, ref value15);
            DefaultUnassigned(property16, ref value16);

            return (value1, value2, value3,
                    value4, value5, value6,
                    value7, value8, value9,
                    value10, value11, value12,
                    value13, value14, value15,
                    value16) is ((true, var v1), (true, var v2), (true, var v3),
                                 (true, var v4), (true, var v5), (true, var v6),
                                 (true, var v7), (true, var v8), (true, var v9),
                                 (true, var v10), (true, var v11), (true, var v12),
                                 (true, var v13), (true, var v14), (true, var v15),
                                 (true, var v16))
                 ? Value(projector(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16))
                 : Error("Invalid JSON object.");
        });

    public static IJsonReader<T[]> Array<T>(IJsonReader<T> itemReader) =>
        Array(itemReader, list => list.ToArray());

    public static IJsonReader<TResult> Array<T, TResult>(IJsonReader<T> itemReader,
                                                         Func<List<T>, TResult> resultSelector) =>
        Create((ref Utf8JsonReader rdr) =>
        {
            if (rdr.TokenType != JsonTokenType.StartArray)
                return Error("Invalid JSON value where a JSON array was expected.");

            _ = rdr.Read(); // "["

            var list = new List<T>();
            while (rdr.TokenType != JsonTokenType.EndArray)
            {
                switch (itemReader.TryRead(ref rdr))
                {
                    case (_, { } error):
                        return Error(error);
                    case var (item, _):
                        list.Add(item);
                        break;
                }
            }

            // Implementation of "Create" will effectively do the following:
            // _ = rdr.Read(); // "]"

            return Value(resultSelector(list));
        });

    public static IJsonReader<TResult> Select<T, TResult>(this IJsonReader<T> reader, Func<T, TResult> selector) =>
        CreatePure((ref Utf8JsonReader rdr) =>
            reader.TryRead(ref rdr) switch
            {
                (_, { } error) => Error(error),
                var (value, _) => Value(selector(value)),
            });

    public static IJsonReader<TResult> TryMap<T, TResult>(this IJsonReader<T> reader, Func<T, JsonReadResult<TResult>> selector) =>
        CreatePure((ref Utf8JsonReader rdr) =>
            reader.TryRead(ref rdr) switch
            {
                (_, { } error) => Error(error),
                var (value, _) => selector(value)
            });

    public static IJsonReader<T> Recursive<T>(Func<IJsonReader<T>, IJsonReader<T>> readerFunction)
    {
        if (readerFunction == null) throw new ArgumentNullException(nameof(readerFunction));
        IJsonReader<T>? reader = null;
        var recReader = CreatePure((ref Utf8JsonReader rdr) => reader!.TryRead(ref rdr));
        reader = readerFunction(recReader);
        return recReader;
    }

    public static IJsonReader<T> Create<T>(Handler<T> handler) =>
        new DelegatingJsonReader<T>(handler, shouldReadOnSuccess: true);

    static IJsonReader<T> CreatePure<T>(Handler<T> handler) =>
        new DelegatingJsonReader<T>(handler, shouldReadOnSuccess: false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static JsonReadResult<T> Value<T>(T value) => JsonReadResult.Value(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static JsonReadError Error(string message) => new(message);

    public delegate JsonReadResult<T> Handler<T>(ref Utf8JsonReader reader);

    sealed class DelegatingJsonReader<T> : IJsonReader<T>
    {
        readonly Handler<T> handler;
        readonly bool shouldReadOnSuccess;

        public DelegatingJsonReader(Handler<T> handler, bool shouldReadOnSuccess)
        {
            this.handler = handler;
            this.shouldReadOnSuccess = shouldReadOnSuccess;
        }

        public JsonReadResult<T> TryRead(ref Utf8JsonReader reader)
        {
            var (value, error) = this.handler(ref reader);
            if (error is not null)
                return new JsonReadError(error);
            if (this.shouldReadOnSuccess)
                _ = reader.Read();
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
