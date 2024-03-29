// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace Jacob.Tests.Streaming;

using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System;
using MoreLinq;
using Xunit;
using Xunit.Abstractions;
using JsonTokenType = System.Text.Json.JsonTokenType;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

public sealed class StreamingTests
{
    static readonly object NullObject = new();

    static readonly IJsonReader<string> NestedObjectReader =
        JsonReader.Object(
            JsonReader.Property(
                "prop1",
                JsonReader.Object(
                    JsonReader.Property("prop2", JsonReader.String()))));

    public static TheoryData<IJsonReader<object>, string, int, int[]> Buffer_TheoryData() => new()
    {
        { JsonReader.Null(NullObject), /*lang=json*/ "null", 2, new[] { 0, 4 } },
        { JsonReader.Boolean().AsObject(), /*lang=json*/ "false", 2, new[] { 0, 0, 5 } },
        { JsonReader.Boolean().AsObject(), /*lang=json*/ "true", 2, new[] { 0, 4 } },
        { JsonReader.Boolean().AsObject(), /*lang=json*/ "true", 5, new[] { 4 } },
        { JsonReader.Int32().AsObject(), /*lang=json*/ "12", 2, new[] { 0, 2 } },
        { JsonReader.Int32().AsObject(), /*lang=json*/ "12", 5, new[] { 0, 2 } },
        { JsonReader.String().AsObject(), /*lang=json*/ """ "foo" """, 2, new[] { 1, 0, 0, 5 } },
        { JsonReader.String().AsObject(), /*lang=json*/ """ "foo" """, 5, new[] { 1, 5 } },
        { JsonReader.String().AsObject(), /*lang=json*/ """ "foo" """, 10, new[] { 6 } },
        { NestedObjectReader.AsObject(), /*lang=json*/ """{ "prop1": { "prop2": "foo" } }""", 2, new[] { 1, 0, 0, 0, 0, 30 } },
        { NestedObjectReader.AsObject(), /*lang=json*/ """{ "prop1": { "prop2": "foo" } }""", 5, new[] { 1, 0, 0, 0, 30 } },
        { NestedObjectReader.AsObject(), /*lang=json*/ """{ "prop1": { "prop2": "foo" } }""", 10, new[] { 1, 0, 0, 30 } },
        { JsonReader.Array(JsonReader.String()).AsObject(), /*lang=json*/ """["foo", "bar", "baz"]""", 2, new[] { 1, 0, 0, 0, 0, 20 } },
        { JsonReader.Array(JsonReader.String()).AsObject(), /*lang=json*/ """["foo", "bar", "baz"]""", 5, new[] { 1, 0, 0, 20 } },
        { JsonReader.Array(JsonReader.String()).AsObject(), /*lang=json*/ """["foo", "bar", "baz"]""", 10, new[] { 1, 0, 20 } },
    };

    [Theory]
    [MemberData(nameof(Buffer_TheoryData))]
    public void Buffer_Expands_Correctly(IJsonReader<object> jsonReader, string json, int bufferSize, int[] bytesConsumedExpectations)
    {
        var bufferedReader = jsonReader.Buffer();
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var r = new StreamChunkReader(ms, bufferSize);
        var state = new JsonReaderState();

        foreach (var (expectedBytesConsumed, _, isLast) in bytesConsumedExpectations.TagFirstLast(ValueTuple.Create))
        {
            var readTask = r.ReadAsync(CancellationToken.None);
            Debug.Assert(readTask.IsCompleted);
            var reader = new Utf8JsonReader(r.RemainingChunkSpan, r.Eof, state);
            var readResult = bufferedReader.TryRead(ref reader);
            var bytesConsumed = (int)reader.BytesConsumed;
            Assert.Equal(expectedBytesConsumed, bytesConsumed);
            r.ConsumeChunkBy(bytesConsumed);
            state = reader.CurrentState;
            Assert.Equal(!isLast, readResult.Incomplete);
        }

        var result = bufferedReader.TryRead(json);
        Assert.False(result.Incomplete);
    }
}

public abstract class StreamingTestsBase : JsonReaderTestsBase
{
    readonly int bufferSize;
    readonly ITestOutputHelper testOutputHelper;

    protected StreamingTestsBase(int bufferSize, ITestOutputHelper testOutputHelper) =>
        (this.bufferSize, this.testOutputHelper) = (bufferSize, testOutputHelper);

    protected override JsonReadResult<T> TryRead<T>(IJsonReader<T> jsonReader, string json) =>
        TryReadCore(jsonReader, json).Result;

    protected override T Read<T>(IJsonReader<T> jsonReader, string json) =>
        TryReadCore(jsonReader, json) switch
        {
            ({ Error: { } error }, _, _) r => throw new JsonException($@"{error} See token ""{r.TokenType}"" at offset {r.TokenStartIndex}."),
            ({ Value: var value, Error: null }, _, _) => value,
        };

    (JsonReadResult<T> Result, JsonTokenType TokenType, long TokenStartIndex)
        TryReadCore<T>(IJsonReader<T> jsonReader, string json)
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var r = new StreamChunkReader(ms, this.bufferSize);
        var state = new JsonReaderState();

        while (true)
        {
            var readTask = r.ReadAsync(CancellationToken.None);
            Debug.Assert(readTask.IsCompleted);

            var span1 = r.RemainingChunkSpan;
            var tokenStartBaseIndex = r.TotalConsumedLength;
            var reader = new Utf8JsonReader(span1, r.Eof, state);
            var readResult = jsonReader.TryRead(ref reader);
            var bytesConsumed = (int)reader.BytesConsumed;
            r.ConsumeChunkBy(bytesConsumed);
            WriteLine($"Buffer[{span1.Length}] = <{Printable(span1)}>, Consumed[{bytesConsumed}] = <{Printable(span1[..bytesConsumed])}>");
            state = reader.CurrentState;
            if (readResult is { Incomplete: false } completedReadResult)
                return (completedReadResult, reader.TokenType, tokenStartBaseIndex + reader.TokenStartIndex);
        }

        static string Printable(ReadOnlySpan<byte> span)
        {
            var json = JsonSerializer.Serialize(Encoding.UTF8.GetString(span));
            return json[1..^1].Replace(@"\u0022", "\"", StringComparison.Ordinal);
        }
    }

    void WriteLine(object? value) => this.testOutputHelper.WriteLine(value?.ToString());
}

public sealed class TinyBufferSizeTests : StreamingTestsBase
{
    public TinyBufferSizeTests(ITestOutputHelper testOutputHelper)
        : base(2, testOutputHelper) { }
}

public sealed class ExtraSmallBufferSizeTests : StreamingTestsBase
{
    public ExtraSmallBufferSizeTests(ITestOutputHelper testOutputHelper)
        : base(5, testOutputHelper) { }
}

public sealed class SmallBufferSizeTests : StreamingTestsBase
{
    public SmallBufferSizeTests(ITestOutputHelper testOutputHelper)
        : base(10, testOutputHelper) { }
}
