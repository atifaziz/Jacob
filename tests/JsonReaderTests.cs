// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Jacob.Tests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using Xunit;
using Utf8JsonReader = Utf8JsonReader;
using JsonException = System.Text.Json.JsonException;

public sealed class JsonReaderTests : JsonReaderTestsBase
{
    protected override T Read<T>(IJsonReader<T> jsonReader, string json) =>
        jsonReader.Read(json);

    protected override JsonReadResult<T> TryRead<T>(IJsonReader<T> jsonReader, string json) =>
        jsonReader.TryRead(json);
}

public abstract class JsonReaderTestsBase
{
    protected abstract JsonReadResult<T> TryRead<T>(IJsonReader<T> jsonReader, string json);

    protected abstract T Read<T>(IJsonReader<T> jsonReader, string json);

    static void TestReaderPositionPostRead<T>(IJsonReader<T> reader, string json)
    {
        var sentinel = $"END-{Guid.NewGuid()}";
        var rdr = new Utf8JsonReader(Encoding.UTF8.GetBytes($"""[{json}, "{sentinel}"]"""));
        Assert.True(rdr.Read());  // array start
        Assert.True(rdr.Read());  // position at first element
        _ = reader.Read(ref rdr); // read it
        Assert.True(rdr.Read());  // read next
        Assert.Equal(JsonTokenType.String, rdr.TokenType);
        Assert.Equal(sentinel, rdr.GetString());
    }

    void TestInvalidInput<T>(IJsonReader<T> reader, string json,
                             string expectedError, string expectedErrorToken, int expectedErrorOffset = 0)
    {
        json = json.TrimStart();

        var (value, error) = TryRead(reader, json);
        Assert.Equal(default, value);
        Assert.Equal(expectedError, error);

        var ex = Assert.Throws<JsonException>(() => Read(reader, json));
        Assert.Equal($@"{expectedError} See token ""{expectedErrorToken}"" at offset {expectedErrorOffset}.", ex.Message);
    }

