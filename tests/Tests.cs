// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Jacob.Tests;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

public class PartialReadTests
{
    readonly ITestOutputHelper testOutputHelper;

    public PartialReadTests(ITestOutputHelper testOutputHelper) => this.testOutputHelper = testOutputHelper;

    void WriteLine(object? value) => this.testOutputHelper.WriteLine(value?.ToString());

    [Theory]
    [InlineData(10)]
    [InlineData(5)]
    [InlineData(2)]
    public void TestArrayOfBoolean(int bufferSize)
    {
        const string json = /*lang=json*/ "[true, false, true]";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
        JsonReadResult<bool[]> array;
        var buffer = new byte[bufferSize];
        var span = buffer.AsSpan();
        _ = ms.Read(span);
        var reader = new Utf8JsonReader(span, isFinalBlock: false, default);
        var jsonReader = JsonReader.Array(JsonReader.Boolean());
        while (true)
        {
            var chunk = Encoding.UTF8.GetString(buffer);
            WriteLine(new { Buffer = $"<{chunk}>", buffer.Length });
            array = jsonReader.TryRead(ref reader);
            WriteLine($"BytesConsumed = {reader.BytesConsumed}");
            if (!array.Incomplete)
                break;
            GetMoreBytesFromStream(ms, ref buffer, ref span, ref reader);
        }
        Assert.Null(array.Error);
        Assert.Equal(new[] { true, false, true }, array.Value);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(5)]
    [InlineData(2)]
    public void TestStringArray(int bufferSize)
    {
        const string json = /*lang=json*/ """
                            [ "foo", "bar", "baz" ]
                            """;

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
        JsonReadResult<string[]> array;
        var buffer = new byte[bufferSize];
        var span = buffer.AsSpan();
        _ = ms.Read(span);
        var reader = new Utf8JsonReader(span, isFinalBlock: false, default);
        var jsonReader = JsonReader.Array(JsonReader.String());
        while (true)
        {
            WriteLine(new { Buffer = $"<{Encoding.UTF8.GetString(buffer)}>", buffer.Length });
            array = jsonReader.TryRead(ref reader);
            WriteLine($"BytesConsumed = {reader.BytesConsumed}");
            if (!array.Incomplete)
                break;
            GetMoreBytesFromStream(ms, ref buffer, ref span, ref reader);
        }
        Assert.Null(array.Error);
        Assert.Equal(new[] { "foo", "bar", "baz" }, array.Value);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(5)]
    [InlineData(2)]
    public void TestArrayOfStringArray(int bufferSize)
    {
        const string json = /*lang=json*/ """
            [
                [ "123", "456", "789"],
                [ "foo", "bar", "baz" ],
                ["big", "fan", "run"]
            ]
            """;

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
        JsonReadResult<string[][]> array;
        var buffer = new byte[bufferSize];
        var span = buffer.AsSpan();
        _ = ms.Read(span);
        var reader = new Utf8JsonReader(span, isFinalBlock: false, default);
        var jsonReader = JsonReader.Array(JsonReader.Array(JsonReader.String()));
        while (true)
        {
            var chunk = Encoding.UTF8.GetString(buffer);
            WriteLine(new { Buffer = $"<{chunk}>", buffer.Length });
            array = jsonReader.TryRead(ref reader);
            WriteLine($"BytesConsumed = {reader.BytesConsumed}");
            if (!array.Incomplete)
                break;
            GetMoreBytesFromStream(ms, ref buffer, ref span, ref reader);
        }
        Assert.Null(array.Error);
        Assert.Equal(new[] { new[] { "123", "456", "789" }, new[] { "foo", "bar", "baz" }, new[] { "big", "fan", "run" } }, array.Value);
    }

