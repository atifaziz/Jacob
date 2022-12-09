// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Jacob.Tests;

using System;
using System.Linq;
using System.Text;
using Xunit;
using MoreLinq;
using JsonTokenType = System.Text.Json.JsonTokenType;
using ReadResult = ArrayReadStateMachine.ReadResult;
using State = ArrayReadStateMachine.State;

public class ArrayReadStateMachineTests
{
    [Fact]
    public void Default_Instance_Is_Initialized()
    {
        var subject = new ArrayReadStateMachine();
        Assert.Equal(State.Initial, subject.CurrentState);
        Assert.Equal(0, subject.CurrentLength);
        Assert.Equal(0, subject.CurrentItemLoopCount);
    }

    [Fact]
    public void OnItemRead_Throws_When_CurrentState_Is_Not_Item()
    {
        var subject = new ArrayReadStateMachine();
        _ = Assert.Throws<InvalidOperationException>(subject.OnItemRead);
    }

    [Theory]
    [InlineData(/*lang=json*/ "null")]
    [InlineData(/*lang=json*/ "true")]
    [InlineData(/*lang=json*/ "false")]
    [InlineData(/*lang=json*/ "42")]
    [InlineData(/*lang=json*/ """ "foobar" """)]
    [InlineData(/*lang=json*/ "{}")]
    public void Read_With_Invalid_Input_Is_An_Error(string json)
    {
        var subject = new ArrayReadStateMachine();
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

        var result = subject.Read(ref reader);

        Assert.Equal(ReadResult.Error, result);
        Assert.Equal(State.Error, subject.CurrentState);
    }

    [Fact]
    public void Read_Throws_When_State_Machine_Is_In_Error_State()
    {
        var subject = new ArrayReadStateMachine();
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes("{"));

        _ = subject.Read(ref reader);

        Assert.Equal(State.Error, subject.CurrentState);

