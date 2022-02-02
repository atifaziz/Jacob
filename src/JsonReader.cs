// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace JsonR;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Unit = System.ValueTuple;

#pragma warning disable CA1716 // Identifiers should not match keywords

public interface IReadResult<out T>
{
    string? Error { get; }
    T Value { get; }
}

public record struct ReadResult<T>(T Value, string? Error) : IReadResult<T>;

public interface IJsonReader<out T, out TReadResult>
    where TReadResult : IReadResult<T>
{
    TReadResult TryRead(ref Utf8JsonReader reader);
}

public interface IJsonProperty<out T, out TReadResult>
    where TReadResult : IReadResult<T>
{
    bool IsMatch(ref Utf8JsonReader reader);
    IJsonReader<T, TReadResult> Reader { get; }
    bool HasDefaultValue { get; }
    T DefaultValue { get; }
}

#pragma warning disable CA1720 // Identifier contains type name (by design)

public static partial class JsonReader
{
    public static T Read<T>(this IJsonReader<T, JsonR.ReadResult<T>> reader, string json) =>
        reader.Read(Encoding.UTF8.GetBytes(json));

    public static T Read<T>(this IJsonReader<T, JsonR.ReadResult<T>> reader, ReadOnlySpan<byte> utf8JsonTextBytes)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        var rdr = new Utf8JsonReader(utf8JsonTextBytes);
        _ = rdr.Read();
        return reader.Read(ref rdr);
    }

    public static IJsonReader<string, JsonR.ReadResult<string>> String() =>
        Create((ref Utf8JsonReader rdr) =>
            rdr.TokenType == JsonTokenType.String
            ? Value(rdr.GetString()!)
            : Error("Invalid JSON value where a JSON string was expected."));

    public static IJsonReader<bool, JsonR.ReadResult<bool>> Boolean() =>
        Create((ref Utf8JsonReader rdr) =>
            rdr.TokenType switch
            {
                JsonTokenType.True => Value(true),
                JsonTokenType.False => Value(false),
                _ => Error("Invalid JSON value where a JSON Boolean was expected.")
            });

    public static IJsonReader<T, JsonR.ReadResult<T>> Null<T>(T @null) =>
        Create((ref Utf8JsonReader rdr) =>
            rdr.TokenType == JsonTokenType.Null
            ? Value(@null)
            : Error("Invalid JSON value where a JSON null was expected."));

    private static bool IsEnumDefined<T>(T value) where T : struct, Enum =>
#if NET5_0_OR_GREATER
        Enum.IsDefined(value);
#else
        Enum.IsDefined(typeof(T), value);