    [Theory]
    [InlineData(10, /*lang=json*/ @"[""123"", ""456"", ""789""]", new[] { "123", "456", "789" })]
    [InlineData(5, /*lang=json*/ @"[""123"", ""456"", ""789""]", new[] { "123", "456", "789" })]
    [InlineData(2, /*lang=json*/ @"[""123"", ""456"", ""789""]", new[] { "123", "456", "789" })]
    [InlineData(10, /*lang=json*/ "[true, false]", new[] { true, false })]
    [InlineData(5, /*lang=json*/ "[true, false]", new[] { true, false })]
    [InlineData(2, /*lang=json*/ "[true, false]", new[] { true, false })]
    public void TestArrayOfEitherStringOrBoolean(int bufferSize, string json, object expected)
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
        JsonReadResult<object[]> array;
        var buffer = new byte[bufferSize];
        var span = buffer.AsSpan();
        _ = ms.Read(span);
        var reader = new Utf8JsonReader(span, false, default);
        var jsonReader = JsonReader.Array(JsonReader.Either(JsonReader.String().AsObject(),
                                                            JsonReader.Boolean().AsObject(),
                                                            null)
                                                    .Buffer());
        while (true)
        {
            var chunk = Encoding.UTF8.GetString(buffer);
            WriteLine(new { Buffer = $"<{chunk}>", buffer.Length });
            array = jsonReader.TryRead(ref reader);
            WriteLine($"BytesConsumed = {reader.BytesConsumed}");
            if (!array.Incomplete)
                break;
            GetMoreBytesFromStream(ms, ref buffer, ref span, ref reader);
        }
        Assert.Null(array.Error);
        Assert.Equal(expected, array.Value);
    }

    static void GetMoreBytesFromStream(Stream stream, ref byte[] buffer, ref Span<byte> span, ref Utf8JsonReader reader)
    {
        int bytesRead;

        var restLength = (int)(span.Length - reader.BytesConsumed);
        if (restLength > 0)
        {
            ReadOnlySpan<byte> rest = buffer.AsSpan(^restLength..);

            if (rest.Length == buffer.Length)
                Array.Resize(ref buffer, buffer.Length * 2);

            rest.CopyTo(buffer);
            bytesRead = stream.Read(buffer.AsSpan(rest.Length));
        }
        else
        {
            bytesRead = stream.Read(buffer);
        }

        span = buffer.AsSpan(..(restLength + bytesRead));
        reader = new Utf8JsonReader(span, isFinalBlock: bytesRead == 0, reader.CurrentState);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(5)]
    [InlineData(2)]
    public async Task TestArrayOfStringArrayAsync(int bufferSize)
    {
        const string json = /*lang=json*/ """
            [
                [ "123", "456", "789"],
                [ "foo", "bar", "baz" ],
                ["big", "fan", "run"]
            ]
            """;

        var jsonReader = JsonReader.Array(JsonReader.Array(JsonReader.String()));
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var r = new StreamChunkReader(ms, bufferSize);
        var bytesConsumed = 0;
        var state = new JsonReaderState();
        while (true)
        {
            var memory = await r.ReadAsync(bytesConsumed, CancellationToken.None).ConfigureAwait(false);
            var array = Read();
            if (array.Incomplete)
                continue;
            Assert.Null(array.Error);
            Assert.Equal(new[] { new[] { "123", "456", "789" }, new[] { "foo", "bar", "baz" }, new[] { "big", "fan", "run" } }, array.Value);
            break;

            JsonReadResult<string[][]> Read()
            {
                var reader = new Utf8JsonReader(memory.Span, memory.Length == 0, state);
                var chunk = Encoding.UTF8.GetString(memory.Span);
                WriteLine(new { Buffer = $"<{chunk}>", memory.Length });
                array = jsonReader.TryRead(ref reader);
                WriteLine($"BytesConsumed = {reader.BytesConsumed}");
                bytesConsumed = (int)reader.BytesConsumed;
                state = reader.CurrentState;
                return array;
            }
        }
    }
}

public sealed class StreamChunkReader : IDisposable
{
    readonly Stream stream;
    byte[] buffer;
    ReadOnlyMemory<byte> memory;

    public StreamChunkReader(Stream stream, int bufferSize)
    {
        this.stream = stream;
        this.buffer = new byte[bufferSize];
        this.memory = null;
    }

    public async ValueTask<ReadOnlyMemory<byte>> ReadAsync(int bytesConsumed, CancellationToken cancellationToken)
    {
        int bytesRead;

        var restLength = this.memory.Length - bytesConsumed;
        if (restLength > 0)
        {
            ReadOnlyMemory<byte> rest = this.buffer.AsMemory(^restLength..);

            if (rest.Length == this.buffer.Length)
                Array.Resize(ref this.buffer, this.buffer.Length * 2);

            rest.CopyTo(this.buffer);
            bytesRead = await this.stream.ReadAsync(this.buffer.AsMemory(rest.Length), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            bytesRead = await this.stream.ReadAsync(this.buffer, cancellationToken).ConfigureAwait(false);
        }

        return this.memory = this.buffer.AsMemory(..(restLength + bytesRead));
    }

    public void Dispose() => this.stream.Dispose();
}
