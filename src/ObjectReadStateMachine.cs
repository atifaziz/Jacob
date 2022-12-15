// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Jacob;

using System;
using System.Text.Json;

public record struct ObjectReadStateMachine
{
    public enum State { Initial, PropertyNameOrEnd, PendingPropertyNameRead, PendingPropertyValueRead, Done, Error }
    public enum ReadResult { Error, Incomplete, PropertyName, PropertyValue, Done }

    public State CurrentState { get; private set; }
    public int CurrentPropertyLoopCount { get; private set; }

    public void OnPropertyNameRead() =>
        (CurrentState, CurrentPropertyLoopCount) =
            CurrentState is State.PendingPropertyNameRead
            ? (State.PendingPropertyValueRead, -1)
            : throw new InvalidOperationException();

    public void OnPropertyValueRead() =>
        (CurrentState, CurrentPropertyLoopCount) =
            CurrentState is State.PendingPropertyValueRead
            ? (State.PropertyNameOrEnd, 0)
            : throw new InvalidOperationException();

    public ReadResult Read(ref Utf8JsonReader reader)
    {
        while (true)
        {
            switch (CurrentState)
            {
                case State.Initial:
                    if (reader.TokenType is JsonTokenType.None && !reader.Read())
                        return ReadResult.Incomplete;

                    if (reader.TokenType is not JsonTokenType.StartObject)
                    {
                        CurrentState = State.Error;
                        return ReadResult.Error;
                    }

                    CurrentState = State.PropertyNameOrEnd;
                    break;

                case State.PropertyNameOrEnd:
                    var lookahead = reader;

                    if (!lookahead.Read())
                        return ReadResult.Incomplete;

                    if (lookahead.TokenType is JsonTokenType.EndObject)
                    {
                        reader = lookahead;
                        CurrentState = State.Done;
                        return ReadResult.Done;
                    }

                    CurrentState = State.PendingPropertyNameRead;
                    return ReadResult.PropertyName;

                case State.PendingPropertyNameRead:
                    CurrentPropertyLoopCount++;
                    return ReadResult.PropertyName;

                case State.PendingPropertyValueRead:
                    CurrentPropertyLoopCount++;
                    return ReadResult.PropertyValue;

                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