        var ex = CatchExceptionForAssertion(reader, r => _ = subject.Read(ref r));
        _ = Assert.IsType<InvalidOperationException>(ex);
    }

    [Fact]
    public void Read_Throws_When_State_Machine_Is_In_Done_State()
    {
        var subject = new ArrayReadStateMachine();
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes("[]"));

        _ = subject.Read(ref reader);

        Assert.Equal(State.Done, subject.CurrentState);

        var ex = CatchExceptionForAssertion(reader, r => _ = subject.Read(ref r));
        _ = Assert.IsType<InvalidOperationException>(ex);
    }

    [Fact]
    public void Read_Increments_CurrentItemLoopCount_When_Current_State_Is_PendingItemRead()
    {
        var subject = new ArrayReadStateMachine();
        var reader = new Utf8JsonReader("[{"u8, isFinalBlock: false, new());

        foreach (var i in Enumerable.Range(0, 10))
        {
            var result = subject.Read(ref reader);

            Assert.Equal(ReadResult.Item, result);
            Assert.Equal(State.PendingItemRead, subject.CurrentState);
            Assert.Equal(0, subject.CurrentLength);
            Assert.Equal(i, subject.CurrentItemLoopCount);
        }
    }

    public static readonly TheoryData<string[], (ReadResult, int, State)[]> Read_Reads_Array_Data =
        new()
        {
            {
                Array.Empty<string>(),
                new[]
                {
                    (ReadResult.Incomplete, 0, State.Initial),
                }
            },
            {
                new[] { "[" },
                new[]
                {
                    (ReadResult.Incomplete, 0, State.ItemOrEnd),
                }
            },
            {
                new[] { "[", "]" },
                new[]
                {
                    (ReadResult.Incomplete, 0, State.ItemOrEnd),
                    (ReadResult.Done, 0, State.Done)
                }
            },
            {
                new[] { "[]" },
                new[]
                {
                    (ReadResult.Done, 0, State.Done)
                }
            },
            {
                new[] { "[", "null", "]" },
                new[]
                {
                    (ReadResult.Incomplete, 0, State.ItemOrEnd),
                    (ReadResult.Item, 1, State.PendingItemRead),
                    (ReadResult.Done, 1, State.Done)
                }
            },
            {
                new[] { "[", "nu", "ll", "]" },
                new[]
                {
                    (ReadResult.Incomplete, 0, State.ItemOrEnd),
                    (ReadResult.Incomplete, 0, State.ItemOrEnd),
                    (ReadResult.Item, 1, State.PendingItemRead),
                    (ReadResult.Done, 1, State.Done)
                }
            },
            {
                new[] { "[", "n", "u", "l", "l", "]" },
                new[]
                {
                    (ReadResult.Incomplete, 0, State.ItemOrEnd),
                    (ReadResult.Incomplete, 0, State.ItemOrEnd),
                    (ReadResult.Incomplete, 0, State.ItemOrEnd),
                    (ReadResult.Incomplete, 0, State.ItemOrEnd),
                    (ReadResult.Item, 1, State.PendingItemRead),
                    (ReadResult.Done, 1, State.Done)
                }
            },
            {
                new[] { "[", "nu", "ll]" },
                new[]
                {
                    (ReadResult.Incomplete, 0, State.ItemOrEnd),
                    (ReadResult.Incomplete, 0, State.ItemOrEnd),
                    (ReadResult.Item, 1, State.PendingItemRead),
                    (ReadResult.Done, 1, State.Done)
                }
            },
            {
                new[] { "[", "123", "]" },
                new[]
                {
                    (ReadResult.Incomplete, 0, State.ItemOrEnd),
                    (ReadResult.Incomplete, 0, State.ItemOrEnd),
                    (ReadResult.Item, 1, State.PendingItemRead),
                    (ReadResult.Done, 1, State.Done)
                }
            },
            {
                new[] { "[", "null", ",", "true", ", false", "]" },
                new[]
                {
                    (ReadResult.Incomplete, 0, State.ItemOrEnd),
                    (ReadResult.Item, 1, State.PendingItemRead),
                    (ReadResult.Incomplete, 0, State.ItemOrEnd),
                    (ReadResult.Item, 2, State.PendingItemRead),
                    (ReadResult.Item, 3, State.PendingItemRead),
                    (ReadResult.Done, 3, State.Done)
                }
            },
            {
                new[] { "[[]]" },
                new[]
                {
                    (ReadResult.Item, 1, State.PendingItemRead),
                    (ReadResult.Done, 1, State.Done)
                }
            },
            {
                new[] { "[[], {}]" },
                new[]
                {
                    (ReadResult.Item, 1, State.PendingItemRead),
                    (ReadResult.Item, 2, State.PendingItemRead),
                    (ReadResult.Done, 2, State.Done)
                }
            },
            {
                new[] { "[[], ", /*lang=json*/"""{ "x": 123, "y": 456 }""", "]"},
                new[]
                {
                    (ReadResult.Item, 1, State.PendingItemRead),
                    (ReadResult.Item, 2, State.PendingItemRead),
                    (ReadResult.Done, 2, State.Done)
                }
            },
        };

    [Theory]
    [MemberData(nameof(Read_Reads_Array_Data))]
    public void Read_Reads_Array(string[] chunks, (ReadResult, int, State)[] expectations)
    {
        var subject = new ArrayReadStateMachine();
        var jsonReaderState = new JsonReaderState();

        var chunkRun = string.Empty;
        foreach (var (thisChunk, (expectedResult, expectedLength, expectedState)) in
                 chunks.ZipLongest(expectations, ValueTuple.Create))
        {
            var chunk = chunkRun + thisChunk;
            var chunkSpan = Encoding.UTF8.GetBytes(chunk).AsSpan();
            var reader = new Utf8JsonReader(chunkSpan, false, jsonReaderState);
            var result = subject.Read(ref reader);

            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedState, subject.CurrentState);

            if (result is ReadResult.Item)
            {
                var read = reader.Read();
                Assert.True(read);

                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    var skipped = reader.TrySkip();
                    Assert.True(skipped);
                }

                subject.OnItemRead();
                Assert.Equal(expectedLength, subject.CurrentLength);
                Assert.Equal(State.ItemOrEnd, subject.CurrentState);
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
