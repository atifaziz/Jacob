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

    public void OnPropertyNameRead() =>
        CurrentState =
            CurrentState is State.PendingPropertyNameRead
            ? State.PendingPropertyValueRead
            : throw new InvalidOperationException();

    public void OnPropertyValueRead() =>
        CurrentState =
            CurrentState is State.PendingPropertyValueRead
            ? State.PropertyNameOrEnd
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

                    if (reader.TokenType is not JsonTokenType.StartObject)
                    {
                        CurrentState = State.Error;
                        return ReadResult.Error;
                    }

                    CurrentState = State.PropertyNameOrEnd;
                    break;
                }

                case State.PropertyNameOrEnd:
                {
                    if (!reader.Read())
                        return ReadResult.Incomplete;

                    if (reader.TokenType is JsonTokenType.EndObject)
                    {
                        CurrentState = State.Done;
                        return ReadResult.Done;
                    }

                    CurrentState = State.PendingPropertyNameRead;
                    return ReadResult.PropertyName;
                }

                case State.PendingPropertyNameRead:
                    return ReadResult.PropertyName;

                case State.PendingPropertyValueRead:
                    return ReadResult.PropertyValue;

                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
