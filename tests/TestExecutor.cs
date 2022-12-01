// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Jacob.Tests;

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Xunit.Abstractions;
using JsonTokenType = System.Text.Json.JsonTokenType;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

interface ITestExecutor
{
    JsonReadResult<T> TryRead<T>(IJsonReader<T> jsonReader, string json);

    T Read<T>(IJsonReader<T> jsonReader, string json);
}

sealed class DefaultTestExecutor : ITestExecutor
{
    public T Read<T>(IJsonReader<T> jsonReader, string json) => jsonReader.Read(json);

    public JsonReadResult<T> TryRead<T>(IJsonReader<T> jsonReader, string json) => jsonReader.TryRead(json);
}

sealed class StreamingTestExecutor : ITestExecutor
{
    readonly int bufferSize;
    readonly ITestOutputHelper testOutputHelper;

    public StreamingTestExecutor(int bufferSize, ITestOutputHelper testOutputHelper) =>
        (this.bufferSize, this.testOutputHelper) = (bufferSize, testOutputHelper);

    public JsonReadResult<T> TryRead<T>(IJsonReader<T> jsonReader, string json) =>
        TryReadCore(jsonReader, json).Result;

    public T Read<T>(IJsonReader<T> jsonReader, string json) =>
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
            var totalBytesConsumed = 0;
            var bytesConsumed = 0;
            var state = new JsonReaderState();

            while (true)
            {
                totalBytesConsumed += bytesConsumed;
                var readTask = r.ReadAsync(bytesConsumed, CancellationToken.None);
                Debug.Assert(readTask.IsCompleted);
                var memory = readTask.Result;

                if (Read(memory.Span) is ({ Incomplete: false }, _, _) result)
                    return result;

                (JsonReadResult<T>, JsonTokenType, long) Read(ReadOnlySpan<byte> span)
                {
                    var reader = new Utf8JsonReader(span, r.Eof, state);
                    var readResult = jsonReader.TryRead(ref reader);
                    bytesConsumed = (int)reader.BytesConsumed;
                    WriteLine($"Buffer[{span.Length}] = <{Printable(span)}>, Consumed[{bytesConsumed}] = <{Printable(span[..bytesConsumed])}>");
                    state = reader.CurrentState;
                    return (readResult, reader.TokenType, totalBytesConsumed + reader.TokenStartIndex);
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
