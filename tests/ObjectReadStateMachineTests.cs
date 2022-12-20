// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Jacob.Tests;

using MoreLinq;
using System;
using System.Text;
using Xunit;
using JsonTokenType = System.Text.Json.JsonTokenType;
using ReadResult = ObjectReadStateMachine.ReadResult;
using State = ObjectReadStateMachine.State;

public sealed class ObjectReadStateMachineTests
{
    [Fact]
    public void Default_Instance_Is_Initialized()
    {
        var subject = new ObjectReadStateMachine();
        Assert.Equal(State.Initial, subject.CurrentState);
    }

    [Fact]
    public void OnPropertyNameRead_Throws_When_CurrentState_Is_Not_PendingPropertyNameRead()
    {
        var subject = new ObjectReadStateMachine();
        _ = Assert.Throws<InvalidOperationException>(subject.OnPropertyNameRead);
    }

    [Fact]
    public void OnPropertyValueRead_Throws_When_CurrentState_Is_Not_PendingPropertyValueRead()
    {
        var subject = new ObjectReadStateMachine();
        _ = Assert.Throws<InvalidOperationException>(subject.OnPropertyValueRead);
    }

    [Theory]
    [InlineData(/*lang=json*/ "null")]
    [InlineData(/*lang=json*/ "true")]
    [InlineData(/*lang=json*/ "false")]
    [InlineData(/*lang=json*/ "42")]
    [InlineData(/*lang=json*/ """ "foobar" """)]
    [InlineData(/*lang=json*/ "[]")]
    public void Read_With_Invalid_Input_Is_An_Error(string json)
    {
        var subject = new ObjectReadStateMachine();
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

        var result = subject.Read(ref reader);

        Assert.Equal(ReadResult.Error, result);
        Assert.Equal(State.Error, subject.CurrentState);
    }

    [Fact]
    public void Read_Throws_When_State_Machine_Is_In_Error_State()
    {
        var subject = new ObjectReadStateMachine();
        var reader = new Utf8JsonReader("["u8);

        _ = subject.Read(ref reader);

        Assert.Equal(State.Error, subject.CurrentState);

        var ex = CatchExceptionForAssertion(reader, r => _ = subject.Read(ref r));
        _ = Assert.IsType<InvalidOperationException>(ex);
    }

    [Fact]
    public void Read_Throws_When_State_Machine_Is_In_Done_State()
    {
        var subject = new ObjectReadStateMachine();
        var reader = new Utf8JsonReader("{}"u8);

        _ = subject.Read(ref reader);

        Assert.Equal(State.Done, subject.CurrentState);

        var ex = CatchExceptionForAssertion(reader, r => _ = subject.Read(ref r));
        _ = Assert.IsType<InvalidOperationException>(ex);
    }

