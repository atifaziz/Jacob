// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Jacob;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public enum JsonKeyKind { Index, Name }

public readonly record struct JsonKey
{
    readonly string? name;

    public JsonKey(int index) => Index = index;
    public JsonKey(string name) => this.name = name;

    public JsonKeyKind Kind => Name is { Length: > 0 } ? JsonKeyKind.Name : JsonKeyKind.Index;

    public int Index { get; }
    public string Name => this.name ?? string.Empty;

    public override string ToString() =>
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
        Kind switch
        {
            JsonKeyKind.Index => Index.ToString((IFormatProvider?)null),
            JsonKeyKind.Name => Name,
        };
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
}

#pragma warning disable IDE0250 // Make struct 'readonly' (modifies in=place)
public struct CurrentJsonPath : IEquatable<CurrentJsonPath>
#pragma warning restore IDE0250 // Make struct 'readonly'
{
    readonly List<JsonKey> keys;

    internal CurrentJsonPath(List<JsonKey> keys) => this.keys = keys;

    public readonly int Count => this.keys.Count;
    public readonly JsonKey this[int index] => this.keys[index];

    public override readonly string ToString()
    {
        var sb = new StringBuilder();
        foreach (var key in this.keys)
        {
            if (key.Kind is JsonKeyKind.Index)
            {
                _ = sb.Append('[').Append(key.Index).Append(']');
            }
            else
            {
                if (sb.Length > 0)
                    _ = sb.Append('.');
                _ = sb.Append(key.Name);
            }
        }
        return sb.ToString();
    }

    public readonly bool Equals(CurrentJsonPath other) => this.keys.SequenceEqual(other.keys);
    public override readonly bool Equals(object? obj) => obj is CurrentJsonPath other && Equals(other);
    public override readonly int GetHashCode() => this.keys.Aggregate(0, HashCode.Combine);

    public static bool operator ==(CurrentJsonPath left, CurrentJsonPath right) => left.Equals(right);
    public static bool operator !=(CurrentJsonPath left, CurrentJsonPath right) => !left.Equals(right);
}

public interface IAsyncJsonEnumerator : IAsyncEnumerator<JsonTokenType>
{
    CurrentJsonPath CurrentPath { get; }
    int CurrentDepth { get; }
    ValueTask<T> ReadAsync<T>(IJsonReader<T> reader, CancellationToken cancellationToken);
}

public static class AsyncJsonEnumerator
{
    enum JsonStructureKind { Object, Array }

    struct StructureReadStateMachine
    {
        readonly JsonStructureKind kind;

        public ObjectReadStateMachine Object;
        public ArrayReadStateMachine Array;

        public StructureReadStateMachine(JsonStructureKind kind) => this.kind = kind;

        public Status OnItemRead()
        {
            Status status;

            if (this.kind is JsonStructureKind.Object)
            {
                this.Object.OnPropertyValueRead();
                status = Status.Object;
            }
            else
            {
                this.Array.OnItemRead();
                status = Status.Array;
            }

            return status;
        }
    }

    enum Status { Initial, Object, Array, Item, Done }

    sealed class State
    {
        StructureReadStateMachine[] machineStack = new StructureReadStateMachine[4];
        int machineStackCount;
        ReadResult lastReadResult;
        JsonReaderState jsonReaderState;

        public Status Status = Status.Initial;
        public readonly PipeReader Reader;
        public readonly List<JsonKey> Path = new();

        public State(PipeReader reader) => this.Reader = reader;

        public ref StructureReadStateMachine Current
        {
            get
            {
                Debug.Assert(this.machineStackCount > 0);
                return ref this.machineStack[this.machineStackCount - 1];
            }
        }

        public void Push(StructureReadStateMachine sm)
        {
            if (this.machineStackCount == this.machineStack.Length)
                Array.Resize(ref this.machineStack, this.machineStackCount * 2);
            this.machineStack[this.machineStackCount++] = sm;
        }

