// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Jacob.Tests;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

sealed class StreamChunkReader : IDisposable
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

    public bool Eof { get; private set; }

    public async ValueTask<ReadOnlyMemory<byte>> ReadAsync(int bytesConsumed, CancellationToken cancellationToken)
    {
        Memory<byte> readMemory;
        var restLength = this.memory.Length - bytesConsumed;
        if (restLength > 0)
        {
            ReadOnlyMemory<byte> rest = this.buffer.AsMemory(bytesConsumed, restLength);

            if (rest.Length == this.buffer.Length)
                Array.Resize(ref this.buffer, this.buffer.Length * 2);

            rest.CopyTo(this.buffer);
            readMemory = this.buffer.AsMemory(rest.Length);
        }
        else
        {
            readMemory = this.buffer;
        }

        var actualReadLength = await this.stream.ReadAsync(readMemory, cancellationToken).ConfigureAwait(false);
        Eof = !Eof && actualReadLength == 0;
        return this.memory = this.buffer.AsMemory(0, restLength + actualReadLength);
    }

    public void Dispose() => this.stream.Dispose();
}