#endif

    public static IJsonReader<TEnum, JsonR.ReadResult<TEnum>> AsEnum<TSource, TEnum>(this IJsonReader<TSource, JsonR.ReadResult<TSource>> reader, Func<TSource, TEnum> selector) where TEnum : struct, Enum =>
        reader.Select(selector).Validate($"Invalid member for {typeof(TEnum)}.", IsEnumDefined);

    public static IJsonReader<T, JsonR.ReadResult<T>> Validate<T>(this IJsonReader<T, JsonR.ReadResult<T>> reader, Func<T, bool> predicate) =>
        reader.Validate(errorMessage: null, predicate);

    public static IJsonReader<T, JsonR.ReadResult<T>> Validate<T>(this IJsonReader<T, JsonR.ReadResult<T>> reader, string? errorMessage, Func<T, bool> predicate) =>
        CreatePure((ref Utf8JsonReader rdr) => reader.OptRead(ref rdr) switch
        {
            (_, { }) error => error,
            var (value, _) => predicate(value)
                            ? Value(value)
                            : Error(errorMessage ?? "Invalid JSON value.")
        });

    public static IJsonReader<object, JsonR.ReadResult<object>> AsObject<T>(this IJsonReader<T, JsonR.ReadResult<T>> reader) =>
        from v in reader select (object)v;

    public static IJsonReader<T, JsonR.ReadResult<T>> Either<T>(IJsonReader<T, JsonR.ReadResult<T>> reader1, IJsonReader<T, JsonR.ReadResult<T>> reader2) =>
        Either(reader1, reader2, null);

    public static IJsonReader<T, JsonR.ReadResult<T>> Either<T>(IJsonReader<T, JsonR.ReadResult<T>> reader1, IJsonReader<T, JsonR.ReadResult<T>> reader2,
                                           string? errorMessage) =>
        CreatePure((ref Utf8JsonReader rdr) =>
        {
            var irdr = rdr;
            switch (reader1.OptRead(ref irdr))
            {
                case (_, { }):
                    switch (reader2.OptRead(ref rdr))
                    {
                        case (_, { }):
                            return Error(errorMessage ?? "Invalid JSON value.");
                        case var some:
                            return some;
                    }
                case var some:
                    rdr = irdr;
                    return some;
            }
        });

    [DebuggerDisplay("{" + nameof(name) + "}")]
    private sealed class JsonProperty<T> : IJsonProperty<T, JsonR.ReadResult<T>>
    {
        private readonly string name;

        public JsonProperty(string name, IJsonReader<T, JsonR.ReadResult<T>> reader, (bool, T) @default = default) =>
            (this.name, Reader, (HasDefaultValue, DefaultValue)) = (name, reader, @default);

        public bool IsMatch(ref Utf8JsonReader reader) =>
            reader.TokenType != JsonTokenType.PropertyName
                ? throw new ArgumentException(null, nameof(reader))
                : reader.ValueTextEquals(this.name);

        public IJsonReader<T, JsonR.ReadResult<T>> Reader { get; }
        public bool HasDefaultValue { get; }
        public T DefaultValue { get; }
    }

    public static IJsonProperty<T, JsonR.ReadResult<T>> Property<T>(string name, IJsonReader<T, JsonR.ReadResult<T>> reader, (bool, T) @default = default) =>
        new JsonProperty<T>(name, reader, @default);

    private sealed class NonProperty : IJsonProperty<Unit, JsonR.ReadResult<Unit>>
    {
        public static readonly NonProperty Instance = new();

        private NonProperty() { }

        public bool IsMatch(ref Utf8JsonReader reader) => false;
        public IJsonReader<Unit, JsonR.ReadResult<Unit>> Reader => throw new NotSupportedException();
        public bool HasDefaultValue => true;
        public Unit DefaultValue => default;
    }

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// <see cref="JsonException"/> to be thrown.
    /// </remarks>
    public static IJsonReader<T, JsonR.ReadResult<T>> Object<T>(IJsonProperty<T, JsonR.ReadResult<T>> property) =>
        Object(property, NonProperty.Instance, NonProperty.Instance,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               NonProperty.Instance,
               (v, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _) => v);

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// <see cref="JsonException"/> to be thrown.
    /// </remarks>
    public static IJsonReader<TResult, JsonR.ReadResult<TResult>>
        Object<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(
            IJsonProperty<T1, JsonR.ReadResult<T1>> property1, IJsonProperty<T2, JsonR.ReadResult<T2>> property2, IJsonProperty<T3, JsonR.ReadResult<T3>> property3,
            IJsonProperty<T4, JsonR.ReadResult<T4>> property4, IJsonProperty<T5, JsonR.ReadResult<T5>> property5, IJsonProperty<T6, JsonR.ReadResult<T6>> property6,
            IJsonProperty<T7, JsonR.ReadResult<T7>> property7, IJsonProperty<T8, JsonR.ReadResult<T8>> property8, IJsonProperty<T9, JsonR.ReadResult<T9>> property9,
            IJsonProperty<T10, JsonR.ReadResult<T10>> property10, IJsonProperty<T11, JsonR.ReadResult<T11>> property11, IJsonProperty<T12, JsonR.ReadResult<T12>> property12,
            IJsonProperty<T13, JsonR.ReadResult<T13>> property13, IJsonProperty<T14, JsonR.ReadResult<T14>> property14, IJsonProperty<T15, JsonR.ReadResult<T15>> property15,
            IJsonProperty<T16, JsonR.ReadResult<T16>> property16,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> projector) =>
        Create((ref Utf8JsonReader reader) =>
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

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

                static bool ReadPropertyValue<TValue>(IJsonProperty<TValue, JsonR.ReadResult<TValue>> property,
                    ref Utf8JsonReader reader,
                    ref (bool, TValue) value,
                    ref string? error)
                {
                    if (value is (true, _) || !property.IsMatch(ref reader))
                        return false;

                    _ = reader.Read();

                    switch (property.Reader.OptRead(ref reader))
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

            static void DefaultUnassigned<T>(IJsonProperty<T, JsonR.ReadResult<T>> property, ref (bool, T) v)
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

    public static IJsonReader<(T1, T2, T3), JsonR.ReadResult<(T1, T2, T3)>>
        Tuple<T1, T2, T3>(IJsonReader<T1, JsonR.ReadResult<T1>> item1Reader,
                          IJsonReader<T2, JsonR.ReadResult<T2>> item2Reader,
                          IJsonReader<T3, JsonR.ReadResult<T3>> item3Reader) =>
        Create((ref Utf8JsonReader rdr) =>
        {
            if (rdr.TokenType != JsonTokenType.StartArray)
                return Error("Invalid JSON value where a JSON array was expected.");

            _ = rdr.Read(); // "["

            switch (item1Reader.OptRead(ref rdr) switch
                    {
                        (_, { } error) => Error(error),
                        var (item1, _) => item2Reader.OptRead(ref rdr) switch
                        {
                            (_, { } error) => Error(error),
                            var (item2, _) => item3Reader.OptRead(ref rdr) switch
                            {
                                (_, { } error) => Error(error),
                                var (item3, _) => Value((item1, item2, item3))
                            }
                        }
                    })
            {
                case (_, { }) error:
                {
                    return error;
                }
                case var result:
                {
                    if (rdr.TokenType != JsonTokenType.EndArray)
                        return Error("Invalid JSON; expected JSON array to end.");

                    // Implementation of "Create" will effectively do the following:
                    // _ = rdr.Read(); // "]"

                    return result;
                }
            }
        });

    public static IJsonReader<T[], JsonR.ReadResult<T[]>> Array<T>(IJsonReader<T, JsonR.ReadResult<T>> itemReader) =>
        Create((ref Utf8JsonReader rdr) =>
        {
            if (rdr.TokenType != JsonTokenType.StartArray)
                return Error("Invalid JSON value where a JSON array was expected.");

            _ = rdr.Read(); // "["

            var list = new List<T>();
            while (rdr.TokenType != JsonTokenType.EndArray)
            {
                switch (itemReader.OptRead(ref rdr))
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

            return Value(list.ToArray());
        });

    public static IJsonReader<TResult, JsonR.ReadResult<TResult>> Select<T, TResult>(this IJsonReader<T, JsonR.ReadResult<T>> reader, Func<T, TResult> selector) =>
        CreatePure((ref Utf8JsonReader rdr) => reader.OptRead(ref rdr) switch
        {
            (_, { } error) => Error(error),
            var (value, _) => Value(selector(value)),
        });

    public static T Read<T>(this IJsonReader<T, JsonR.ReadResult<T>> reader, ref Utf8JsonReader utf8Reader)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));

        return reader.OptRead(ref utf8Reader) switch
        {
            (_, { } message) => throw new JsonException(message),
            var (value, _) => value,
        };
    }

    private static ReadResult<T> OptRead<T>(this IJsonReader<T, JsonR.ReadResult<T>> reader, ref Utf8JsonReader utf8Reader) =>
        reader.TryRead(ref utf8Reader) switch
        {
            (_, { } error) => Error(error),
            (var value, null) => Value(value),
        };

    private static IJsonReader<T, JsonR.ReadResult<T>> Create<T>(JsonReaderHandler<T> handler) =>
        new DelegatingJsonReader<T>(handler, shouldReadOnSuccess: true);

    private static IJsonReader<T, JsonR.ReadResult<T>> CreatePure<T>(JsonReaderHandler<T> handler) =>
        new DelegatingJsonReader<T>(handler, shouldReadOnSuccess: false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ReadResult<T> Value<T>(T value) => new(value, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ReadErrorResult Error(string message) => new(message);

    private record struct ReadErrorResult(string Message);

    private record struct ReadResult<T>(T Value, string? Error)
    {
        public static implicit operator ReadResult<T>(ReadErrorResult error) => new(default!, error.Message);
    }

    private delegate ReadResult<T> JsonReaderHandler<T>(ref Utf8JsonReader reader);

    sealed class DelegatingJsonReader<T> : IJsonReader<T, JsonR.ReadResult<T>>
    {
        readonly JsonReaderHandler<T> handler;
        readonly bool shouldReadOnSuccess;

        public DelegatingJsonReader(JsonReaderHandler<T> handler, bool shouldReadOnSuccess)
        {
            this.handler = handler;
            this.shouldReadOnSuccess = shouldReadOnSuccess;
        }

        public JsonR.ReadResult<T> TryRead(ref Utf8JsonReader reader)
        {
            var (value, error) = this.handler(ref reader);
            if (error is not null)
                return new(default!, error);
            if (this.shouldReadOnSuccess)
                reader.Read();
            return new(value, null);
        }
    }
}