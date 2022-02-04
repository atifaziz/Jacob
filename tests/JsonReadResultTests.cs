// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Jacob.Tests;

using Xunit;

public class JsonReadResultTests
{
    [Fact]
    public void Value_Represents_Value_Result()
    {
        var (value, error) = JsonReadResult.Value(42);
        Assert.Null(error);
        Assert.Equal(42, value);
    }

    [Fact]
    public void Converts_JsonReadError_To_Error_Result()
    {
        var error = new JsonReadError("oops");
        JsonReadResult<int> result = error;
        Assert.NotNull(result.Error);
        Assert.Same(error.Message, result.Error);
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public void ToString_With_Value_Result_Returns_Value_Prefixed_String()
    {
        var str = JsonReadResult.Value(42).ToString();
        Assert.Equal("Value: 42", str);
    }

    [Fact]
    public void ToString_With_Error_Result_Returns_Error_Prefixed_String()
    {
        var error = new JsonReadError("oops");
        JsonReadResult<int> result = error;
        var str = result.ToString();
        Assert.Equal("Error: oops", str);
    }
}
