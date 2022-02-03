// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace JsonR.Tests;

using Xunit;

public class JsonReadErrorTests
{
    [Fact]
    public void Constructor_Initializes_Message()
    {
        const string message = "oops";
        var error = new JsonReadError(message);
        Assert.Equal(message, error.Message);
    }

    [Fact]
    public void ToString_Returns_Message()
    {
        const string message = "oops";
        var error = new JsonReadError(message);
        Assert.Equal(message, error.ToString());
    }
}
