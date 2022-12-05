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
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using JsonTokenType = System.Text.Json.JsonTokenType;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

public sealed class StreamingTests
{
    static readonly IJsonReader<object> NestedObjectReader =
        JsonReader.Object(
            JsonReader.Property(
                "prop1",
                JsonReader.Object(
                    JsonReader.Property("prop2", JsonReader.String().AsObject()))));

    public static TheoryData<IJsonReader<object>, string, int, int[]> Buffer_TheoryData() => new()
    {
        { JsonReader.Null((object?)null).AsObject(), /*lang=json*/"null", 2, new[] { 0, 4 } },
        { JsonReader.Boolean().AsObject(), /*lang=json*/"false", 2, new[] { 0, 0, 5 } },
        { JsonReader.Boolean().AsObject(), /*lang=json*/"true", 2, new[] { 0, 4 } },
        { JsonReader.Boolean().AsObject(), /*lang=json*/"true", 5, new[] { 4 } },
        { JsonReader.Int32().AsObject(), /*lang=json*/"12", 2, new[] { 0, 2 } },
        { JsonReader.Int32().AsObject(), /*lang=json*/"12", 5, new[] { 0, 2 } },
        { JsonReader.String().AsObject(), /*lang=json*/""" "foo" """, 2, new[] { 0, 0, 6 } },
        { JsonReader.String().AsObject(), /*lang=json*/""" "foo" """, 5, new[] { 0, 6 } },
        { JsonReader.String().AsObject(), /*lang=json*/""" "foo" """, 10, new[] { 6 } },
        { NestedObjectReader, /*lang=json*/""" {"prop1":{"prop2":"foo"}} """, 2, new[] { 0, 0, 0, 0, 26 } },
        { NestedObjectReader, /*lang=json*/""" {"prop1":{"prop2":"foo"}} """, 5, new[] { 0, 0, 0, 26 } },
        { NestedObjectReader, /*lang=json*/""" {"prop1":{"prop2":"foo"}} """, 10, new[] { 0, 0, 26 } },
        { JsonReader.Array(JsonReader.String()).AsObject(), /*lang=json*/""" ["foo","bar","baz"] """, 2, new[] { 0, 0, 0, 0, 20 } },
        { JsonReader.Array(JsonReader.String()).AsObject(), /*lang=json*/""" ["foo","bar","baz"] """, 5, new[] { 0, 0, 20 } },
        { JsonReader.Array(JsonReader.String()).AsObject(), /*lang=json*/""" ["foo","bar","baz"] """, 10, new[] { 0, 20 } },
    };

    [Theory]
    [MemberData(nameof(Buffer_TheoryData))]
    public void Buffer_Expands_Correctly(IJsonReader<object> jsonReader, string json, int bufferSize, int[] expectedBytesConsumed)
    {
        var bufferedReader = jsonReader.Buffer();
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var r = new StreamChunkReader(ms, bufferSize);
        var state = new JsonReaderState();

        foreach (var (expectedBytesCons, isLast) in expectedBytesConsumed.Select((e, i) => (e, i + 1 == expectedBytesConsumed.Length)))
        {
            var readTask = r.ReadAsync(CancellationToken.None);
            Debug.Assert(readTask.IsCompleted);
            Assert.Equal(!isLast, Read(r.RemainingChunkSpan).Incomplete);

            JsonReadResult<object> Read(ReadOnlySpan<byte> span)
            {
                var reader = new Utf8JsonReader(span, r.Eof, state);
                var readResult = bufferedReader.TryRead(ref reader);
                var bytesConsumed = (int)reader.BytesConsumed;
                Assert.Equal(expectedBytesCons, bytesConsumed);
                r.ConsumeChunkBy(bytesConsumed);
                state = reader.CurrentState;
                return readResult;
            }
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
        try
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            using var r = new StreamChunkReader(ms, this.bufferSize);
            var state = new JsonReaderState();

            while (true)
            {
                var readTask = r.ReadAsync(CancellationToken.None);
                Debug.Assert(readTask.IsCompleted);

                if (Read(r.RemainingChunkSpan) is ({ Incomplete: false }, _, _) result)
                    return result;

                (JsonReadResult<T>, JsonTokenType, long) Read(ReadOnlySpan<byte> span)
                {
                    var tokenStartBaseIndex = r.TotalConsumedLength;
                    var reader = new Utf8JsonReader(span, r.Eof, state);
                    var readResult = jsonReader.TryRead(ref reader);
                    var bytesConsumed = (int)reader.BytesConsumed;
                    r.ConsumeChunkBy(bytesConsumed);
                    WriteLine($"Buffer[{span.Length}] = <{Printable(span)}>, Consumed[{bytesConsumed}] = <{Printable(span[..bytesConsumed])}>");
                    state = reader.CurrentState;
                    return (readResult, reader.TokenType, tokenStartBaseIndex + reader.TokenStartIndex);
                }
            }
        }
        catch (NotSupportedException)
        {
            return TryReadCore(jsonReader.Buffer(), json);
        }

        string Printable(ReadOnlySpan<byte> span)
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
