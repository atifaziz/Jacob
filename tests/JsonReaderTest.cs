// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace JsonR.Tests;

using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Xunit;
using JToken = Newtonsoft.Json.Linq.JToken;
using Formatting = Newtonsoft.Json.Formatting;

public class JsonReaderTest
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

    private static void TestMovesReaderPastReadValue<T>(IJsonReader<T> reader, string json)
    {
        var sentinel = $"END-{Guid.NewGuid()}";
        var rdr = new Utf8JsonReader(Encoding.UTF8.GetBytes(Strictify($"[{json}, '{sentinel}']")));
        Assert.True(rdr.Read()); // start
        Assert.True(rdr.Read()); // "["
        reader.Read(ref rdr);
        Assert.Equal(JsonTokenType.String, rdr.TokenType);
        Assert.Equal(sentinel, rdr.GetString());
    }

    [Fact]
    public void String_Moves_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.String(), "'foobar'");
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
    [InlineData("null")]
    [InlineData("false")]
    [InlineData("true")]
    [InlineData("42")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void String_With_Invalid_Input(string json)
    {
        Assert.Throws<JsonException>(() => _ = JsonReader.String().Read(Strictify(json)));
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
    [InlineData("null")]
    [InlineData("false")]
    [InlineData("true")]
    [InlineData("-42")]
    [InlineData("-4.2")]
    [InlineData("256")]
    [InlineData("'foobar'")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void Byte_With_Invalid_Input(string json)
    {
        Assert.Throws<JsonException>(() => _ = JsonReader.Byte().Read(Strictify(json)));
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
    [InlineData("42")]
    [InlineData("-4.2")]
    [InlineData("'foobar'")]
    [InlineData("[]")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("{}")]
    public void Null_With_Invalid_Input(string json)
    {
        var reader = JsonReader.Null((object?)null);
        Assert.Throws<JsonException>(() => _ = reader.Read(Strictify(json)));
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
    [InlineData("null")]
    [InlineData("42")]
    [InlineData("'foobar'")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void Boolean_With_Invalid_Input(string json)
    {
        Assert.Throws<JsonException>(() => _ = JsonReader.Boolean().Read(Strictify(json)));
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
    [InlineData("null")]
    [InlineData("false")]
    [InlineData("true")]
    [InlineData("'foobar'")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void Single_With_Invalid_Input(string json)
    {
        Assert.Throws<JsonException>(() => _ = JsonReader.Single().Read(Strictify(json)));
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
    [InlineData("null")]
    [InlineData("false")]
    [InlineData("true")]
    [InlineData("-4.2")]
    [InlineData("'foobar'")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void Int32_With_Invalid_Input(string json)
    {
        Assert.Throws<JsonException>(() => _ = JsonReader.Int32().Read(Strictify(json)));
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
    [InlineData("null")]
    [InlineData("false")]
    [InlineData("true")]
    [InlineData("-42")]
    [InlineData("-4.2")]
    [InlineData("65536")] // ushort.MaxValue + 1
    [InlineData("'foobar'")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void UInt16_With_Invalid_Input(string json)
    {
        Assert.Throws<JsonException>(() => _ = JsonReader.UInt16().Read(Strictify(json)));
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
    [InlineData("null")]
    [InlineData("false")]
    [InlineData("true")]
    [InlineData("-42")]
    [InlineData("-4.2")]
    [InlineData("4294967296")] // uint.MaxValue + 1
    [InlineData("'foobar'")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void UInt32_With_Invalid_Input(string json)
    {
        Assert.Throws<JsonException>(() => _ = JsonReader.UInt32().Read(Strictify(json)));
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
    [InlineData("null")]
    [InlineData("false")]
    [InlineData("true")]
    [InlineData("-42")]
    [InlineData("-4.2")]
    [InlineData("'foobar'")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void UInt64_With_Invalid_Input(string json)
    {
        Assert.Throws<JsonException>(() => _ = JsonReader.UInt64().Read(Strictify(json)));
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
    [InlineData("null")]
    [InlineData("false")]
    [InlineData("true")]
    [InlineData("'foobar'")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void Double_With_Invalid_Input(string json)
    {
        Assert.Throws<JsonException>(() => _ = JsonReader.Double().Read(Strictify(json)));
    }

    [Fact]
    public void Array_Moves_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.Array(JsonReader.UInt64()), "[42]");
    }

    [Theory]
    [InlineData(new ulong[] { 42 }, "[42]")]
    [InlineData(new ulong[] { 1, 2, 3 }, "[1, 2, 3]")]
    public void Array_With_Valid_Input(ulong[] expected, string json)
    {
        var result = JsonReader.Array(JsonReader.UInt64()).Read(Strictify(json));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("false")]
    [InlineData("true")]
    [InlineData("'foobar'")]
    [InlineData("{}")]
    [InlineData("[42, null, 42]")]
    [InlineData("[42, false, 42]")]
    [InlineData("[42, true, 42]")]
    [InlineData("[42, 'foobar', 42]")]
    [InlineData("[42, [], 42]")]
    [InlineData("[42, {}, 42]")]
    public void Array_With_Invalid_Input(string json)
    {
        Assert.Throws<JsonException>(() => _ = JsonReader.Array(JsonReader.UInt64()).Read(Strictify(json)));
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

    private static readonly IJsonReader<(ulong, string)> Object2Reader =
        JsonReader.Object(JsonReader.Property("num", JsonReader.UInt64(), (true, 0UL)),
                          JsonReader.Property("str", JsonReader.String()),
                          ValueTuple.Create);

    [Theory]
    [InlineData(0, "foobar", "{ str: 'foobar' }")]
    [InlineData(42, "foobar", "{ num: 42, str: 'foobar' }")]
    [InlineData(42, "foobar", "{ str: 'foobar', num: 42 }")]
    [InlineData(42, "foobar", "{ str: 'FOOBAR', num: -42, str: 'foobar', num: 42 }")]
    [InlineData(42, "foobar", "{ nums: [1, 2, 3], str: 'foobar', num: 42, obj: {} }")]
    public void Object2_With_Valid_Input(ulong expectedNum, string expectedStr, string json)
    {
        var (num, str) = Object2Reader.Read(Strictify(json));

        Assert.Equal(expectedNum, num);
        Assert.Equal(expectedStr, str);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("false")]
    [InlineData("true")]
    [InlineData("'foobar'")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{ num: '42', str: 'foobar' }")]
    [InlineData("{ NUM: 42, STR: 'foobar' }")]
    [InlineData("{ num: 42 }")]
    public void Object2_With_Invalid_Input(string json)
    {
        Assert.Throws<JsonException>(() => _ = Object2Reader.Read(Strictify(json)));
    }

    private static readonly IJsonReader<object> EitherReader =
        JsonReader.Either(JsonReader.String().AsObject(),
                          JsonReader.Either(JsonReader.Array(JsonReader.UInt64().AsObject()),
                                            JsonReader.Array(JsonReader.Boolean().AsObject())));

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
    [InlineData(new ulong[] { 123, 456, 789 }, "[123, 456, 789]")]
    [InlineData(new ulong[0], "[]")]
    [InlineData(new[] { true, false }, "[true, false]")]
    public void Either_With_Valid_Input(object expected, string json)
    {
        var result = EitherReader.Read(Strictify(json));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("false")]
    [InlineData("true")]
    [InlineData("[12.3, 45.6, 78.9]")]
    [InlineData("{}")]
    public void Either_With_Invalid_Input(string json)
    {
        Assert.Throws<JsonException>(() => _ = EitherReader.Read(Strictify(json)));
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
    public void AsEnum_With_Valid_Input(LoRaBandwidth expected, string json)
    {
        var reader = JsonReader.Int32().AsEnum(n => (LoRaBandwidth)n);
        var result = reader.Read(Strictify(json));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("225")]
    [InlineData("350")]
    [InlineData("600")]
    public void AsEnum_With_Invalid_Input(string json)
    {
        var reader = JsonReader.Int32().AsEnum(n => (LoRaBandwidth)n);

        var ex = Assert.Throws<JsonException>(() => reader.Read(Strictify(json)));
        Assert.Equal($"Invalid member for {typeof(LoRaBandwidth)}.", ex.Message);
    }

    [Fact]
    public void Tuple3_Moves_Reader()
    {
        var reader = JsonReader.Tuple(JsonReader.UInt64(), JsonReader.String(), JsonReader.UInt64());
        TestMovesReaderPastReadValue(reader, "[123, 'foobar', 456]");
    }

    [Fact]
    public void Tuple3_With_Valid_Input()
    {
        var reader = JsonReader.Tuple(JsonReader.UInt64(), JsonReader.String(), JsonReader.UInt64());
        var result = reader.Read(Strictify("[123, 'foobar', 456]"));
        Assert.Equal((123UL, "foobar", 456UL), result);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("false")]
    [InlineData("true")]
    [InlineData("'foobar'")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("[123]")]
    [InlineData("[123, 456]")]
    [InlineData("[123, 'foo', 'bar']")]
    [InlineData("['foobar', 123, 456]")]
    [InlineData("[123, 'foobar', 456, 789]")]
    [InlineData("[123, null, 456]")]
    [InlineData("[123, false, 456]")]
    [InlineData("[123, true, 456]")]
    [InlineData("[123, [], 456]")]
    [InlineData("[123, {}, 456]")]
    public void Tuple3_With_Invalid_Input(string json)
    {
        var reader = JsonReader.Tuple(JsonReader.UInt64(), JsonReader.String(), JsonReader.UInt64());
        Assert.Throws<JsonException>(() => _ = reader.Read(Strictify(json)));
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
    public void Validate_With_Invalid_Input(string input)
    {
        var reader = JsonReader.Int32().Validate(n => n >= 1_000);

        var ex = Assert.Throws<JsonException>(() => reader.Read(Strictify(input)));
        Assert.Equal("Invalid JSON value.", ex.Message);
    }

    [Fact]
    public void Validate_Doesnt_Move_Reader()
    {
        TestMovesReaderPastReadValue(JsonReader.String().Validate(_ => true), "'foobar'");
    }
}
