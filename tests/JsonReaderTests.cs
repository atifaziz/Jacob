// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Jacob.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Xunit;
using JToken = Newtonsoft.Json.Linq.JToken;
using Formatting = Newtonsoft.Json.Formatting;

public class JsonReaderTests
{
    /// <summary>
    /// Takes somewhat non-conforming JSON
    /// (<a href="https://github.com/JamesNK/Newtonsoft.Json/issues/646#issuecomment-356194475">as accepted by Json.NET</a>)
    /// text and re-formats it to be strictly conforming to RFC 7159.
    /// </summary>
    /// <remarks>
    /// This is a helper primarily designed to make it easier to express JSON as C# literals in
    /// inline data for theory tests, where the double quotes don't have to be escaped.
    /// </remarks>
    public static string Strictify(string json) =>
        JToken.Parse(json).ToString(Formatting.None);

    static void TestMovesReaderPastReadValue<T>(IJsonReader<T, JsonReadResult<T>> reader, string json)
    {
        var sentinel = $"END-{Guid.NewGuid()}";
        var rdr = new Utf8JsonReader(Encoding.UTF8.GetBytes(Strictify($"[{json}, '{sentinel}']")));
        Assert.True(rdr.Read()); // start
        Assert.True(rdr.Read()); // "["
        _ = reader.Read(ref rdr);
        Assert.Equal(JsonTokenType.String, rdr.TokenType);
        Assert.Equal(sentinel, rdr.GetString());
    }

    static void TestInvalidInput<T>(IJsonReader<T, JsonReadResult<T>> reader, string json,
                                    string expectedError, string expectedErrorToken, int expectedErrorOffset = 0)
    {
        json = Strictify(json);

        var (value, error) = reader.TryRead(json);
        Assert.Equal(default, value);
        Assert.Equal(expectedError, error);

        var ex = Assert.Throws<JsonException>(() => reader.Read(json));
        Assert.Equal($@"{expectedError} See token ""{expectedErrorToken}"" at offset {expectedErrorOffset}.", ex.Message);
    }

