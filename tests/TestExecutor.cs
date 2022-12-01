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

    record struct TokenState(System.Text.Json.JsonTokenType TokenType, long TokenStartIndex);

    public StreamingTestExecutor(int bufferSize, ITestOutputHelper testOutputHelper) =>
        (this.bufferSize, this.testOutputHelper) = (bufferSize, testOutputHelper);

    public JsonReadResult<T> TryRead<T>(IJsonReader<T> jsonReader, string json)
    {
        var (result, _) = TryReadInner(jsonReader, json);
        return result;
    }

    public T Read<T>(IJsonReader<T> jsonReader, string json) =>
        TryReadInner(jsonReader, json) switch
        {
            ((_, { } message), var tokenState) => throw new System.Text.Json.JsonException($@"{message} See token ""{tokenState.TokenType}"" at offset {tokenState.TokenStartIndex}."),
            var ((value, _), _) => value,
        };

    (JsonReadResult<T>, TokenState) TryReadInner<T>(IJsonReader<T> jsonReader, string json)
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

                var (jsonReadResult, tokenState) = Read();

                if (!jsonReadResult.Incomplete)
                    return (jsonReadResult, tokenState);

                (JsonReadResult<T>, TokenState) Read()
                {
                    var reader = new Utf8JsonReader(memory.Span, r.Eof, state);
                    var chunk = Encoding.UTF8.GetString(memory.Span);
                    WriteLine(new { Buffer = $"<{chunk}>", memory.Length });
                    jsonReadResult = jsonReader.TryRead(ref reader);
                    WriteLine($"BytesConsumed = {reader.BytesConsumed}");
                    bytesConsumed = (int)reader.BytesConsumed;
                    state = reader.CurrentState;
                    return (jsonReadResult, new TokenState(reader.TokenType, totalBytesConsumed + reader.TokenStartIndex));
                }
            }
        }
        catch (NotSupportedException)
        {
            return TryReadInner(jsonReader.Buffer(), json);
        }
    }

    void WriteLine(object? value) => this.testOutputHelper.WriteLine(value?.ToString());
}