    void TestValidInput<T>(IJsonReader<T> reader, string json, T expected)
    {
        var result = Read(reader, json);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void String_Moves_Reader()
    {
        TestReaderPositionPostRead(JsonReader.String(), /*lang=json*/ """ "foobar" """);
    }

    [Fact]
    public void Error_Returns_Error()
    {
        const string message = "oops";
        var reader = JsonReader.Error<string>(message);
        var utf8Reader = new Utf8JsonReader("42"u8);
        var (value, error) = reader.TryRead(ref utf8Reader);

        Assert.Equal(0, utf8Reader.BytesConsumed);
        Assert.Null(value);
        Assert.Equal(message, error);
    }

    [Theory]
    [InlineData("", /*lang=json*/ """ "" """)]
    [InlineData("foobar", /*lang=json*/ """ "foobar" """)]
    [InlineData("foo bar", /*lang=json*/ """ "foo bar" """)]
    public void String_With_Valid_Input(string expected, string json)
    {
        TestValidInput(JsonReader.String(), json, expected);
    }

    [Theory]
    [InlineData("Null", /*lang=json*/ "null")]
    [InlineData("False", /*lang=json*/ "false")]
    [InlineData("True", /*lang=json*/ "true")]
    [InlineData("Number", /*lang=json*/ "42")]
    [InlineData("StartArray", /*lang=json*/ "[]")]
    [InlineData("StartObject", /*lang=json*/ "{}")]
    public void String_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.String(), json,
                         "Invalid JSON value where a JSON string was expected.",
                         expectedErrorToken);
    }

    [Fact]
    public void Byte_Moves_Reader()
    {
        TestReaderPositionPostRead(JsonReader.Byte(), /*lang=json*/ "42");
    }

    [Theory]
    [InlineData(42, /*lang=json*/ "42")]
    public void Byte_With_Valid_Input(byte expected, string json)
    {
        TestValidInput(JsonReader.Byte(), json, expected);
    }

    [Theory]
    [InlineData("Null", /*lang=json*/ "null")]
    [InlineData("False", /*lang=json*/ "false")]
    [InlineData("True", /*lang=json*/ "true")]
    [InlineData("Number", /*lang=json*/ "-42")]
    [InlineData("Number", /*lang=json*/ "-4.2")]
    [InlineData("Number", /*lang=json*/ "256")]
    [InlineData("String", /*lang=json*/ """ "foobar" """)]
    [InlineData("StartArray", /*lang=json*/ "[]")]
    [InlineData("StartObject", /*lang=json*/ "{}")]
    public void Byte_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.Byte(), json,
                         "Invalid JSON value; expecting a JSON number compatible with Byte.", expectedErrorToken);
    }

    [Fact]
    public void Null_Moves_Reader()
    {
        TestReaderPositionPostRead(JsonReader.Null((object?)null), /*lang=json*/ "null");
    }

    [Fact]
    public void Null_With_Valid_Input()
    {
        TestValidInput(JsonReader.Null((object?)null), /*lang=json*/ "null", null);
    }

    [Theory]
    [InlineData("True", /*lang=json*/ "true")]
    [InlineData("False", /*lang=json*/ "false")]
    [InlineData("Number", /*lang=json*/ "42")]
    [InlineData("String", /*lang=json*/ """ "foobar" """)]
    [InlineData("StartArray", /*lang=json*/ "[]")]
    [InlineData("StartObject", /*lang=json*/ "{}")]
    public void Null_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.Null((object?)null), json,
                         "Invalid JSON value where a JSON null was expected.", expectedErrorToken);
    }

    [Fact]
    public void Boolean_Moves_Reader()
    {
        TestReaderPositionPostRead(JsonReader.Boolean(), /*lang=json*/ "true");
    }

    [Theory]
    [InlineData(true, /*lang=json*/ "true")]
    [InlineData(false, /*lang=json*/ "false")]
    public void Boolean_With_Valid_Input(bool expected, string json)
    {
        TestValidInput(JsonReader.Boolean(), json, expected);
    }

    [Theory]
    [InlineData("Null", /*lang=json*/ "null")]
    [InlineData("Number", /*lang=json*/ "42")]
    [InlineData("String", /*lang=json*/ """ "foobar" """)]
    [InlineData("StartArray", /*lang=json*/ "[]")]
    [InlineData("StartObject", /*lang=json*/ "{}")]
    public void Boolean_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.Boolean(), json,
                         "Invalid JSON value where a JSON Boolean was expected.", expectedErrorToken);
    }

    [Fact]
    public void DateTime_Moves_Reader()
    {
        TestReaderPositionPostRead(JsonReader.DateTime(), /*lang=json*/ """ "2022-02-02T12:34:56" """);
    }

    [Theory]
    [InlineData(2022, 2, 2, 0, 0, 0, 0, /*lang=json*/ """ "2022-02-02" """)]
    [InlineData(2022, 2, 2, 12, 34, 56, 0, /*lang=json*/ """ "2022-02-02T12:34:56" """)]
    public void DateTime_With_Valid_Input(int year, int month, int day, int hour, int minute, int second, int millisecond, string json)
    {
        var expected = new DateTime(year, month, day, hour, minute, second, millisecond);
        TestValidInput(JsonReader.DateTime(), json, expected);
    }

    [Theory]
    [InlineData("Null", /*lang=json*/ "null")]
    [InlineData("False", /*lang=json*/ "false")]
    [InlineData("True", /*lang=json*/ "true")]
    [InlineData("Number", /*lang=json*/ "42")]
    [InlineData("StartArray", /*lang=json*/ "[]")]
    [InlineData("StartObject", /*lang=json*/ "{}")]
    [InlineData("String", /*lang=json*/ """ "20220202" """)]
    [InlineData("String", /*lang=json*/ """ "02/02/2022" """)]
    [InlineData("String", /*lang=json*/ """ "2022-02-02 12:34:56" """)]
    public void DateTime_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.DateTime(), json,
                         "JSON value cannot be interpreted as a date and time in ISO 8601-1 extended format.",
                         expectedErrorToken);
    }

    [Fact]
    public void DateTimeOffset_Moves_Reader()
    {
        TestReaderPositionPostRead(JsonReader.DateTimeOffset(), """ "2022-02-02T12:34:56-01:00" """);
    }

    [Theory]
    [InlineData(/*lang=json*/ """ "2022-02-02" """)]
    [InlineData(/*lang=json*/ """ "2022-02-02T12:34" """)]
    [InlineData(/*lang=json*/ """ "2022-02-02T12:34Z" """)]
    [InlineData(/*lang=json*/ """ "2022-02-02T12:34:56" """)]
    [InlineData(/*lang=json*/ """ "2022-02-02T12:34:56Z" """)]
    [InlineData(/*lang=json*/ """ "2022-02-02T12:34:56.078" """)]
    [InlineData(/*lang=json*/ """ "2022-02-02T12:34:56.078Z" """)]
    [InlineData(/*lang=json*/ """ "2022-02-02T12:34:00+01:00" """)]
    [InlineData(/*lang=json*/ """ "2022-02-02T12:34:56+02:00" """)]
    [InlineData(/*lang=json*/ """ "2022-02-02T12:34:56.078-00:05" """)]
    public void DateTimeOffset_With_Valid_Input(string json)
    {
        var formats = new[]
        {
            "yyyy-MM-dd",
            "yyyy-MM-dd'T'HH:mm",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss.fff",
            "yyyy-MM-dd'T'HH:mmK",
            "yyyy-MM-dd'T'HH:mm:ssK",
            "yyyy-MM-dd'T'HH:mm:ss.fffK",
        };
        var unquoted = json[2..^2];
        var expected = DateTimeOffset.ParseExact(unquoted, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
        TestValidInput(JsonReader.DateTimeOffset(), json, expected);
    }

    [Theory]
    [InlineData("Null", /*lang=json*/ "null")]
    [InlineData("False", /*lang=json*/ "false")]
    [InlineData("True", /*lang=json*/ "true")]
    [InlineData("Number", /*lang=json*/ "42")]
    [InlineData("StartArray", /*lang=json*/ "[]")]
    [InlineData("StartObject", /*lang=json*/ "{}")]
    [InlineData("String", /*lang=json*/ """ "20220202" """)]
    [InlineData("String", /*lang=json*/ """ "02/02/2022" """)]
    [InlineData("String", /*lang=json*/ """ "2022-02-02 12:34:56" """)]
    [InlineData("String", /*lang=json*/ """ "2022-02-02t12:34:56" """)]
    [InlineData("String", /*lang=json*/ """ "2022-02-02T12 34 56" """)]
    [InlineData("String", /*lang=json*/ """ "2022-02-02T12:34:56 01" """)]
    [InlineData("String", /*lang=json*/ """ "2022-02-02T12:34:56+01 00" """)]
    public void DateTimeOffset_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.DateTimeOffset(), json,
                         "JSON value cannot be interpreted as a date and time offset in ISO 8601-1 extended format.",
                         expectedErrorToken);
    }

    [Fact]
    public void Int32_Moves_Reader()
    {
        TestReaderPositionPostRead(JsonReader.Int32(), /*lang=json*/ "42");
    }

    [Theory]
    [InlineData(42, /*lang=json*/ "42")]
    public void Int32_With_Valid_Input(int expected, string json)
    {
        TestValidInput(JsonReader.Int32(), json, expected);
    }

    [Theory]
    [InlineData("Null", /*lang=json*/ "null")]
    [InlineData("False", /*lang=json*/ "false")]
    [InlineData("True", /*lang=json*/ "true")]
    [InlineData("String", /*lang=json*/ """ "foobar" """)]
    [InlineData("StartArray", /*lang=json*/ "[]")]
    [InlineData("StartObject", /*lang=json*/ "{}")]
    public void Single_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.Single(), json,
                         "Invalid JSON value; expecting a JSON number compatible with Single.", expectedErrorToken);
    }

    [Fact]
    public void Single_Moves_Reader()
    {
        TestReaderPositionPostRead(JsonReader.Single(), /*lang=json*/ "4.2");
    }

    [Theory]
    [InlineData(4.2, /*lang=json*/ "4.2")]
    public void Single_With_Valid_Input(float expected, string json)
    {
        TestValidInput(JsonReader.Single(), json, expected);
    }

    [Theory]
    [InlineData("Null", /*lang=json*/ "null")]
    [InlineData("False", /*lang=json*/ "false")]
    [InlineData("True", /*lang=json*/ "true")]
    [InlineData("Number", /*lang=json*/ "-4.2")]
    [InlineData("String", /*lang=json*/ """ "foobar" """)]
    [InlineData("StartArray", /*lang=json*/ "[]")]
    [InlineData("StartObject", /*lang=json*/ "{}")]
    public void Int32_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.Int32(), json,
                         "Invalid JSON value; expecting a JSON number compatible with Int32.", expectedErrorToken);
    }

    [Fact]
    public void UInt16_Moves_Reader()
    {
        TestReaderPositionPostRead(JsonReader.UInt16(), /*lang=json*/ "42");
    }

    [Theory]
    [InlineData(42, /*lang=json*/ "42")]
    public void UInt16_With_Valid_Input(ushort expected, string json)
    {
        TestValidInput(JsonReader.UInt16(), json, expected);
    }

    [Theory]
    [InlineData("Null", /*lang=json*/ "null")]
    [InlineData("False", /*lang=json*/ "false")]
    [InlineData("True", /*lang=json*/ "true")]
    [InlineData("Number", /*lang=json*/ "-42")]
    [InlineData("Number", /*lang=json*/ "-4.2")]
    [InlineData("Number", /*lang=json*/ "65536")] // ushort.MaxValue + 1
    [InlineData("String", /*lang=json*/ """ "foobar" """)]
    [InlineData("StartArray", /*lang=json*/ "[]")]
    [InlineData("StartObject", /*lang=json*/ "{}")]
    public void UInt16_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.UInt16(), json,
                         "Invalid JSON value; expecting a JSON number compatible with UInt16.",
                         expectedErrorToken);
    }

    [Fact]
    public void UInt32_Moves_Reader()
    {
        TestReaderPositionPostRead(JsonReader.UInt32(), /*lang=json*/ "42");
    }

    [Theory]
    [InlineData(42, /*lang=json*/ "42")]
    public void UInt32_With_Valid_Input(uint expected, string json)
    {
        TestValidInput(JsonReader.UInt32(), json, expected);
    }

    [Theory]
    [InlineData("Null", /*lang=json*/ "null")]
    [InlineData("False", /*lang=json*/ "false")]
    [InlineData("True", /*lang=json*/ "true")]
    [InlineData("Number", /*lang=json*/ "-42")]
    [InlineData("Number", /*lang=json*/ "-4.2")]
    [InlineData("Number", /*lang=json*/ "4294967296")] // uint.MaxValue + 1
    [InlineData("String", /*lang=json*/ """ "foobar" """)]
    [InlineData("StartArray", /*lang=json*/ "[]")]
    [InlineData("StartObject", /*lang=json*/ "{}")]
    public void UInt32_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.UInt32(), json,
                         "Invalid JSON value; expecting a JSON number compatible with UInt32.",
                         expectedErrorToken);
    }

    [Fact]
    public void UInt64_Moves_Reader()
    {
        TestReaderPositionPostRead(JsonReader.UInt64(), /*lang=json*/ "42");
    }

    [Theory]
    [InlineData(42, /*lang=json*/ "42")]
    public void UInt64_With_Valid_Input(ulong expected, string json)
    {
        TestValidInput(JsonReader.UInt64(), json, expected);
    }

    [Theory]
    [InlineData("Null", /*lang=json*/ "null")]
    [InlineData("False", /*lang=json*/ "false")]
    [InlineData("True", /*lang=json*/ "true")]
    [InlineData("Number", /*lang=json*/ "-42")]
    [InlineData("Number", /*lang=json*/ "-4.2")]
    [InlineData("String", /*lang=json*/ """ "foobar" """)]
    [InlineData("StartArray", /*lang=json*/ "[]")]
    [InlineData("StartObject", /*lang=json*/ "{}")]
    public void UInt64_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.UInt64(), json,
                         "Invalid JSON value; expecting a JSON number compatible with UInt64.",
                         expectedErrorToken);
    }

    [Fact]
    public void Int64_Moves_Reader()
    {
        TestReaderPositionPostRead(JsonReader.Int64(), /*lang=json*/ "42");
    }

    [Theory]
    [InlineData(42, /*lang=json*/ "42")]
    public void Int64_With_Valid_Input(long expected, string json)
    {
        TestValidInput(JsonReader.Int64(), json, expected);
    }

    [Theory]
    [InlineData("Null", /*lang=json*/ "null")]
    [InlineData("False", /*lang=json*/ "false")]
    [InlineData("True", /*lang=json*/ "true")]
    [InlineData("Number", /*lang=json*/ "9223372036854775808")] // long.MaxValue + 1
    [InlineData("String", /*lang=json*/ """ "foobar" """)]
    [InlineData("StartArray", /*lang=json*/ "[]")]
    [InlineData("StartObject", /*lang=json*/ "{}")]
    public void Int64_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.Int64(), json,
                         "Invalid JSON value; expecting a JSON number compatible with Int64.", expectedErrorToken);
    }

    [Fact]
    public void Double_Moves_Reader()
    {
        TestReaderPositionPostRead(JsonReader.Double(), /*lang=json*/ "42");
    }

    [Theory]
    [InlineData(42, /*lang=json*/ "42")]
    [InlineData(-42, /*lang=json*/ "-42")]
    [InlineData(-4.2, /*lang=json*/ "-4.2")]
    [InlineData(400, /*lang=json*/ "4e2")]
    public void Double_With_Valid_Input(double expected, string json)
    {
        TestValidInput(JsonReader.Double(), json, expected);
    }

    [Theory]
    [InlineData("Null", /*lang=json*/ "null")]
    [InlineData("False", /*lang=json*/ "false")]
    [InlineData("True", /*lang=json*/ "true")]
    [InlineData("String", /*lang=json*/ """ "foobar" """)]
    [InlineData("StartArray", /*lang=json*/ "[]")]
    [InlineData("StartObject", /*lang=json*/ "{}")]
    public void Double_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.Double(), json,
                         "Invalid JSON value; expecting a JSON number compatible with Double.", expectedErrorToken);
    }

    [Fact]
    public void Array_Moves_Reader()
    {
        TestReaderPositionPostRead(JsonReader.Array(JsonReader.Int32()), /*lang=json*/ "[42]");
    }

    [Theory]
    [InlineData(new[] { 42 }, /*lang=json*/ "[42]")]
    [InlineData(new[] { 1, 2, 3 }, /*lang=json*/ "[1, 2, 3]")]
    public void Array_With_Valid_Input(int[] expected, string json)
    {
        TestValidInput(JsonReader.Array(JsonReader.Int32()), json, expected);
    }

    [Theory]
    [InlineData("Invalid JSON value where a JSON array was expected.", "Null", 0, /*lang=json*/ "null")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "False", 0, /*lang=json*/ "false")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "True", 0, /*lang=json*/ "true")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "String", 0, /*lang=json*/ """ "foobar" """)]
    [InlineData("Invalid JSON value where a JSON array was expected.", "StartObject", 0, /*lang=json*/ "{}")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "Null", 5, /*lang=json*/ "[42, null, 42]")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "False", 5, /*lang=json*/ "[42, false, 42]")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "True", 5, /*lang=json*/ "[42, true, 42]")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "String", 5, /*lang=json*/ """[42, "foobar", 42]""")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "StartArray", 5, /*lang=json*/ "[42, [], 42]")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "StartObject", 5, /*lang=json*/ "[42, {}, 42]")]
    public void Array_With_Invalid_Input(string expectedError, string expectedErrorToken, int expectedErrorOffset, string json)
    {
        TestInvalidInput(JsonReader.Array(JsonReader.Int32()), json, expectedError, expectedErrorToken, expectedErrorOffset);
    }

    [Fact]
    public void Array_Of_String_Array_With_Valid_Input()
    {
        TestValidInput(JsonReader.Array(JsonReader.Array(JsonReader.String())),
                       /*lang=json*/ """
                       [
                           ["123", "456", "789"],
                           ["foo", "bar", "baz"],
                           ["big", "fan", "run"]
                       ]
                       """,
                       new[]
                       {
                           new[] { "123", "456", "789" },
                           new[] { "foo", "bar", "baz" },
                           new[] { "big", "fan", "run" }
                       });
    }

    [Fact]
    public void String_Array_With_Valid_Input()
    {
        TestValidInput(JsonReader.Array(JsonReader.String()),
                       /*lang=json*/ """["foo", "bar", "baz"]""",
                       new[] { "foo", "bar", "baz" });
    }

    [Fact]
    public void Boolean_Array_With_Valid_Input()
    {
        TestValidInput(JsonReader.Array(JsonReader.Boolean()),
                       /*lang=json*/ "[true, false, true]",
                       new[] { true, false, true });
    }

    [Fact]
    public void Select_Invokes_Projection_Function_For_Read_Value()
    {
        var reader =
            from words in JsonReader.Array(JsonReader.String())
            select string.Join("-", from w in words select w.ToUpperInvariant());

        var result = reader.Read(/*lang=json*/ """["foo", "bar", "baz"]""");

        Assert.Equal("FOO-BAR-BAZ", result);
    }

    [Fact]
    public void Select_Doesnt_Move_Reader()
    {
        TestReaderPositionPostRead(from s in JsonReader.String() select s, /*lang=json*/ """ "foobar" """);
    }

    [Fact]
    public void Property_With_No_Default_Initializes_Property_As_Expected()
    {
        const string name = "foobar";
        var valueReader = JsonReader.String();
        var property = JsonReader.Property(name, valueReader);

        const string json = /*lang=json*/ """{ "foobar": 42 }""";

        var reader =
            new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        _ = reader.Read(); // "{"
        _ = reader.Read(); // property

        Assert.True(property.IsMatch(reader));
        Assert.Same(valueReader, property.Reader);
        Assert.False(property.HasDefaultValue);
        Assert.Null(property.DefaultValue);
    }

    [Fact]
    public void Property_With_Default_Initializes_Property_As_Expected()
    {
        const string name = "foobar";
        var valueReader = JsonReader.String();
        const string defaultValue = "baz";
        var property = JsonReader.Property(name, valueReader, (true, defaultValue));

        const string json = /*lang=json*/ """{ "foobar": 42 }""";

        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        _ = reader.Read(); // "{"
        _ = reader.Read(); // property

        Assert.True(property.IsMatch(reader));
        Assert.True(property.HasDefaultValue);
        Assert.Same(defaultValue, property.DefaultValue);
        Assert.Same(valueReader, property.Reader);
    }

    [Fact]
    public void Property_IsMatch_Throws_When_Reader_Is_On_Wrong_Token()
    {
        const string name = "foobar";
        var property = JsonReader.Property(name, JsonReader.String());

        var ex = Assert.Throws<ArgumentException>(() =>
        {
            const string json = /*lang=json*/ """{ "foobar": 42 }""";
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            _ = reader.Read(); // "{"
            return _ = property.IsMatch(reader);
        });

        Assert.Equal("reader", ex.ParamName);
    }

    static readonly IJsonReader<(int, string)> ObjectReader =
        JsonReader.Object(JsonReader.Property("num", JsonReader.Int32(), (true, 0)),
                          JsonReader.Property("str", JsonReader.String()),
                          ValueTuple.Create);

    [Theory]
    [InlineData( 0, "foobar", /*lang=json*/ """{ "str": "foobar" }""")]
    [InlineData(42, "foobar", /*lang=json*/ """{ "num": 42, "str": "foobar" }""")]
    [InlineData(42, "foobar", /*lang=json*/ """{ "str": "foobar", "num": 42 }""")]
    [InlineData(42, "foobar", /*lang=json*/ """{ "str": "FOOBAR", "num": -42, "str": "foobar", "num": 42 }""")]
    [InlineData(42, "foobar", /*lang=json*/ """{ "nums": [1, 2, 3], "str": "foobar", "num": 42, "obj": {} }""")]
    public void Object_With_Valid_Input(int expectedNum, string expectedStr, string json)
    {
        TestValidInput(ObjectReader, json, (expectedNum, expectedStr));
    }

    [Theory]
    [InlineData("Invalid JSON value where a JSON object was expected.", "Null", 0, /*lang=json*/ "null")]
    [InlineData("Invalid JSON value where a JSON object was expected.", "False", 0, /*lang=json*/ "false")]
    [InlineData("Invalid JSON value where a JSON object was expected.", "True", 0, /*lang=json*/ "true")]
    [InlineData("Invalid JSON value where a JSON object was expected.", "String", 0, /*lang=json*/ """ "foobar" """)]
    [InlineData("Invalid JSON value where a JSON object was expected.", "StartArray", 0, /*lang=json*/ "[]")]
    [InlineData("Invalid JSON object.", "EndObject", 1, /*lang=json*/ "{}")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "String", 9, /*lang=json*/ """{ "num": "42", "str": "foobar" }""")]
    [InlineData("Invalid JSON object.", "EndObject", 29, /*lang=json*/ """{ "NUM": 42, "STR": "foobar" }""")]
    [InlineData("Invalid JSON object.", "EndObject", 12,/*lang=json*/ """{ "num": 42 }""")]
    public void Object_With_Invalid_Input(string expectedError, string expectedErrorToken, int expectedErrorOffset, string json)
    {
        TestInvalidInput(ObjectReader, json, expectedError, expectedErrorToken, expectedErrorOffset);
    }

    [Theory]
    [InlineData(/*lang=json*/ """{ "str": "foobar", "num": 42 }""")]
    public void Object_Does_Move_Reader(string json)
    {
        TestReaderPositionPostRead(ObjectReader, json);
    }

    static readonly IJsonReader<Dictionary<string, int>>
        KeyIntMapReader = JsonReader.Object(JsonReader.Int32(), ps => ps.ToDictionary(e => e.Key, e => e.Value));

    [Fact]
    public void Object_General_With_Valid_Input()
    {
        const string json = /*lang=json*/ """{ "foo": 123, "bar": 456, "baz": 789 }""";

        TestValidInput(KeyIntMapReader, json, new() { ["foo"] = 123, ["bar"] = 456, ["baz"] = 789 });
    }

    static readonly IJsonReader<Dictionary<string, Dictionary<string, string>>> NestedObjectReader =
        JsonReader.Object(JsonReader.Object(JsonReader.String(),
                                            ps => ps.ToDictionary(e => e.Key, e => e.Value)),
                          ps => ps.ToDictionary(e => e.Key, e => e.Value));

    [Fact]
    public void Object_General_With_Nested_Property_And_Valid_Input()
    {
        const string json = /*lang=json*/ """{ "foo": { "bar": "foobarbaz" } }""";

        TestValidInput(NestedObjectReader, json, new() { ["foo"] = new() { ["bar"] = "foobarbaz" } });
    }

    [Fact]
    public void Object_General_With_Nested_Property_And_Invalid_Input()
    {
        const string json = /*lang=json*/ """{ "foo": { "bar": { "baz": "foo" } } }""";

        TestInvalidInput(NestedObjectReader, json, "Invalid JSON value where a JSON string was expected.", "StartObject", 16);
    }

    [Theory]
    [InlineData("Invalid JSON value where a JSON object was expected.", "Null", /*lang=json*/ "null")]
    [InlineData("Invalid JSON value where a JSON object was expected.", "False", /*lang=json*/ "false")]
    [InlineData("Invalid JSON value where a JSON object was expected.", "True", /*lang=json*/ "true")]
    [InlineData("Invalid JSON value where a JSON object was expected.", "String", /*lang=json*/ """ "foobar" """)]
    [InlineData("Invalid JSON value where a JSON object was expected.", "StartArray", /*lang=json*/ "[]")]
    public void Object_General_With_Invalid_Input(string expectedError, string expectedErrorToken, string json)
    {
        TestInvalidInput(KeyIntMapReader, json, expectedError, expectedErrorToken);
    }

    [Theory]
    [InlineData(/*lang=json*/ """{ "foo": 123, "bar": 456, "baz": 789 }""")]
    public void Object_General_Doesnt_Move_Reader(string json)
    {
        TestReaderPositionPostRead(KeyIntMapReader, json);
    }

    static readonly IJsonReader<object> EitherReader =
        JsonReader.Either(JsonReader.String().AsObject(),
                          JsonReader.Either(JsonReader.Array(JsonReader.Int32()).AsObject(),
                                            JsonReader.Array(JsonReader.Boolean()).AsObject()));

    [Theory]
    [InlineData(/*lang=json*/ """ "foobar" """)]
    [InlineData(/*lang=json*/ "[123, 456, 789]")]
    [InlineData(/*lang=json*/ "[true, false]")]
    public void Either_Doesnt_Move_Reader(string json)
    {
        TestReaderPositionPostRead(EitherReader, json);
    }

    [Theory]
    [InlineData("foobar", /*lang=json*/ """ "foobar" """)]
    [InlineData(new[] { 123, 456, 789 }, /*lang=json*/ "[123, 456, 789]")]
    [InlineData(new int[0], /*lang=json*/ "[]")]
    [InlineData(new[] { true, false }, /*lang=json*/ "[true, false]")]
    public void Either_With_Valid_Input(object expected, string json)
    {
        TestValidInput(EitherReader, json, expected);
    }

    [Theory]
    [InlineData("Null", 0, /*lang=json*/ "null")]
    [InlineData("False", 0, /*lang=json*/ "false")]
    [InlineData("True", 0, /*lang=json*/ "true")]
    [InlineData("Number", 1, /*lang=json*/ "[12.3, 45.6, 78.9]")]
    [InlineData("StartObject", 0, /*lang=json*/ "{}")]
    public void Either_With_Invalid_Input(string expectedErrorToken, int expectedErrorOffset, string json)
    {
        TestInvalidInput(EitherReader, json, "Invalid JSON value.", expectedErrorToken, expectedErrorOffset);
    }

    [Theory]
    [InlineData(new[] { "foo", "bar", "baz" }, /*lang=json*/ """["foo", "bar", "baz"]""")]
    [InlineData(new[] { true, false }, /*lang=json*/ "[true, false]")]
    [InlineData(new[] { 123, 456, 789 }, /*lang=json*/ "[123, 456, 789]")]
    public void Array_Either_With_Valid_Input(object expected, string json)
    {
        var reader =
            JsonReader.Array(JsonReader.Either(JsonReader.Int32().AsObject(),
                                               JsonReader.Boolean().AsObject())
                                       .Or(JsonReader.String().AsObject())).AsObject();
        TestValidInput(reader, json, expected);
    }