    public static readonly TheoryData<string[], (ReadResult, State)[]> Read_Reads_Object_Data =
        new()
        {
            {
                Array.Empty<string>(),
                new[]
                {
                    (ReadResult.Incomplete, State.Initial),
                }
            },
            {
                new[] { "{" },
                new[]
                {
                    (ReadResult.Incomplete, State.PropertyNameOrEnd),
                }
            },
            {
                new[] { "{", "}" },
                new[]
                {
                    (ReadResult.Incomplete, State.PropertyNameOrEnd),
                    (ReadResult.Done, State.Done)
                }
            },
            {
                new[] { "{}" },
                new[]
                {
                    (ReadResult.Done, State.Done)
                }
            },
            {
                new[] { "{", """ "foo": """, """ "bar" """, "}" },
                new[]
                {
                    (ReadResult.Incomplete, State.PropertyNameOrEnd),
                    (ReadResult.PropertyName, State.PendingPropertyNameRead),
                    (ReadResult.PropertyValue, State.PendingPropertyValueRead),
                    (ReadResult.Done, State.Done)
                }
            },
            {
                new[] { "{", """ "f""", """oo": """, """ "bar" """, "}" },
                new[]
                {
                    (ReadResult.Incomplete, State.PropertyNameOrEnd),
                    (ReadResult.Incomplete, State.PropertyNameOrEnd),
                    (ReadResult.PropertyName, State.PendingPropertyNameRead),
                    (ReadResult.PropertyValue, State.PendingPropertyValueRead),
                    (ReadResult.Done, State.Done)
                }
            },
            {
                new[] { "{", """ "foo": """, """ "ba""", """r" """, "}" },
                new[]
                {
                    (ReadResult.Incomplete, State.PropertyNameOrEnd),
                    (ReadResult.PropertyName, State.PendingPropertyNameRead),
                    (ReadResult.PropertyValue, State.PendingPropertyValueRead),
                    (ReadResult.PropertyValue, State.PendingPropertyValueRead),
                    (ReadResult.Done, State.Done)
                }
            },
            {
                new[] { "{", "\"", "f", "o", "o", "\"", ":", "4", "2", "}" },
                new[]
                {
                    (ReadResult.Incomplete, State.PropertyNameOrEnd),
                    (ReadResult.Incomplete, State.PropertyNameOrEnd),
                    (ReadResult.Incomplete, State.PropertyNameOrEnd),
                    (ReadResult.Incomplete, State.PropertyNameOrEnd),
                    (ReadResult.Incomplete, State.PropertyNameOrEnd),
                    (ReadResult.Incomplete, State.PropertyNameOrEnd),
                    (ReadResult.PropertyName, State.PendingPropertyNameRead),
                    (ReadResult.PropertyValue, State.PendingPropertyValueRead),
                    (ReadResult.PropertyValue, State.PendingPropertyValueRead),
                    (ReadResult.PropertyValue, State.PendingPropertyValueRead),
                    (ReadResult.Done, State.Done)
                }
            },
            {
                new[] { "{", """ "foo": """, "123", "}" },
                new[]
                {
                    (ReadResult.Incomplete, State.PropertyNameOrEnd),
                    (ReadResult.PropertyName, State.PendingPropertyNameRead),
                    (ReadResult.PropertyValue, State.PendingPropertyValueRead),
                    (ReadResult.PropertyValue, State.PendingPropertyValueRead),
                    (ReadResult.Done, State.Done)
                }
            },
            {
                new[] { "{", """ "foo":42 """, ",", """ "bar": "baz" """, "}" },
                new[]
                {
                    (ReadResult.Incomplete, State.PropertyNameOrEnd),
                    (ReadResult.PropertyName, State.PendingPropertyNameRead),
                    (ReadResult.PropertyValue, State.PendingPropertyValueRead),
                    (ReadResult.PropertyName, State.PendingPropertyNameRead),
                    (ReadResult.PropertyValue, State.PendingPropertyValueRead),
                    (ReadResult.Done, State.Done)
                }
            },
            {
                new[] { """ {"foo": """, /*lang=json*/ """ {"bar":42} """, "}" },
                new[]
                {
                    (ReadResult.PropertyName, State.PendingPropertyNameRead),
                    (ReadResult.PropertyValue, State.PendingPropertyValueRead),
                    (ReadResult.Done, State.Done)
                }
            },
            {
                new[] { """ {"foo": """, /*lang=json*/ """ [123,456] """, "}"},
                new[]
                {
                    (ReadResult.PropertyName, State.PendingPropertyNameRead),
                    (ReadResult.PropertyValue, State.PendingPropertyValueRead),
                    (ReadResult.Done, State.Done)
                }
            },
        };

    [Theory]
    [MemberData(nameof(Read_Reads_Object_Data))]
    public void Read_Reads_Object(string[] chunks, (ReadResult, State)[] expectations)
    {
        var subject = new ObjectReadStateMachine();
        var jsonReaderState = new JsonReaderState();

        var chunkRun = string.Empty;
        foreach (var (thisChunk, (expectedResult, expectedState)) in
                 chunks.ZipLongest(expectations, ValueTuple.Create))
        {
            var chunk = chunkRun + thisChunk;
            var chunkSpan = Encoding.UTF8.GetBytes(chunk).AsSpan();
            var reader = new Utf8JsonReader(chunkSpan, false, jsonReaderState);
            var result = subject.Read(ref reader);

            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedState, subject.CurrentState);

            if (result is ReadResult.PropertyName)
            {
                subject.OnPropertyNameRead();
                Assert.Equal(State.PendingPropertyValueRead, subject.CurrentState);
            }

            if (result is ReadResult.PropertyValue)
            {
                var read = reader.Read();

                if (read)
                {
                    if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                    {
                        var skipped = reader.TrySkip();
                        Assert.True(skipped);
                    }

                    subject.OnPropertyValueRead();
                    Assert.Equal(State.PropertyNameOrEnd, subject.CurrentState);
                }
            }

            chunkRun = Encoding.UTF8.GetString(chunkSpan[(int)reader.BytesConsumed..]);
            jsonReaderState = reader.CurrentState;
        }

        Assert.Empty(chunkRun);
    }

    delegate void ReaderAction(Utf8JsonReader reader);

    static Exception? CatchExceptionForAssertion(Utf8JsonReader reader, ReaderAction action)
    {
        try
        {
            action(reader);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return ex;
        }

        return null;
    }
}
