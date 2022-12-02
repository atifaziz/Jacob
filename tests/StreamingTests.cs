// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace Jacob.Tests.Streaming;

using Xunit.Abstractions;

public sealed class TinyBufferSizeTests : JsonReaderTestsBase
{
    public TinyBufferSizeTests(ITestOutputHelper testOutputHelper)
        : base(new StreamingTestExecutor(2, testOutputHelper)) { }
}

public sealed class ExtraSmallBufferSizeTests : JsonReaderTestsBase
{
    public ExtraSmallBufferSizeTests(ITestOutputHelper testOutputHelper)
        : base(new StreamingTestExecutor(5, testOutputHelper)) { }
}

public sealed class SmallBufferSizeTests : JsonReaderTestsBase
{
    public SmallBufferSizeTests(ITestOutputHelper testOutputHelper)
        : base(new StreamingTestExecutor(10, testOutputHelper)) { }
}
