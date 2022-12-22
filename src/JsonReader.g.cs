// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//------------------------------------------------------------------------------
// This code was generated by a tool.
// Changes to this file will be lost if the code is re-generated.
//------------------------------------------------------------------------------

namespace Jacob;

using System;
using System.Text.Json;

partial class JsonReader
{
    static IJsonReader<byte> byteReader;

    public static IJsonReader<byte> Byte() =>
        byteReader ??=
            Create(static (ref Utf8JsonReader rdr) =>
                rdr.TokenType is JsonTokenType.Number && rdr.TryGetByte(out var value)
                ? Value(value)
                : Error("Invalid JSON value; expecting a JSON number compatible with Byte."));

    static IJsonReader<int> intReader;

    public static IJsonReader<int> Int32() =>
        intReader ??=
            Create(static (ref Utf8JsonReader rdr) =>
                rdr.TokenType is JsonTokenType.Number && rdr.TryGetInt32(out var value)
                ? Value(value)
                : Error("Invalid JSON value; expecting a JSON number compatible with Int32."));

    static IJsonReader<long> longReader;

    public static IJsonReader<long> Int64() =>
        longReader ??=
            Create(static (ref Utf8JsonReader rdr) =>
                rdr.TokenType is JsonTokenType.Number && rdr.TryGetInt64(out var value)
                ? Value(value)
                : Error("Invalid JSON value; expecting a JSON number compatible with Int64."));

    static IJsonReader<ushort> ushortReader;

    public static IJsonReader<ushort> UInt16() =>
        ushortReader ??=
            Create(static (ref Utf8JsonReader rdr) =>
                rdr.TokenType is JsonTokenType.Number && rdr.TryGetUInt16(out var value)
                ? Value(value)
                : Error("Invalid JSON value; expecting a JSON number compatible with UInt16."));

    static IJsonReader<uint> uintReader;

    public static IJsonReader<uint> UInt32() =>
        uintReader ??=
            Create(static (ref Utf8JsonReader rdr) =>
                rdr.TokenType is JsonTokenType.Number && rdr.TryGetUInt32(out var value)
                ? Value(value)
                : Error("Invalid JSON value; expecting a JSON number compatible with UInt32."));

    static IJsonReader<ulong> ulongReader;

    public static IJsonReader<ulong> UInt64() =>
        ulongReader ??=
            Create(static (ref Utf8JsonReader rdr) =>
                rdr.TokenType is JsonTokenType.Number && rdr.TryGetUInt64(out var value)
                ? Value(value)
                : Error("Invalid JSON value; expecting a JSON number compatible with UInt64."));

    static IJsonReader<double> doubleReader;

    public static IJsonReader<double> Double() =>
        doubleReader ??=
            Create(static (ref Utf8JsonReader rdr) =>
                rdr.TokenType is JsonTokenType.Number && rdr.TryGetDouble(out var value)
                ? Value(value)
                : Error("Invalid JSON value; expecting a JSON number compatible with Double."));

    static IJsonReader<float> floatReader;