    [Fact]
    public void String_Moves_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.String(), "'foobar'");
    }

    [Fact]
    public void Error_Returns_Error()
    {
        const string message = "oops";
        var reader = JsonReader.Error<string>(message);
        var utf8Reader = new Utf8JsonReader(Encoding.UTF8.GetBytes("42"));
        var (value, error) = reader.TryRead(ref utf8Reader);

        Assert.Equal(0, utf8Reader.BytesConsumed);
        Assert.Null(value);
        Assert.Equal(message, error);
    }

    [Theory]
    [InlineData("", "''")]
    [InlineData("foobar", "'foobar'")]
    [InlineData("foo bar", "'foo bar'")]
    public void String_With_Valid_Input(string expected, string json)
    {
        var result = JsonReader.String().Read(Strictify(json));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Null", "null")]
    [InlineData("False", "false")]
    [InlineData("True", "true")]
    [InlineData("Number", "42")]
    [InlineData("StartArray", "[]")]
    [InlineData("StartObject", "{}")]
    public void String_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.String(), json,
                         "Invalid JSON value where a JSON string was expected.",
                         expectedErrorToken);
    }

    [Fact]
    public void Byte_Moves_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.Byte(), "42");
    }

    [Theory]
    [InlineData(42, "42")]
    public void Byte_With_Valid_Input(ulong expected, string json)
    {
        var result = JsonReader.Byte().Read(Strictify(json));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Null", "null")]
    [InlineData("False", "false")]
    [InlineData("True", "true")]
    [InlineData("Number", "-42")]
    [InlineData("Number", "-4.2")]
    [InlineData("Number", "256")]
    [InlineData("String", "'foobar'")]
    [InlineData("StartArray", "[]")]
    [InlineData("StartObject", "{}")]
    public void Byte_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.Byte(), json,
                         "Invalid JSON value; expecting a JSON number compatible with Byte.", expectedErrorToken);
    }

    [Fact]
    public void Null_Moves_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.Null((object?)null), "null");
    }

    [Fact]
    public void Null_With_Valid_Input()
    {
        var result = JsonReader.Null((object?)null).Read("null");
        Assert.Null(result);
    }

    [Theory]
    [InlineData("True", "true")]
    [InlineData("False", "false")]
    [InlineData("Number", "42")]
    [InlineData("String", "'foobar'")]
    [InlineData("StartArray", "[]")]
    [InlineData("StartObject", "{}")]
    public void Null_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.Null((object?)null), json,
                         "Invalid JSON value where a JSON null was expected.", expectedErrorToken);
    }

    [Fact]
    public void Boolean_Moves_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.Boolean(), "true");
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void Boolean_With_Valid_Input(bool expected, string json)
    {
        var result = JsonReader.Boolean().Read(Strictify(json));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Null", "null")]
    [InlineData("Number", "42")]
    [InlineData("String", "'foobar'")]
    [InlineData("StartArray", "[]")]
    [InlineData("StartObject", "{}")]
    public void Boolean_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.Boolean(), json,
                         "Invalid JSON value where a JSON Boolean was expected.", expectedErrorToken);
    }

    [Fact]
    public void DateTime_Moves_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.DateTime(), "'2022-02-02T12:34:56'");
    }

    [Theory]
    [InlineData(2022, 2, 2, 0, 0, 0, 0, "'2022-02-02'")]
    [InlineData(2022, 2, 2, 12, 34, 56, 0, "'2022-02-02T12:34:56'")]
    public void DateTime_With_Valid_Input(int year, int month, int day, int hour, int minute, int second, int millisecond, string json)
    {
        var result = JsonReader.DateTime().Read(Strictify(json));
        var expected = new DateTime(year, month, day, hour, minute, second, millisecond);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Null", "null")]
    [InlineData("False", "false")]
    [InlineData("True", "true")]
    [InlineData("Number", "42")]
    [InlineData("StartArray", "[]")]
    [InlineData("StartObject", "{}")]
    [InlineData("String", "'20220202'")]
    [InlineData("String", "'02/02/2022'")]
    [InlineData("String", "'2022-02-02 12:34:56'")]
    public void DateTime_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.DateTime(), json,
                         "JSON value cannot be interpreted as a date and time in ISO 8601-1 extended format.",
                         expectedErrorToken);
    }

    [Fact]
    public void Int32_Moves_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.Int32(), "42");
    }

    [Theory]
    [InlineData(42, "42")]
    public void Int32_With_Valid_Input(int expected, string json)
    {
        var result = JsonReader.Int32().Read(Strictify(json));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Null", "null")]
    [InlineData("False", "false")]
    [InlineData("True", "true")]
    [InlineData("String", "'foobar'")]
    [InlineData("StartArray", "[]")]
    [InlineData("StartObject", "{}")]
    public void Single_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.Single(), json,
                         "Invalid JSON value; expecting a JSON number compatible with Single.", expectedErrorToken);
    }

    [Fact]
    public void Single_Moves_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.Single(), "4.2");
    }

    [Theory]
    [InlineData(4.2, "4.2")]
    public void Single_With_Valid_Input(float expected, string json)
    {
        var result = JsonReader.Single().Read(Strictify(json));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Null", "null")]
    [InlineData("False", "false")]
    [InlineData("True", "true")]
    [InlineData("Number", "-4.2")]
    [InlineData("String", "'foobar'")]
    [InlineData("StartArray", "[]")]
    [InlineData("StartObject", "{}")]
    public void Int32_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.Int32(), json,
                         "Invalid JSON value; expecting a JSON number compatible with Int32.", expectedErrorToken);
    }

    [Fact]
    public void UInt16_Moves_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.UInt16(), "42");
    }

    [Theory]
    [InlineData(42, "42")]
    public void UInt16_With_Valid_Input(ulong expected, string json)
    {
        var result = JsonReader.UInt16().Read(Strictify(json));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Null", "null")]
    [InlineData("False", "false")]
    [InlineData("True", "true")]
    [InlineData("Number", "-42")]
    [InlineData("Number", "-4.2")]
    [InlineData("Number", "65536")] // ushort.MaxValue + 1
    [InlineData("String", "'foobar'")]
    [InlineData("StartArray", "[]")]
    [InlineData("StartObject", "{}")]
    public void UInt16_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.UInt16(), json,
                         "Invalid JSON value; expecting a JSON number compatible with UInt16.",
                         expectedErrorToken);
    }

    [Fact]
    public void UInt32_Moves_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.UInt32(), "42");
    }

    [Theory]
    [InlineData(42, "42")]
    public void UInt32_With_Valid_Input(ulong expected, string json)
    {
        var result = JsonReader.UInt32().Read(Strictify(json));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Null", "null")]
    [InlineData("False", "false")]
    [InlineData("True", "true")]
    [InlineData("Number", "-42")]
    [InlineData("Number", "-4.2")]
    [InlineData("Number", "4294967296")] // uint.MaxValue + 1
    [InlineData("String", "'foobar'")]
    [InlineData("StartArray", "[]")]
    [InlineData("StartObject", "{}")]
    public void UInt32_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.UInt32(), json,
                         "Invalid JSON value; expecting a JSON number compatible with UInt32.",
                         expectedErrorToken);
    }

    [Fact]
    public void UInt64_Moves_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.UInt64(), "42");
    }

    [Theory]
    [InlineData(42, "42")]
    public void UInt64_With_Valid_Input(ulong expected, string json)
    {
        var result = JsonReader.UInt64().Read(Strictify(json));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Null", "null")]
    [InlineData("False", "false")]
    [InlineData("True", "true")]
    [InlineData("Number", "-42")]
    [InlineData("Number", "-4.2")]
    [InlineData("String", "'foobar'")]
    [InlineData("StartArray", "[]")]
    [InlineData("StartObject", "{}")]
    public void UInt64_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.UInt64(), json,
                         "Invalid JSON value; expecting a JSON number compatible with UInt64.",
                         expectedErrorToken);
    }

    [Fact]
    public void Int64_Moves_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.Int64(), "42");
    }

    [Theory]
    [InlineData(42, "42")]
    public void Int64_With_Valid_Input(long expected, string json)
    {
        var result = JsonReader.Int64().Read(Strictify(json));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Null", "null")]
    [InlineData("False", "false")]
    [InlineData("True", "true")]
    [InlineData("Number", "9223372036854775808")] // long.MaxValue + 1
    [InlineData("String", "'foobar'")]
    [InlineData("StartArray", "[]")]
    [InlineData("StartObject", "{}")]
    public void Int64_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.Int64(), json,
                         "Invalid JSON value; expecting a JSON number compatible with Int64.", expectedErrorToken);
    }

    [Fact]
    public void Double_Moves_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.Double(), "42");
    }

    [Theory]
    [InlineData(42, "42")]
    [InlineData(-42, "-42")]
    [InlineData(-4.2, "-4.2")]
    [InlineData(400, "4e2")]
    public void Double_With_Valid_Input(double expected, string json)
    {
        var result = JsonReader.Double().Read(Strictify(json));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Null", "null")]
    [InlineData("False", "false")]
    [InlineData("True", "true")]
    [InlineData("String", "'foobar'")]
    [InlineData("StartArray", "[]")]
    [InlineData("StartObject", "{}")]
    public void Double_With_Invalid_Input(string expectedErrorToken, string json)
    {
        TestInvalidInput(JsonReader.Double(), json,
                         "Invalid JSON value; expecting a JSON number compatible with Double.", expectedErrorToken);
    }

    [Fact]
    public void Array_Moves_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.Array(JsonReader.Int32()), "[42]");
    }

    [Theory]
    [InlineData(new[] { 42 }, "[42]")]
    [InlineData(new[] { 1, 2, 3 }, "[1, 2, 3]")]
    public void Array_With_Valid_Input(int[] expected, string json)
    {
        var result = JsonReader.Array(JsonReader.Int32()).Read(Strictify(json));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Invalid JSON value where a JSON array was expected.", "Null", 0, "null")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "False", 0, "false")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "True", 0, "true")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "String", 0, "'foobar'")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "StartObject", 0, "{}")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "Null", 4, "[42, null, 42]")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "False", 4, "[42, false, 42]")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "True", 4, "[42, true, 42]")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "String", 4, "[42, 'foobar', 42]")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "StartArray", 4, "[42, [], 42]")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "StartObject", 4, "[42, {}, 42]")]
    public void Array_With_Invalid_Input(string expectedError, string expectedErrorToken, int expectedErrorOffset, string json)
    {
        TestInvalidInput(JsonReader.Array(JsonReader.Int32()), json, expectedError, expectedErrorToken, expectedErrorOffset);
    }

    [Fact]
    public void Select_Invokes_Projection_Function_For_Read_Value()
    {
        var reader =
            from words in JsonReader.Array(JsonReader.String())
            select string.Join("-", from w in words select w.ToUpperInvariant());

        var result = reader.Read(Strictify("['foo', 'bar', 'baz']"));

        Assert.Equal("FOO-BAR-BAZ", result);
    }

    [Fact]
    public void Select_Doesnt_Move_Reader()
    {
        TestMovesReaderPastReadValue(from s in JsonReader.String() select s, "'foobar'");
    }

    [Fact]
    public void Property_With_No_Default_Initializes_Property_As_Expected()
    {
        const string name = "foobar";
        var valueReader = JsonReader.String();
        var property = JsonReader.Property(name, valueReader);

        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(Strictify("{ foobar: 42 }")));
        _ = reader.Read(); // "{"
        _ = reader.Read(); // property

        Assert.True(property.IsMatch(ref reader));
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

        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(Strictify("{ foobar: 42 }")));
        _ = reader.Read(); // "{"
        _ = reader.Read(); // property

        Assert.True(property.IsMatch(ref reader));
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
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(Strictify("{ foobar: 42 }")));
            _ = reader.Read(); // "{"
            return _ = property.IsMatch(ref reader);
        });

        Assert.Equal("reader", ex.ParamName);
    }

    static readonly IJsonReader<(int, string), JsonReadResult<(int, string)>> ObjectReader =
        JsonReader.Object(JsonReader.Property("num", JsonReader.Int32(), (true, 0)),
                          JsonReader.Property("str", JsonReader.String()),
                          ValueTuple.Create);

    [Theory]
    [InlineData(0, "foobar", "{ str: 'foobar' }")]
    [InlineData(42, "foobar", "{ num: 42, str: 'foobar' }")]
    [InlineData(42, "foobar", "{ str: 'foobar', num: 42 }")]
    [InlineData(42, "foobar", "{ str: 'FOOBAR', num: -42, str: 'foobar', num: 42 }")]
    [InlineData(42, "foobar", "{ nums: [1, 2, 3], str: 'foobar', num: 42, obj: {} }")]
    public void Object_With_Valid_Input(int expectedNum, string expectedStr, string json)
    {
        var (num, str) = ObjectReader.Read(Strictify(json));

        Assert.Equal(expectedNum, num);
        Assert.Equal(expectedStr, str);
    }

    [Theory]
    [InlineData("Invalid JSON value where a JSON object was expected.", "Null", 0, "null")]
    [InlineData("Invalid JSON value where a JSON object was expected.", "False", 0, "false")]
    [InlineData("Invalid JSON value where a JSON object was expected.", "True", 0, "true")]
    [InlineData("Invalid JSON value where a JSON object was expected.", "String", 0, "'foobar'")]
    [InlineData("Invalid JSON value where a JSON object was expected.", "StartArray", 0, "[]")]
    [InlineData("Invalid JSON object.", "EndObject", 1, "{}")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "String", 7, "{ num: '42', str: 'foobar' }")]
    [InlineData("Invalid JSON object.", "EndObject", 24, "{ NUM: 42, STR: 'foobar' }")]
    [InlineData("Invalid JSON object.", "EndObject", 9, "{ num: 42 }")]
    public void Object_With_Invalid_Input(string expectedError, string expectedErrorToken, int expectedErrorOffset, string json)
    {
        TestInvalidInput(ObjectReader, json, expectedError, expectedErrorToken, expectedErrorOffset);
    }

    [Theory]
    [InlineData("{ str: 'foobar', num: 42 }")]
    public void Object_Does_Move_Reader(string json)
    {
        TestMovesReaderPastReadValue(ObjectReader, json);
    }

    static readonly IJsonReader<Dictionary<string, int>, JsonReadResult<Dictionary<string, int>>>
        KeyIntMapReader = JsonReader.Object(JsonReader.Int32(), ps => ps.ToDictionary(e => e.Key, e => e.Value));

    [Fact]
    public void Object_General_With_Valid_Input()
    {
        const string json = @"{ foo: 123, bar: 456, baz: 789 }";

        var obj = KeyIntMapReader.Read(Strictify(json));

        Assert.Equal(3, obj.Count);
        Assert.Equal(123, obj["foo"]);
        Assert.Equal(456, obj["bar"]);
        Assert.Equal(789, obj["baz"]);
    }

    [Theory]
    [InlineData("Invalid JSON value where a JSON object was expected.", "Null", "null")]
    [InlineData("Invalid JSON value where a JSON object was expected.", "False", "false")]
    [InlineData("Invalid JSON value where a JSON object was expected.", "True", "true")]
    [InlineData("Invalid JSON value where a JSON object was expected.", "String", "'foobar'")]
    [InlineData("Invalid JSON value where a JSON object was expected.", "StartArray", "[]")]
    public void Object_General_With_Invalid_Input(string expectedError, string expectedErrorToken, string json)
    {
        TestInvalidInput(KeyIntMapReader, json, expectedError, expectedErrorToken);
    }

    [Theory]
    [InlineData("{ foo: 123, bar: 456, baz: 789 }")]
    public void Object_General_Doesnt_Move_Reader(string json)
    {
        TestMovesReaderPastReadValue(KeyIntMapReader, json);
    }

    static readonly IJsonReader<object, JsonReadResult<object>> EitherReader =
        JsonReader.Either(JsonReader.String().AsObject(),
                          JsonReader.Either(JsonReader.Array(JsonReader.Int32()).AsObject(),
                                            JsonReader.Array(JsonReader.Boolean()).AsObject()));

    [Theory]
    [InlineData("'foobar'")]
    [InlineData("[123, 456, 789]")]
    [InlineData("[true, false]")]
    public void Either_Doesnt_Move_Reader(string json)
    {
        TestMovesReaderPastReadValue(EitherReader, json);
    }

    [Theory]
    [InlineData("foobar", "'foobar'")]
    [InlineData(new[] { 123, 456, 789 }, "[123, 456, 789]")]
    [InlineData(new int[0], "[]")]
    [InlineData(new[] { true, false }, "[true, false]")]
    public void Either_With_Valid_Input(object expected, string json)
    {
        var result = EitherReader.Read(Strictify(json));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Null", 0, "null")]
    [InlineData("False", 0, "false")]
    [InlineData("True", 0, "true")]
    [InlineData("Number", 1, "[12.3, 45.6, 78.9]")]
    [InlineData("StartObject", 0, "{}")]
    public void Either_With_Invalid_Input(string expectedErrorToken, int expectedErrorOffset, string json)
    {
        TestInvalidInput(EitherReader, json, "Invalid JSON value.", expectedErrorToken, expectedErrorOffset);
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
    [InlineData(LoRaBandwidth.BW125, "125")]
    [InlineData(LoRaBandwidth.BW250, "250")]
    [InlineData(LoRaBandwidth.BW500, "500")]
    public void Number_AsEnum_With_Valid_Input(LoRaBandwidth expected, string json)
    {
        var reader = JsonReader.Int32().AsEnum(n => (LoRaBandwidth)n);
        var result = reader.Read(Strictify(json));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("225")]
    [InlineData("350")]
    [InlineData("600")]
    public void Number_AsEnum_With_Invalid_Input(string json)
    {
        TestInvalidInput(JsonReader.Int32().AsEnum(n => (LoRaBandwidth)n), json,
                         $"Invalid member for {typeof(LoRaBandwidth)}.", "Number");
    }

    [Fact]
    public void Number_AsEnum_Doesnt_Move_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.Int32().AsEnum(n => (LoRaBandwidth)n), "125");
    }

    [Theory]
    [InlineData(JsonValueKind.Undefined, "'Undefined'")]
    [InlineData(JsonValueKind.Object, "'Object'")]
    [InlineData(JsonValueKind.Array, "'Array'")]
    [InlineData(JsonValueKind.String, "'String'")]
    [InlineData(JsonValueKind.Number, "'Number'")]
    [InlineData(JsonValueKind.True, "'True'")]
    [InlineData(JsonValueKind.False, "'False'")]
    [InlineData(JsonValueKind.Null, "'Null'")]
    public void String_AsEnum_With_Valid_Input(JsonValueKind expected, string json)
    {
        var reader = JsonReader.String().AsEnum<JsonValueKind>();
        var result = reader.Read(Strictify(json));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(JsonValueKind.Undefined, true, "'undefined'")]
    [InlineData(JsonValueKind.Object, true, "'object'")]
    [InlineData(JsonValueKind.Array, true, "'array'")]
    [InlineData(JsonValueKind.String, true, "'string'")]
    [InlineData(JsonValueKind.Number, true, "'number'")]
    [InlineData(JsonValueKind.True, true, "'true'")]
    [InlineData(JsonValueKind.False, true, "'false'")]
    [InlineData(JsonValueKind.Null, true, "'null'")]
    [InlineData(JsonValueKind.Undefined, false, "'Undefined'")]
    [InlineData(JsonValueKind.Object, false, "'Object'")]
    [InlineData(JsonValueKind.Array, false, "'Array'")]
    [InlineData(JsonValueKind.String, false, "'String'")]
    [InlineData(JsonValueKind.Number, false, "'Number'")]
    [InlineData(JsonValueKind.True, false, "'True'")]
    [InlineData(JsonValueKind.False, false, "'False'")]
    [InlineData(JsonValueKind.Null, false, "'Null'")]
    public void String_AsEnum_With_Ignore_Case_Option_With_Valid_Input(JsonValueKind expected, bool ignoreCase, string json)
    {
        var reader = JsonReader.String().AsEnum<JsonValueKind>(ignoreCase);
        var result = reader.Read(Strictify(json));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("'foo'")]
    [InlineData("'bar'")]
    [InlineData("'baz'")]
    public void String_AsEnum_With_Invalid_Input(string json)
    {
        TestInvalidInput(JsonReader.String().AsEnum<JsonValueKind>(), json,
                         $"Invalid member for {typeof(JsonValueKind)}.", "String");
    }

    [Fact]
    public void String_AsEnum_Doesnt_Move_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.String().AsEnum<JsonValueKind>(), "'Null'");
    }

    [Fact]
    public void Tuple2_Moves_Reader()
    {
        var reader = JsonReader.Tuple(JsonReader.Int32(), JsonReader.String());
        TestMovesReaderPastReadValue(reader, "[123, 'foobar']");
    }

    [Fact]
    public void Tuple2_With_Valid_Input()
    {
        var reader = JsonReader.Tuple(JsonReader.Int32(), JsonReader.String());
        var result = reader.Read(Strictify("[123, 'foobar']"));
        Assert.Equal((123, "foobar"), result);
    }

    [Theory]
    [InlineData("Invalid JSON value where a JSON array was expected.", "Null", 0, "null")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "False", 0, "false")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "True", 0, "true")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "String", 0, "'foobar'")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "EndArray", 1, "[]")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "StartObject", 0, "{}")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "EndArray", 4, "[123]")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "String", 1, "['foobar', 123]")]
    [InlineData("Invalid JSON value; JSON array has too many values.", "Number", 14, "[123, 'foobar', 456]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "Null", 5, "[123, null]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "False", 5, "[123, false]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "True", 5, "[123, true]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "StartArray", 5, "[123, []]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "StartObject", 5, "[123, {}]")]
    public void Tuple2_With_Invalid_Input(string expectedError, string expectedErrorToken, int expectedErrorOffset, string json)
    {
        var reader = JsonReader.Tuple(JsonReader.Int32(), JsonReader.String());
        TestInvalidInput(reader, json, expectedError, expectedErrorToken, expectedErrorOffset);
    }

    [Fact]
    public void Tuple3_Moves_Reader()
    {
        var reader = JsonReader.Tuple(JsonReader.Int32(), JsonReader.String(), JsonReader.Int32());
        TestMovesReaderPastReadValue(reader, "[123, 'foobar', 456]");
    }

    [Fact]
    public void Tuple3_With_Valid_Input()
    {
        var reader = JsonReader.Tuple(JsonReader.Int32(), JsonReader.String(), JsonReader.Int32());
        var result = reader.Read(Strictify("[123, 'foobar', 456]"));
        Assert.Equal((123, "foobar", 456), result);
    }

    [Theory]
    [InlineData("Invalid JSON value where a JSON array was expected.", "Null", 0, "null")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "False", 0, "false")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "True", 0, "true")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "String", 0, "'foobar'")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "EndArray", 1, "[]")]
    [InlineData("Invalid JSON value where a JSON array was expected.", "StartObject", 0, "{}")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "EndArray", 4, "[123]")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "EndArray", 13, "[123, 'foobar']")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "String", 11, "[123, 'foo', 'bar']")]
    [InlineData("Invalid JSON value; expecting a JSON number compatible with Int32.", "String", 1, "['foobar', 123, 456]")]
    [InlineData("Invalid JSON value; JSON array has too many values.", "Number", 18, "[123, 'foobar', 456, 789]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "Null", 5, "[123, null, 456]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "False", 5, "[123, false, 456]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "True", 5, "[123, true, 456]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "StartArray", 5, "[123, [], 456]")]
    [InlineData("Invalid JSON value where a JSON string was expected.", "StartObject", 5, "[123, {}, 456]")]
    public void Tuple3_With_Invalid_Input(string expectedError, string expectedErrorToken, int expectedErrorOffset, string json)
    {
        var reader = JsonReader.Tuple(JsonReader.Int32(), JsonReader.String(), JsonReader.Int32());
        TestInvalidInput(reader, json, expectedError, expectedErrorToken, expectedErrorOffset);
    }

    [Theory]
    [InlineData("'foobar'")]
    [InlineData("'FOOBAR'")]
    [InlineData("'FooBar'")]
    public void Validate_With_Valid_Input(string json)
    {
        json = Strictify(json);
        var reader = JsonReader.String().Validate(s => "foobar".Equals(s, StringComparison.OrdinalIgnoreCase));
        var expected = JsonSerializer.Deserialize<string>(json);
        var result = reader.Read(json);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("468")]
    [InlineData("789")]
    public void Validate_With_Invalid_Input(string json)
    {
        TestInvalidInput(JsonReader.Int32().Validate(n => n >= 1_000), json,
                         "Invalid JSON value.", "Number");
    }

    [Fact]
    public void Validate_Doesnt_Move_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.String().Validate(_ => true), "'foobar'");
    }

    [Fact]
    public void Guid_Moves_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.Guid(), $"'{Guid.NewGuid()}'");
    }

    [Theory]
    [InlineData("fe58502d-1da1-456d-960c-314e09c2dcd1", "'fe58502d-1da1-456d-960c-314e09c2dcd1'")]
    public void Guid_With_Valid_Input(Guid expected, string json)
    {
        var result = JsonReader.Guid().Read(Strictify(json));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Null", "null")]
    [InlineData("False", "false")]
    [InlineData("True", "true")]
    [InlineData("Number", "42")]
    [InlineData("String", "'foobar'")]
    [InlineData("StartArray", "[]")]
    [InlineData("StartObject", "{}")]
    [InlineData("String", "'000-000'")]
    [InlineData("String", "'this0000-is00-not0-very-valid0000000'")]
    [InlineData("String", "'00000000000000000000000000000000'")]
    [InlineData("String", "'{00000000-0000-0000-0000-000000000000}'")]
    [InlineData("String", "'(00000000-0000-0000-0000-000000000000)'")]
    [InlineData("String", "'{0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}'")]
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

        var result = reader.Read(Strictify(@"['foo', 'bar', ['baz', [['qux']]]]"));
        Assert.Equal(new object[] { "foo", "bar", new object[] { "baz", new object[] { new object[] { "qux" } } } },
                     result);
    }
}
