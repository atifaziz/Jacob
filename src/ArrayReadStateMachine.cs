// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Jacob;

using System;
using System.Text.Json;

public record struct ArrayReadStateMachine
{
    public enum State { Initial, ItemOrEnd, Item, Done, Error }
    public enum ReadResult { Error, Incomplete, Item, Done }

    public State CurrentState { get; private set; }

    public void OnItemRead() =>
        CurrentState = CurrentState is State.Item
            ? State.ItemOrEnd
            : throw new InvalidOperationException();

    public ReadResult Read(ref Utf8JsonReader reader)
    {
        while (true)
        {
            switch (CurrentState)
            {
                case State.Initial:
                {
                    if (!reader.Read())
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
                    if (!reader.Read())
                        return ReadResult.Incomplete;

                    if (reader.TokenType is JsonTokenType.EndArray)
                    {
                        CurrentState = State.Done;
                        return ReadResult.Done;
                    }

                    reader.AssumeTokenRead();
                    CurrentState = State.Item;
                    return ReadResult.Item;
                }
                case State.Item:
                {
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