#pragma warning disable CA1008 // Enums should have zero value (by-design)
    public enum LoRaBandwidth
#pragma warning restore CA1008 // Enums should have zero value
    {
        BW125 = 125,
        BW250 = 250,
        BW500 = 500,
    }

    [Theory]
    [InlineData(LoRaBandwidth.BW125, /*lang=json*/ "125")]
    [InlineData(LoRaBandwidth.BW250, /*lang=json*/ "250")]
    [InlineData(LoRaBandwidth.BW500, /*lang=json*/ "500")]
    public void Number_AsEnum_With_Valid_Input(LoRaBandwidth expected, string json)
    {
        TestValidInput(JsonReader.Int32().AsEnum(n => (LoRaBandwidth)n), json, expected);
    }

    [Theory]
    [InlineData(/*lang=json*/ "225")]
    [InlineData(/*lang=json*/ "350")]
    [InlineData(/*lang=json*/ "600")]
    public void Number_AsEnum_With_Invalid_Input(string json)
    {
        TestInvalidInput(JsonReader.Int32().AsEnum(n => (LoRaBandwidth)n), json,
                         $"Invalid member for {typeof(LoRaBandwidth)}.", "Number");
    }

    [Fact]
    public void Number_AsEnum_Doesnt_Move_Reader()
    {
        TestReaderPositionPostRead(JsonReader.Int32().AsEnum(n => (LoRaBandwidth)n), /*lang=json*/ "125");
    }

    [Theory]
    [InlineData(JsonValueKind.Undefined, /*lang=json*/ """ "Undefined" """)]
    [InlineData(JsonValueKind.Object, /*lang=json*/ """ "Object" """)]
    [InlineData(JsonValueKind.Array, /*lang=json*/ """ "Array" """)]
    [InlineData(JsonValueKind.String, /*lang=json*/ """ "String" """)]
    [InlineData(JsonValueKind.Number, /*lang=json*/ """ "Number" """)]
    [InlineData(JsonValueKind.True, /*lang=json*/ """ "True" """)]
    [InlineData(JsonValueKind.False, /*lang=json*/ """ "False" """)]
    [InlineData(JsonValueKind.Null, /*lang=json*/ """ "Null" """)]
    public void String_AsEnum_With_Valid_Input(JsonValueKind expected, string json)
    {
        TestValidInput(JsonReader.String().AsEnum<JsonValueKind>(), json, expected);
    }

    [Theory]
    [InlineData(JsonValueKind.Undefined, true, /*lang=json*/ """ "undefined" """)]
    [InlineData(JsonValueKind.Object, true, /*lang=json*/ """ "object" """)]
    [InlineData(JsonValueKind.Array, true, /*lang=json*/ """ "array" """)]
    [InlineData(JsonValueKind.String, true, /*lang=json*/ """ "string" """)]
    [InlineData(JsonValueKind.Number, true, /*lang=json*/ """ "number" """)]
    [InlineData(JsonValueKind.True, true, /*lang=json*/ """ "true" """)]
    [InlineData(JsonValueKind.False, true, /*lang=json*/ """ "false" """)]
    [InlineData(JsonValueKind.Null, true, /*lang=json*/ """ "null" """)]
    [InlineData(JsonValueKind.Undefined, false, /*lang=json*/ """ "Undefined" """)]
    [InlineData(JsonValueKind.Object, false, /*lang=json*/ """ "Object" """)]
    [InlineData(JsonValueKind.Array, false, /*lang=json*/ """ "Array" """)]
    [InlineData(JsonValueKind.String, false, /*lang=json*/ """ "String" """)]
    [InlineData(JsonValueKind.Number, false, /*lang=json*/ """ "Number" """)]
    [InlineData(JsonValueKind.True, false, /*lang=json*/ """ "True" """)]
    [InlineData(JsonValueKind.False, false, /*lang=json*/ """ "False" """)]
    [InlineData(JsonValueKind.Null, false, /*lang=json*/ """ "Null" """)]
    public void String_AsEnum_With_Ignore_Case_Option_With_Valid_Input(JsonValueKind expected, bool ignoreCase, string json)
    {
        TestValidInput(JsonReader.String().AsEnum<JsonValueKind>(ignoreCase), json, expected);
    }

    [Theory]
    [InlineData(/*lang=json*/ """ "foo" """)]
    [InlineData(/*lang=json*/ """ "bar" """)]
    [InlineData(/*lang=json*/ """ "baz" """)]
    public void String_AsEnum_With_Invalid_Input(string json)
    {
        TestInvalidInput(JsonReader.String().AsEnum<JsonValueKind>(), json,
                         $"Invalid member for {typeof(JsonValueKind)}.", "String");
    }

    [Fact]
    public void String_AsEnum_Doesnt_Move_Reader()
    {
        TestReaderPositionPostRead(JsonReader.String().AsEnum<JsonValueKind>(), /*lang=json*/ """ "Null" """);
    }

    [Theory]
    [InlineData("foobar", /*lang=json*/ """ "foobar" """)]
    [InlineData(null, /*lang=json*/ "null")]
    public void String_OrNull_With_Valid_Input(string? expected, string json)
    {
        TestValidInput(JsonReader.String().OrNull(), json, expected);
    }

    [Theory]
    [InlineData("False", /*lang=json*/ "false")]
    [InlineData("True", /*lang=json*/ "true")]
    [InlineData("Number", /*lang=json*/ "12")]
    [InlineData("StartArray", /*lang=json*/ "[12.3, 45.6]")]
    [InlineData("StartObject", /*lang=json*/ "{}")]
    public void String_OrNull_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.String().OrNull(), json,
                         "Invalid JSON value.", expectedErrorToken);
    }

    [Theory]
    [InlineData(1, /*lang=json*/ "1")]
    [InlineData(null, /*lang=json*/ "null")]
    public void Number_OrNull_With_Valid_Input(int? expected, string json)
    {
        TestValidInput(JsonReader.Int32().OrNull(), json, expected);
    }

    [Theory]
    [InlineData("False", /*lang=json*/ "false")]
    [InlineData("True", /*lang=json*/ "true")]
    [InlineData("String", /*lang=json*/ """ "foobar" """)]
    [InlineData("StartArray", /*lang=json*/ "[12.3, 45.6]")]
    [InlineData("StartObject", /*lang=json*/ "{}")]
    public void Number_OrNull_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.Int32().OrNull(), json,
                         "Invalid JSON value.", expectedErrorToken);
    }

    [Fact]
    public void Number_OrNull_Moves_Reader()
    {
        var reader = JsonReader.Int32().OrNull();
        TestReaderPositionPostRead(reader, /*lang=json*/ "1");
    }

    [Fact]
    public void String_OrNull_Moves_Reader()
    {
        var reader = JsonReader.String().OrNull();
        TestReaderPositionPostRead(reader, /*lang=json*/ """ "foobar" """);
    }

    [Fact]
    public void Tuple2_Moves_Reader()
    {
        var reader = JsonReader.Tuple(JsonReader.Int32(), JsonReader.String());
        TestReaderPositionPostRead(reader, /*lang=json*/ """[123, "foobar"]""");
    }

    [Fact]
    public void Tuple2_With_Valid_Input()
    {
        var reader = JsonReader.Tuple(JsonReader.Int32(), JsonReader.String());
        var result = reader.Read(/*lang=json*/ """[123, "foobar"]""");
        Assert.Equal((123, "foobar"), result);
    }

    [Theory]
    [InlineData("Invalid JSON value where a JSON array was expected.", "Null", 0, /*lang=json*/ "null")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "False", 0, /*lang=json*/ "false")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "True", 0, /*lang=json*/ "true")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "String", 0, /*lang=json*/ """ "foobar" """)]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "EndArray", 1, /*lang=json*/ "[]")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "StartObject", 0, /*lang=json*/ "{}")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "EndArray", 4, /*lang=json*/ "[123]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "Null", 6, /*lang=json*/ "[123, null]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "False", 6, /*lang=json*/ "[123, false]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "True", 6, /*lang=json*/ "[123, true]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "StartArray", 6, /*lang=json*/ "[123, []]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "StartObject", 6, /*lang=json*/ "[123, {}]")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "String", 1, /*lang=json*/ """["foobar", 123]""")]
    [InlineData("Invalid JSON value; JSON array has too many values.", "Number", 16, /*lang=json*/ """[123, "foobar", 456]""")]
    public void Tuple2_With_Invalid_Input(string expectedError, string expectedErrorToken, int expectedErrorOffset, string json)
    {
        var reader = JsonReader.Tuple(JsonReader.Int32(), JsonReader.String());
        TestInvalidInput(reader, json, expectedError, expectedErrorToken, expectedErrorOffset);
    }

    [Fact]
    public void Tuple3_Moves_Reader()
    {
        var reader = JsonReader.Tuple(JsonReader.Int32(), JsonReader.String(), JsonReader.Int32());
        TestReaderPositionPostRead(reader, /*lang=json*/ """[123, "foobar", 456]""");
    }

    [Fact]
    public void Tuple3_With_Valid_Input()
    {
        var reader = JsonReader.Tuple(JsonReader.Int32(), JsonReader.String(), JsonReader.Int32());
        var result = reader.Read(/*lang=json*/ """[123, "foobar", 456]""");
        Assert.Equal((123, "foobar", 456), result);
    }

    [Theory]
    [InlineData("Invalid JSON value where a JSON array was expected.", "Null", 0, /*lang=json*/ "null")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "False", 0, /*lang=json*/ "false")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "True", 0, /*lang=json*/ "true")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "String", 0, /*lang=json*/ """ "foobar" """)]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "EndArray", 1, /*lang=json*/ "[]")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "StartObject", 0, /*lang=json*/ "{}")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "EndArray", 4, /*lang=json*/ "[123]")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "EndArray", 14, /*lang=json*/"""[123, "foobar"]""")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "String", 13, /*lang=json*/"""[123, "foo", "bar"]""")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "String", 1, /*lang=json*/"""["foobar", 123, 456]""")]
    [InlineData("Invalid JSON value; JSON array has too many values.", "Number", 21, /*lang=json*/"""[123, "foobar", 456, 789]""")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "Null", 6, /*lang=json*/ "[123, null, 456]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "False", 6, /*lang=json*/ "[123, false, 456]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "True", 6, /*lang=json*/ "[123, true, 456]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "StartArray", 6, /*lang=json*/ "[123, [], 456]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "StartObject", 6, /*lang=json*/ "[123, {}, 456]")]
    public void Tuple3_With_Invalid_Input(string expectedError, string expectedErrorToken, int expectedErrorOffset, string json)
    {
        var reader = JsonReader.Tuple(JsonReader.Int32(), JsonReader.String(), JsonReader.Int32());
        TestInvalidInput(reader, json, expectedError, expectedErrorToken, expectedErrorOffset);
    }

    [Theory]
    [InlineData(/*lang=json*/ """ "foobar" """)]
    [InlineData(/*lang=json*/ """ "FOOBAR" """)]
    [InlineData(/*lang=json*/ """ "FooBar" """)]
    public void Validate_With_Valid_Input(string json)
    {
        var reader = JsonReader.String().Validate(s => "foobar".Equals(s, StringComparison.OrdinalIgnoreCase));
        var expected = JsonSerializer.Deserialize<string>(json);
        var result = reader.Read(json);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(/*lang=json*/ "123")]
    [InlineData(/*lang=json*/ "468")]
    [InlineData(/*lang=json*/ "789")]
    public void Validate_With_Invalid_Input(string json)
    {
        TestInvalidInput(JsonReader.Int32().Validate(n => n >= 1_000), json,
                         "Invalid JSON value.", "Number");
    }

    [Fact]
    public void Validate_Doesnt_Move_Reader()
    {
        TestReaderPositionPostRead(JsonReader.String().Validate(_ => true), /*lang=json*/ """ "foobar" """);
    }

    [Fact]
    public void Guid_Moves_Reader()
    {
        TestReaderPositionPostRead(JsonReader.Guid(), /*lang=json*/ """ "fe58502d-1da1-456d-960c-314e09c2dcd1" """);
    }

    [Theory]
    [InlineData("fe58502d-1da1-456d-960c-314e09c2dcd1", /*lang=json*/ """ "fe58502d-1da1-456d-960c-314e09c2dcd1" """)]
    public void Guid_With_Valid_Input(Guid expected, string json)
    {
        TestValidInput(JsonReader.Guid(), json, expected);
    }

    [Theory]
    [InlineData("Null", /*lang=json*/ "null")]
    [InlineData("False", /*lang=json*/ "false")]
    [InlineData("True", /*lang=json*/ "true")]
    [InlineData("Number", /*lang=json*/ "42")]
    [InlineData("String", /*lang=json*/ """ "foobar" """)]
    [InlineData("StartArray", /*lang=json*/ "[]")]
    [InlineData("StartObject", /*lang=json*/ "{}")]
    [InlineData("String", /*lang=json*/ """ "000-000" """)]
    [InlineData("String", /*lang=json*/ """ "this0000-is00-not0-very-valid0000000" """)]
    [InlineData("String", /*lang=json*/ """ "00000000000000000000000000000000" """)]
    [InlineData("String", /*lang=json*/ """ "{00000000-0000-0000-0000-000000000000}" """)]
    [InlineData("String", /*lang=json*/ """ "(00000000-0000-0000-0000-000000000000)" """)]
    [InlineData("String", /*lang=json*/ """ "{0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}" """)]
    public void Guid_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.Guid(), json,
                         "Invalid JSON value where a Guid was expected in the 'D' format (hyphen-separated).",
                         expectedErrorToken);
    }

    [Fact]
    public void Recursive_With_Null_Function_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => JsonReader.Recursive<object>(null!));
        Assert.Equal("readerFunction", ex.ParamName);
    }

    [Fact]
    public void Recursive_Sets_Up_Reader_With_Self()
    {
        var reader =
            JsonReader.Recursive<object>(it => JsonReader.Either(JsonReader.String().AsObject(),
                                                                 JsonReader.Array(it).AsObject()));

        var result = reader.Read(/*lang=json*/ """["foo", "bar", ["baz", [["qux"]]]]""");
        Assert.Equal(new object[] { "foo", "bar", new object[] { "baz", new object[] { new object[] { "qux" } } } },
                     result);
    }

    [Fact]
    public void Let_Throws_When_Reader_Is_Null()
    {
        var e = Assert.Throws<ArgumentNullException>(() =>
            JsonReader.Let<int, object>(null!, delegate { throw new NotImplementedException(); }));

        Assert.Equal("reader", e.ParamName);
    }

    [Fact]
    public void Let_Throws_When_Selector_Is_Null()
    {
        var e = Assert.Throws<ArgumentNullException>(() =>
            JsonReader.Int32().Let<int, object>(null!));

        Assert.Equal("selector", e.ParamName);
    }

    [Fact]
    public void Let_Returns_Select_That_Receives_Reader_Return()
    {
        var reader = JsonReader.Int32();

        IJsonReader<int>? inputReader = null;
        IJsonReader<object>? outputReader = null;

        var result = reader.Let(it => outputReader = (inputReader = it).AsObject());

        Assert.Same(reader, inputReader);
        Assert.NotNull(result);
        Assert.Same(outputReader, result);
    }

    [Theory]
    [InlineData(/*lang=json*/ "null")]
    [InlineData(/*lang=json*/ "false")]
    [InlineData(/*lang=json*/ "true")]
    [InlineData(/*lang=json*/ "-4.2")]
    [InlineData(/*lang=json*/ """ "foobar" """)]
    [InlineData(/*lang=json*/ "[1, 2, 3]")]
    [InlineData(/*lang=json*/ """
                {
                    "null": null,
                    "true": true,
                    "false": false,
                    "number": 12345.67890,
                    "string": "The quick brown fox jumps over the lazy dog.",
                    "array": [],
                    "object": {}
                }
                """)]
    public void Element_With_Valid_Input(string json)
    {
        var actual = JsonReader.Element().Read(json);
        var reader = new System.Text.Json.Utf8JsonReader(Encoding.UTF8.GetBytes(json));
#if NET6_0_OR_GREATER
        var expected = JsonElement.ParseValue(ref reader);
#elif NETCOREAPP3_1_OR_GREATER
        using var doc = JsonDocument.ParseValue(ref reader);
        var expected = doc.RootElement.Clone();
#else
#error Unsupported platform.
#endif
        Assert.Equal(expected.ToString(), actual.ToString());
    }

    [Theory]
    [InlineData(/*lang=json*/ "null")]
    [InlineData(/*lang=json*/ "false")]
    [InlineData(/*lang=json*/ "true")]
    [InlineData(/*lang=json*/ "-4.2")]
    [InlineData(/*lang=json*/ """ "foobar" """)]
    [InlineData(/*lang=json*/ "[]")]
    [InlineData(/*lang=json*/ "{}")]
    public void Element_Moves_Reader(string json)
    {
        TestReaderPositionPostRead(JsonReader.Element(), json);
    }
}