    public static IJsonReader<float> Single() =>
        floatReader ??=
            Create(static (ref Utf8JsonReader rdr) =>
                rdr.TokenType is JsonTokenType.Number && rdr.TryGetSingle(out var value)
                ? Value(value)
                : Error("Invalid JSON value; expecting a JSON number compatible with Single."));

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// the reader to return an error result.
    /// </remarks>
    public static IJsonReader<TResult> Object<T1, T2, TResult>(
            IJsonProperty<T1> property1, IJsonProperty<T2> property2,
            Func<T1, T2, TResult> projector) =>
        Object(property1, property2,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               (v1, v2, _, _, _, _, _, _, _, _, _, _, _, _, _, _) =>
            projector(v1, v2));

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// the reader to return an error result.
    /// </remarks>
    public static IJsonReader<TResult> Object<T1, T2, T3, TResult>(
            IJsonProperty<T1> property1, IJsonProperty<T2> property2, IJsonProperty<T3> property3,
            Func<T1, T2, T3, TResult> projector) =>
        Object(property1, property2, property3,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               (v1, v2, v3, _, _, _, _, _, _, _, _, _, _, _, _, _) =>
            projector(v1, v2, v3));

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// the reader to return an error result.
    /// </remarks>
    public static IJsonReader<TResult> Object<T1, T2, T3, T4, TResult>(
            IJsonProperty<T1> property1, IJsonProperty<T2> property2, IJsonProperty<T3> property3, IJsonProperty<T4> property4,
            Func<T1, T2, T3, T4, TResult> projector) =>
        Object(property1, property2, property3, property4,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               (v1, v2, v3, v4, _, _, _, _, _, _, _, _, _, _, _, _) =>
            projector(v1, v2, v3, v4));

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// the reader to return an error result.
    /// </remarks>
    public static IJsonReader<TResult> Object<T1, T2, T3, T4, T5, TResult>(
            IJsonProperty<T1> property1, IJsonProperty<T2> property2, IJsonProperty<T3> property3, IJsonProperty<T4> property4, IJsonProperty<T5> property5,
            Func<T1, T2, T3, T4, T5, TResult> projector) =>
        Object(property1, property2, property3, property4, property5,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               (v1, v2, v3, v4, v5, _, _, _, _, _, _, _, _, _, _, _) =>
            projector(v1, v2, v3, v4, v5));

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// the reader to return an error result.
    /// </remarks>
    public static IJsonReader<TResult> Object<T1, T2, T3, T4, T5, T6, TResult>(
            IJsonProperty<T1> property1, IJsonProperty<T2> property2, IJsonProperty<T3> property3, IJsonProperty<T4> property4, IJsonProperty<T5> property5, IJsonProperty<T6> property6,
            Func<T1, T2, T3, T4, T5, T6, TResult> projector) =>
        Object(property1, property2, property3, property4, property5, property6,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               (v1, v2, v3, v4, v5, v6, _, _, _, _, _, _, _, _, _, _) =>
            projector(v1, v2, v3, v4, v5, v6));

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// the reader to return an error result.
    /// </remarks>
    public static IJsonReader<TResult> Object<T1, T2, T3, T4, T5, T6, T7, TResult>(
            IJsonProperty<T1> property1, IJsonProperty<T2> property2, IJsonProperty<T3> property3, IJsonProperty<T4> property4, IJsonProperty<T5> property5, IJsonProperty<T6> property6, IJsonProperty<T7> property7,
            Func<T1, T2, T3, T4, T5, T6, T7, TResult> projector) =>
        Object(property1, property2, property3, property4, property5, property6, property7,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               (v1, v2, v3, v4, v5, v6, v7, _, _, _, _, _, _, _, _, _) =>
            projector(v1, v2, v3, v4, v5, v6, v7));

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// the reader to return an error result.
    /// </remarks>
    public static IJsonReader<TResult> Object<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(
            IJsonProperty<T1> property1, IJsonProperty<T2> property2, IJsonProperty<T3> property3, IJsonProperty<T4> property4, IJsonProperty<T5> property5, IJsonProperty<T6> property6, IJsonProperty<T7> property7, IJsonProperty<T8> property8,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> projector) =>
        Object(property1, property2, property3, property4, property5, property6, property7, property8,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               (v1, v2, v3, v4, v5, v6, v7, v8, _, _, _, _, _, _, _, _) =>
            projector(v1, v2, v3, v4, v5, v6, v7, v8));

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// the reader to return an error result.
    /// </remarks>
    public static IJsonReader<TResult> Object<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(
            IJsonProperty<T1> property1, IJsonProperty<T2> property2, IJsonProperty<T3> property3, IJsonProperty<T4> property4, IJsonProperty<T5> property5, IJsonProperty<T6> property6, IJsonProperty<T7> property7, IJsonProperty<T8> property8, IJsonProperty<T9> property9,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> projector) =>
        Object(property1, property2, property3, property4, property5, property6, property7, property8, property9,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               (v1, v2, v3, v4, v5, v6, v7, v8, v9, _, _, _, _, _, _, _) =>
            projector(v1, v2, v3, v4, v5, v6, v7, v8, v9));

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// the reader to return an error result.
    /// </remarks>
    public static IJsonReader<TResult> Object<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(
            IJsonProperty<T1> property1, IJsonProperty<T2> property2, IJsonProperty<T3> property3, IJsonProperty<T4> property4, IJsonProperty<T5> property5, IJsonProperty<T6> property6, IJsonProperty<T7> property7, IJsonProperty<T8> property8, IJsonProperty<T9> property9, IJsonProperty<T10> property10,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> projector) =>
        Object(property1, property2, property3, property4, property5, property6, property7, property8, property9, property10,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               (v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, _, _, _, _, _, _) =>
            projector(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10));

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// the reader to return an error result.
    /// </remarks>
    public static IJsonReader<TResult> Object<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(
            IJsonProperty<T1> property1, IJsonProperty<T2> property2, IJsonProperty<T3> property3, IJsonProperty<T4> property4, IJsonProperty<T5> property5, IJsonProperty<T6> property6, IJsonProperty<T7> property7, IJsonProperty<T8> property8, IJsonProperty<T9> property9, IJsonProperty<T10> property10, IJsonProperty<T11> property11,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> projector) =>
        Object(property1, property2, property3, property4, property5, property6, property7, property8, property9, property10, property11,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               (v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, _, _, _, _, _) =>
            projector(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11));

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// the reader to return an error result.
    /// </remarks>
    public static IJsonReader<TResult> Object<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(
            IJsonProperty<T1> property1, IJsonProperty<T2> property2, IJsonProperty<T3> property3, IJsonProperty<T4> property4, IJsonProperty<T5> property5, IJsonProperty<T6> property6, IJsonProperty<T7> property7, IJsonProperty<T8> property8, IJsonProperty<T9> property9, IJsonProperty<T10> property10, IJsonProperty<T11> property11, IJsonProperty<T12> property12,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> projector) =>
        Object(property1, property2, property3, property4, property5, property6, property7, property8, property9, property10, property11, property12,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               (v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, _, _, _, _) =>
            projector(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12));

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// the reader to return an error result.
    /// </remarks>
    public static IJsonReader<TResult> Object<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(
            IJsonProperty<T1> property1, IJsonProperty<T2> property2, IJsonProperty<T3> property3, IJsonProperty<T4> property4, IJsonProperty<T5> property5, IJsonProperty<T6> property6, IJsonProperty<T7> property7, IJsonProperty<T8> property8, IJsonProperty<T9> property9, IJsonProperty<T10> property10, IJsonProperty<T11> property11, IJsonProperty<T12> property12, IJsonProperty<T13> property13,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> projector) =>
        Object(property1, property2, property3, property4, property5, property6, property7, property8, property9, property10, property11, property12, property13,
               NonProperty.Instance, NonProperty.Instance, NonProperty.Instance,
               (v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, _, _, _) =>
            projector(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13));

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// the reader to return an error result.
    /// </remarks>
    public static IJsonReader<TResult> Object<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(
            IJsonProperty<T1> property1, IJsonProperty<T2> property2, IJsonProperty<T3> property3, IJsonProperty<T4> property4, IJsonProperty<T5> property5, IJsonProperty<T6> property6, IJsonProperty<T7> property7, IJsonProperty<T8> property8, IJsonProperty<T9> property9, IJsonProperty<T10> property10, IJsonProperty<T11> property11, IJsonProperty<T12> property12, IJsonProperty<T13> property13, IJsonProperty<T14> property14,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> projector) =>
        Object(property1, property2, property3, property4, property5, property6, property7, property8, property9, property10, property11, property12, property13, property14,
               NonProperty.Instance, NonProperty.Instance,
               (v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, _, _) =>
            projector(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14));

