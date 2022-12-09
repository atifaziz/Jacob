// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Jacob;

using System;
using System.Text.Json;

public record struct ArrayReadStateMachine
{
    public enum State { Initial, ItemOrEnd, PendingItemRead, Done, Error }
    public enum ReadResult { Error, Incomplete, Item, Done }

    public State CurrentState { get; private set; }
    public int CurrentLength { get; private set; }
    public int CurrentItemLoopCount { get; private set; }

    public void OnItemRead() =>
        (CurrentState, CurrentItemLoopCount, CurrentLength) =
            CurrentState is State.PendingItemRead
            ? (State.ItemOrEnd, 0, CurrentLength + 1)
            : throw new InvalidOperationException();

    public ReadResult Read(ref Utf8JsonReader reader)
    {
        while (true)
        {
            switch (CurrentState)
            {
                case State.Initial:
                {
                    if (reader.TokenType is JsonTokenType.None && !reader.Read())
                        return ReadResult.Incomplete;

                    if (reader.TokenType is not JsonTokenType.StartArray)
                    {
                        CurrentState = State.Error;
                        return ReadResult.Error;
                    }

                    CurrentState = State.ItemOrEnd;
                    break;
                }
                case State.ItemOrEnd:
                {
                    var lookahead = reader;

                    if (!lookahead.Read())
                        return ReadResult.Incomplete;

                    if (lookahead.TokenType is JsonTokenType.EndArray)
                    {
                        reader = lookahead;
                        CurrentState = State.Done;
                        return ReadResult.Done;
                    }

                    CurrentState = State.PendingItemRead;
                    return ReadResult.Item;
                }
                case State.PendingItemRead:
                {
                    CurrentItemLoopCount++;
                    return ReadResult.Item;
                }
                default:
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}
