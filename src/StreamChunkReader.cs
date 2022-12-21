// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Jacob;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

sealed class StreamChunkReader : IDisposable
{
    readonly Stream stream;
    readonly bool doesNotOwnStream;
    byte[] buffer;
    ReadOnlyMemory<byte> memory;
    ReadOnlyMemory<byte> chunk;

    public StreamChunkReader(Stream stream, int initialBufferSize, bool doesNotOwnStream = false)
    {
        this.stream = stream;
        this.buffer =
            new byte[initialBufferSize switch
            {
                < 0 => throw new ArgumentOutOfRangeException(nameof(initialBufferSize), initialBufferSize, null),
                0 => 1024,
                var size => size
            }];
        this.memory = this.chunk = null;
        this.doesNotOwnStream = doesNotOwnStream;
    }

    public bool Eof { get; private set; }

    public int TotalConsumedLength { get; private set; }
    public int ConsumedChunkLength { get; private set; }

    public ReadOnlySpan<byte> RemainingChunkSpan => this.chunk.Span;

    public void ConsumeChunkBy(int count)
    {
        this.chunk = this.chunk[count..];
        TotalConsumedLength += count;
        ConsumedChunkLength += count;
    }

    public async ValueTask ReadAsync(CancellationToken cancellationToken)
    {
        var bytesConsumed = ConsumedChunkLength;

        // |-- buffer ----------------------------------------|
        // |-- memory --------------------------|
        // |<- bytesConsumed ->|<- restLength ->|

        Memory<byte> readMemory;
        var restLength = this.memory.Length - bytesConsumed;
        if (restLength > 0)
        {
            // |-- buffer ----------------------------------------|
            // |-- memory --------------------------|
            // |<- bytesConsumed ->|<- restLength ->|
            //                     |-- rest --------|

            ReadOnlyMemory<byte> rest = this.buffer.AsMemory(bytesConsumed, restLength);

            if (rest.Length == this.buffer.Length)
                Array.Resize(ref this.buffer, this.buffer.Length * 2);

            // |-- buffer ----------------------------------------|
            // |-- memory --------------------------|
            // |<- bytesConsumed ->|<- restLength ->|
            //                     |-- rest --------|
            //                           |
            // |-- (rest) ------| <<- (CopyTo)

            rest.CopyTo(this.buffer);

            // |-- buffer ----------------------------------------|
            // |-- memory --------------------------|
            // |<- bytesConsumed ->|<- restLength ->|
            //                     |-- rest --------|
            // |-- rest --------|
            // |<- restLength ->|
            //                  |-- readMemory -------------------|

            readMemory = this.buffer.AsMemory(rest.Length);

            // (cont'd)...
        }
        else
        {
            // |-- buffer ----------------------------------------|
            // |-- readMemory ------------------------------------|

            readMemory = this.buffer;
        }

        var actualReadLength = await this.stream.ReadAsync(readMemory, cancellationToken).ConfigureAwait(false);
        Eof = !Eof && actualReadLength == 0;

        // ...(cont'd)
        //
        // |-- buffer ----------------------------------------|
        // |-- memory --------------------------|
        // |<- bytesConsumed ->|<- restLength ->|
        // |-- rest --------|
        //                  |-- readMemory -------------------|
        // |<- restLength ->|<-- actualReadLength -->|
        // |-- memory -------------------------------|

        ConsumedChunkLength = 0;
        this.chunk = this.memory = this.buffer.AsMemory(0, restLength + actualReadLength);
    }

    public void Dispose()
    {
        if (!this.doesNotOwnStream)
            this.stream.Dispose();
    }
}
