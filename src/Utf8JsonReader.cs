// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Jacob;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

#pragma warning disable CA1815 // Override equals and operator equals on value types
public readonly struct JsonReaderState
#pragma warning restore CA1815 // Override equals and operator equals on value types
{
    internal JsonReaderState(System.Text.Json.JsonReaderState state, Stack<object>? stack)
    {
        InnerState = state;
        Stack = stack;
    }

    internal System.Text.Json.JsonReaderState InnerState { get; }
    internal Stack<object>? Stack { get; }
}

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public ref struct Utf8JsonReader
{
    System.Text.Json.Utf8JsonReader reader;
    Stack<object>? stack;

    public static ref System.Text.Json.Utf8JsonReader GetInnerReader(ref Utf8JsonReader reader) => ref reader.reader;

    public Utf8JsonReader(ReadOnlySpan<byte> jsonData, JsonReaderOptions options = default) :
        this(new(jsonData, options), stack: null) { }

    public Utf8JsonReader(ReadOnlySpan<byte> jsonData, bool isFinalBlock, JsonReaderState state) :
        this(new(jsonData, isFinalBlock, state.InnerState), state.Stack) { }

    Utf8JsonReader(System.Text.Json.Utf8JsonReader reader, Stack<object>? stack)
    {
        this.stack = stack;
        this.reader = reader;
    }

    void Push(object frame) => (this.stack ??= new()).Push(frame);

#pragma warning disable IDE0251 // Make member 'readonly' (false negative?)
    object Pop() => (this.stack ?? throw new InvalidOperationException()).Pop();
#pragma warning restore IDE0251 // Make member 'readonly'

    public JsonReadError Suspend(object frame)
    {
        Push(frame);
        return JsonReadError.Incomplete;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ResumeOrDefault<T>() where T : struct =>
        this.stack?.Count > 0 ? (T)Pop() : default;

    public void SetStack(Stack<object>? stack) =>
        this.stack = stack;

    public bool Read() => this.reader.Read();

    public bool TryReadToken(out JsonTokenType tokenType)
    {
        if (!Read())
        {
            tokenType = default;
            return false;
        }

        tokenType = TokenType;
        return true;
    }

    public void Skip() => this.reader.Skip();
    public bool TrySkip() => this.reader.TrySkip();
    public bool GetBoolean() => this.reader.GetBoolean();
    public byte GetByte() => this.reader.GetByte();
    public byte[] GetBytesFromBase64() => this.reader.GetBytesFromBase64();
    public string GetComment() => this.reader.GetComment();
    public DateTime GetDateTime() => this.reader.GetDateTime();
    public DateTimeOffset GetDateTimeOffset() => this.reader.GetDateTimeOffset();
    public decimal GetDecimal() => this.reader.GetDecimal();
    public double GetDouble() => this.reader.GetDouble();
    public Guid GetGuid() => this.reader.GetGuid();
    public short GetInt16() => this.reader.GetInt16();
    public int GetInt32() => this.reader.GetInt32();
    public long GetInt64() => this.reader.GetInt64();
    public sbyte GetSByte() => this.reader.GetSByte();
    public float GetSingle() => this.reader.GetSingle();
    public string? GetString() => this.reader.GetString();
    public ushort GetUInt16() => this.reader.GetUInt16();
    public uint GetUInt32() => this.reader.GetUInt32();
    public ulong GetUInt64() => this.reader.GetUInt64();
    public bool TryGetByte(out byte value) => this.reader.TryGetByte(out value);
    public bool TryGetBytesFromBase64(out byte[]? value) => this.reader.TryGetBytesFromBase64(out value);
    public bool TryGetDateTime(out DateTime value) => this.reader.TryGetDateTime(out value);
    public bool TryGetDateTimeOffset(out DateTimeOffset value) => this.reader.TryGetDateTimeOffset(out value);
    public bool TryGetDecimal(out decimal value) => this.reader.TryGetDecimal(out value);
    public bool TryGetDouble(out double value) => this.reader.TryGetDouble(out value);
    public bool TryGetGuid(out Guid value) => this.reader.TryGetGuid(out value);
    public bool TryGetInt16(out short value) => this.reader.TryGetInt16(out value);
    public bool TryGetInt32(out int value) => this.reader.TryGetInt32(out value);
    public bool TryGetInt64(out long value) => this.reader.TryGetInt64(out value);
    public bool TryGetSByte(out sbyte value) => this.reader.TryGetSByte(out value);
    public bool TryGetSingle(out float value) => this.reader.TryGetSingle(out value);
    public bool TryGetUInt16(out ushort value) => this.reader.TryGetUInt16(out value);
    public bool TryGetUInt32(out uint value) => this.reader.TryGetUInt32(out value);
    public bool TryGetUInt64(out ulong value) => this.reader.TryGetUInt64(out value);
    public readonly bool ValueTextEquals(ReadOnlySpan<byte> utf8Text) => this.reader.ValueTextEquals(utf8Text);
    public readonly bool ValueTextEquals(ReadOnlySpan<char> text) => this.reader.ValueTextEquals(text);
    public readonly bool ValueTextEquals(string? text) => this.reader.ValueTextEquals(text);
    public readonly long BytesConsumed => this.reader.BytesConsumed;
    public readonly int CurrentDepth => this.reader.CurrentDepth;
    public readonly JsonReaderState CurrentState => new(this.reader.CurrentState, this.stack);
    public readonly bool HasValueSequence => this.reader.HasValueSequence;
    public readonly bool IsFinalBlock => this.reader.IsFinalBlock;
    public readonly SequencePosition Position => this.reader.Position;
    public readonly long TokenStartIndex => this.reader.TokenStartIndex;
    public readonly JsonTokenType TokenType => this.reader.TokenType;
    public readonly ReadOnlySequence<byte> ValueSequence => this.reader.ValueSequence;
    public readonly ReadOnlySpan<byte> ValueSpan => this.reader.ValueSpan;

    readonly string DebuggerDisplay => $"TokenType = {TokenType}, CurrentDepth = {CurrentDepth}, BytesConsumed = {BytesConsumed}";
}
