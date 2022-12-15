// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Jacob.Tests;

using System;
using System.Linq;
using System.Text;
using Xunit;
using ReadResult = ObjectReadStateMachine.ReadResult;
using State = ObjectReadStateMachine.State;

public sealed class ObjectReadStateMachineTests
{
    [Fact]
    public void Default_Instance_Is_Initialized()
    {
        var subject = new ObjectReadStateMachine();
        Assert.Equal(State.Initial, subject.CurrentState);
        Assert.Equal(0, subject.CurrentPropertyLoopCount);
    }

    [Fact]
    public void OnPropertyNameRead_Throws_When_CurrentState_Is_Not_PendingPropertyNameRead()
    {
        var subject = new ObjectReadStateMachine();
        _ = Assert.Throws<InvalidOperationException>(subject.OnPropertyNameRead);
    }

    [Fact]
    public void OnPropertyValueRead_Throws_When_CurrentState_Is_Not_PendingPropertyValueRead()
    {
        var subject = new ObjectReadStateMachine();
        _ = Assert.Throws<InvalidOperationException>(subject.OnPropertyValueRead);
    }

    [Theory]
    [InlineData(/*lang=json*/ "null")]
    [InlineData(/*lang=json*/ "true")]
    [InlineData(/*lang=json*/ "false")]
    [InlineData(/*lang=json*/ "42")]
    [InlineData(/*lang=json*/ """ "foobar" """)]
    [InlineData(/*lang=json*/ "[]")]
    public void Read_With_Invalid_Input_Is_An_Error(string json)
    {
        var subject = new ObjectReadStateMachine();
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

        var result = subject.Read(ref reader);

        Assert.Equal(ReadResult.Error, result);
        Assert.Equal(State.Error, subject.CurrentState);
    }

    [Fact]
    public void Read_Throws_When_State_Machine_Is_In_Error_State()
    {
        var subject = new ObjectReadStateMachine();
        var reader = new Utf8JsonReader("["u8);

        _ = subject.Read(ref reader);

        Assert.Equal(State.Error, subject.CurrentState);

        var ex = CatchExceptionForAssertion(reader, r => _ = subject.Read(ref r));
        _ = Assert.IsType<InvalidOperationException>(ex);
    }

    [Fact]
    public void Read_Throws_When_State_Machine_Is_In_Done_State()
    {
        var subject = new ObjectReadStateMachine();
        var reader = new Utf8JsonReader("{}"u8);

        _ = subject.Read(ref reader);

        Assert.Equal(State.Done, subject.CurrentState);

        var ex = CatchExceptionForAssertion(reader, r => _ = subject.Read(ref r));
        _ = Assert.IsType<InvalidOperationException>(ex);
    }

    [Fact]
    public void Read_Increments_CurrentPropertyLoopCount_When_Current_State_Is_PendingPropertyNameRead()
    {
        var subject = new ObjectReadStateMachine();
        var reader = new Utf8JsonReader("{\"foo\":"u8, isFinalBlock: false, new());

        foreach (var i in Enumerable.Range(0, 10))
        {
            var result = subject.Read(ref reader);

            Assert.Equal(ReadResult.PropertyName, result);
            Assert.Equal(State.PendingPropertyNameRead, subject.CurrentState);
            Assert.Equal(i, subject.CurrentPropertyLoopCount);
        }
    }

    [Fact]
    public void Read_Increments_CurrentPropertyLoopCount_When_Current_State_Is_PendingPropertyValueRead()
    {
        var subject = new ObjectReadStateMachine();
        var reader = new Utf8JsonReader("{\"foo\":4"u8, isFinalBlock: false, new());

        _ = subject.Read(ref reader);
        subject.OnPropertyNameRead();

        foreach (var i in Enumerable.Range(0, 10))
        {
            var result = subject.Read(ref reader);

            Assert.Equal(ReadResult.PropertyValue, result);
            Assert.Equal(State.PendingPropertyValueRead, subject.CurrentState);
            Assert.Equal(i, subject.CurrentPropertyLoopCount);
        }
    }

    delegate void ReaderAction(Utf8JsonReader reader);

    static Exception? CatchExceptionForAssertion(Utf8JsonReader reader, ReaderAction action)
    {
        try
        {
            action(reader);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return ex;
        }

        return null;
    }
}