    /// <remarks>
    /// Properties without a default value that are missing from the read JSON object will cause
    /// the reader to return an error result.
    /// </remarks>
    public static IJsonReader<TResult> Object<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(
            IJsonProperty<T1> property1, IJsonProperty<T2> property2, IJsonProperty<T3> property3, IJsonProperty<T4> property4, IJsonProperty<T5> property5, IJsonProperty<T6> property6, IJsonProperty<T7> property7, IJsonProperty<T8> property8, IJsonProperty<T9> property9, IJsonProperty<T10> property10, IJsonProperty<T11> property11, IJsonProperty<T12> property12, IJsonProperty<T13> property13, IJsonProperty<T14> property14, IJsonProperty<T15> property15,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> projector) =>
        Object(property1, property2, property3, property4, property5, property6, property7, property8, property9, property10, property11, property12, property13, property14, property15,
               NonProperty.Instance,
               (v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, _) =>
            projector(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15));

    public static IJsonReader<(T1, T2)>
        Tuple<T1, T2>(
            IJsonReader<T1> item1Reader,
            IJsonReader<T2> item2Reader) =>
        CreatePure((ref Utf8JsonReader rdr) =>
        {
            var (sm, item1, item2) = rdr.ResumeOrDefault<(ArrayReadStateMachine, T1, T2)>();

            while (true)
            {
                switch (sm.Read(ref rdr))
                {
                    case ArrayReadStateMachine.ReadResult.Error:
                        return Error("Invalid JSON value where a JSON array was expected.");

                    case ArrayReadStateMachine.ReadResult.Incomplete:
                        return rdr.Suspend((sm, item1, item2));

                    case ArrayReadStateMachine.ReadResult.Done:
                        return sm.CurrentLength is not 2
                            ? Error("Invalid JSON value; JSON array has too few values.")
                            : Value((item1, item2));

                    case ArrayReadStateMachine.ReadResult.Item:
                    {
                        switch ((sm.CurrentLength + 1) switch
                        {
                            1 => sm.TryReadItem(item1Reader, ref rdr).TryGetValue(out item1),
                            2 => sm.TryReadItem(item2Reader, ref rdr).TryGetValue(out item2),
                            _ => Error("Invalid JSON value; JSON array has too many values.")
                        })
                        {
                            case { IsIncomplete: true }: return rdr.Suspend((sm, item1, item2));
                            case { } other: return other;
                        }

                        break;
                    }
                }
            }
        });