        public int Pop()
        {
            Debug.Assert(this.machineStackCount > 0);
            return --this.machineStackCount;
        }

        public void CreateUtf8JsonReader(out Utf8JsonReader reader) =>
            reader = new Utf8JsonReader(this.lastReadResult.Buffer,
                                        isFinalBlock: this.lastReadResult.IsCompleted,
                                        this.jsonReaderState);

        public async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken) =>
            this.lastReadResult = await this.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

        public void UpdateRead(in Utf8JsonReader reader, bool incomplete)
        {
            var bytesConsumed = reader.BytesConsumed;
            this.jsonReaderState = reader.CurrentState;
            var buffer = this.lastReadResult.Buffer;
            var consumedPosition = buffer.GetPosition(bytesConsumed);
            var (consumed, examined) = incomplete
                                     ? (consumedPosition, buffer.End)
                                     : (consumedPosition, consumedPosition);
            this.Reader.AdvanceTo(consumed, examined);
        }
    }

    sealed class Enumerator : IAsyncJsonEnumerator
    {
        readonly State state;
        readonly IAsyncEnumerator<JsonTokenType> enumerator;

        public Enumerator(State state, IAsyncEnumerator<JsonTokenType> enumerator)
        {
            this.state = state;
            this.enumerator = enumerator;
        }

        public ValueTask DisposeAsync() => this.enumerator.DisposeAsync();
        public ValueTask<bool> MoveNextAsync() => this.enumerator.MoveNextAsync();
        public JsonTokenType Current => this.enumerator.Current;
        public CurrentJsonPath CurrentPath => new(this.state.Path);
        public int CurrentDepth => CurrentPath.Count;

        public async ValueTask<T> ReadAsync<T>(IJsonReader<T> reader, CancellationToken cancellationToken)
        {
            while (true)
            {
                var result = await this.state.ReadAsync(cancellationToken).ConfigureAwait(false);

                if (result.IsCanceled)
                    throw new OperationCanceledException();

                if (TryRead(this.state, reader, out var item))
                {
                    this.state.Status = this.state.Current.OnItemRead();
                    return item;
                }

                // TODO: handle result.IsCompleted
            }

            static bool TryRead(State state, IJsonReader<T> reader, [NotNullWhen(true)] out T? item)
            {
                state.CreateUtf8JsonReader(out var rdr);
                var read = false;
                try
                {
                    switch (reader.TryRead(ref rdr))
                    {
                        case { Incomplete: true }: item = default; break;
                        case { Error: { } error }: throw new JsonException(error);
                        case { Value: var value }: (read, item) = (true, value); break;
                    }
                    return read;
                }
                finally
                {
                    state.UpdateRead(rdr, !read);
                }
            }
        }
    }

    public static IAsyncJsonEnumerator Open(PipeReader reader, CancellationToken cancellationToken)
    {
        var state = new State(reader);
        return new Enumerator(state, Open());

        async IAsyncEnumerator<JsonTokenType> Open()
        {
            while (true)
            {
                var readResult = await state.ReadAsync(cancellationToken).ConfigureAwait(false);

                if (readResult.IsCanceled)
                    throw new OperationCanceledException();

                var tokenType = TryRead(state);
                cancellationToken.ThrowIfCancellationRequested();
                if (tokenType is { } someTokenType)
                    yield return someTokenType;
                else if (state.Status is Status.Done)
                    break;
            }

            // TODO? state.Reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
            await state.Reader.CompleteAsync().ConfigureAwait(false);

            static JsonTokenType? TryRead(State state)
            {
                state.CreateUtf8JsonReader(out var rdr);
                var read = false;
                try
                {
                    read = Iterate(ref rdr);
                    return read ? rdr.TokenType : null;
                }
                finally
                {
                    state.UpdateRead(rdr, !read);
                }

                bool Iterate(ref Utf8JsonReader rdr)
                {
                    while (true)
                    {
                        switch (state.Status)
                        {
                            case Status.Done:
                            {
                                return false;
                            }
                            case Status.Initial:
                            {
                                if (rdr.TokenType is JsonTokenType.None && !rdr.Read())
                                    return false;
                                goto case Status.Item;
                            }
                            case Status.Item:
                            {
                                switch (rdr.TokenType)
                                {
                                    case JsonTokenType.StartObject:
                                    {
                                        state.Push(new(JsonStructureKind.Object));
                                        state.Status = Status.Object;
                                        state.Path.Add(new("?"));
                                        break;
                                    }
                                    case JsonTokenType.StartArray:
                                    {
                                        state.Push(new(JsonStructureKind.Array));
                                        state.Status = Status.Array;
                                        state.Path.Add(new(-1));
                                        break;
                                    }
                                    case JsonTokenType.Null or JsonTokenType.True or JsonTokenType.False or JsonTokenType.Number or JsonTokenType.String:
                                    {
                                        if (!rdr.TrySkip())
                                            return false;
                                        state.Status = state.Current.OnItemRead();
                                        break;
                                    }
                                    case var other:
                                    {
                                        throw new SwitchExpressionException($"Unexpected token: {other}");
                                    }
                                }
                                break;
                            }
                            case Status.Object:
                            {
                                ref var obj = ref state.Current.Object;

                                if (obj.CurrentState is ObjectReadStateMachine.State.PendingPropertyValueRead)
                                    goto case Status.Item;

                                read:
                                switch (obj.Read(ref rdr))
                                {
                                    case ObjectReadStateMachine.ReadResult.Error:
                                    {
                                        throw new JsonException("Invalid JSON value where a JSON object was expected.");
                                    }
                                    case ObjectReadStateMachine.ReadResult.PropertyName:
                                    {
                                        var name = rdr.GetString();
                                        Debug.Assert(name is not null);
                                        obj.OnPropertyNameRead();
                                        state.Path[^1] = new(name);
                                        goto read;
                                    }
                                    case ObjectReadStateMachine.ReadResult.PropertyValue:
                                    {
                                        return true;
                                    }
                                    case ObjectReadStateMachine.ReadResult.Incomplete:
                                    {
                                        return false;
                                    }
                                    case ObjectReadStateMachine.ReadResult.Done:
                                    {
                                        state.Path.RemoveAt(state.Path.Count - 1);
                                        state.Status = state.Pop() == 0
                                                     ? Status.Done
                                                     : state.Current.OnItemRead();
                                        break;
                                    }
                                    case var readResult:
                                        throw new SwitchExpressionException(readResult);
                                }
                                break;
                            }
                            case Status.Array:
                            {
                                ref var arr = ref state.Current.Array;

                                if (arr.CurrentState is ArrayReadStateMachine.State.PendingItemRead)
                                    goto case Status.Item;

                                switch (arr.Read(ref rdr))
                                {
                                    case ArrayReadStateMachine.ReadResult.Error:
                                    {
                                        throw new JsonException("Invalid JSON value where a JSON array was expected.");
                                    }
                                    case ArrayReadStateMachine.ReadResult.Item:
                                    {
                                        state.Path[^1] = new(state.Path[^1].Index + 1);
                                        return true;
                                    }
                                    case ArrayReadStateMachine.ReadResult.Done:
                                    {
                                        state.Path.RemoveAt(state.Path.Count - 1);
                                        state.Status = state.Pop() == 0
                                                     ? Status.Done
                                                     : state.Current.OnItemRead();
                                        break;
                                    }
                                    case ArrayReadStateMachine.ReadResult.Incomplete:
                                    {
                                        return false;
                                    }
                                }
                                break;
                            }
                            case var status:
                            {
                                throw new SwitchExpressionException(status);
                            }
                        }
                    }
                }
            }
        }
    }
}