    public static IJsonReader<(T1, T2, T3)>
        Tuple<T1, T2, T3>(
            IJsonReader<T1> item1Reader,
            IJsonReader<T2> item2Reader,
            IJsonReader<T3> item3Reader) =>
        CreatePure((ref Utf8JsonReader rdr) =>
        {
            var (sm, item1, item2, item3) = rdr.ResumeOrDefault<(ArrayReadStateMachine, T1, T2, T3)>();

            while (true)
            {
                switch (sm.Read(ref rdr))
                {
                    case ArrayReadStateMachine.ReadResult.Error:
                        return Error("Invalid JSON value where a JSON array was expected.");

                    case ArrayReadStateMachine.ReadResult.Incomplete:
                        return rdr.Suspend((sm, item1, item2, item3));

                    case ArrayReadStateMachine.ReadResult.Done:
                        return sm.CurrentLength is not 3
                            ? Error("Invalid JSON value; JSON array has too few values.")
                            : Value((item1, item2, item3));

                    case ArrayReadStateMachine.ReadResult.Item:
                    {
                        switch ((sm.CurrentLength + 1) switch
                        {
                            1 => sm.TryReadItem(item1Reader, ref rdr).TryGetValue(out item1),
                            2 => sm.TryReadItem(item2Reader, ref rdr).TryGetValue(out item2),
                            3 => sm.TryReadItem(item3Reader, ref rdr).TryGetValue(out item3),
                            _ => Error("Invalid JSON value; JSON array has too many values.")
                        })
                        {
                            case { IsIncomplete: true }: return rdr.Suspend((sm, item1, item2, item3));
                            case { } other: return other;
                        }

                        break;
                    }
                }
            }
        });
}
