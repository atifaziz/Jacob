# Jacob

Jacob provides a succinct JSON combinator API over `Utf8JsonReader` for
deserializing/reading JSON data.

.NET Core 3.0 introduced [`Utf8JsonReader`], a high-performance, low-allocation,
forward-only reader for JSON text encoded in UTF-8. It is a low-level type for
building custom parsers and deserializers, and because it has _low-level_
semantics, its API is not very straightforward to use. It's far more common
instead to see developers create simple classes that mirror the JSON data and
then use [`JsonSerializer`] to deserialize/read the JSON data into instances of
those classes. However, when your application or domain object model is far
richer than the value types available in JSON, you have two possible approaches:

1. Customize the deserialization by writing and registering a [`JsonConverter`].
   This requires a non-trivial amount of coding and understanding, not to
   mention having to work with `Utf8JsonReader` and writing unit tests for each
   converter. Moreover, the converters are not immediately reusable and
   composable (in an ad-hoc manner or otherwise) without considerable effort.

2. Maintain two sets of object models: one that is usually anemic (containing
   very little logic) and for the sole purpose of deserializing from JSON, and
   another that is the rich application/domain model. Then, have logic to
   transform the former into the latter. This requires extra processing as well
   as additional and temporary intermediate represetations of the data in
   memory.

Jacob provides most of the benefits of `Utf8JsonReader`, such as low-allocation,
but without all the ceremony and complexity associated with either of the above
approaches.


## Usage

All of the following examples assume these imports:

```c#
using System;
using System.Collections.Generic;
using System.Text.Json;
using Jacob;
```

Furthermore, to make the JSON data easier to read when encoded as C# literals
(e.g., by permitting single-quoted strings and unquoted JSON object member names
so double-quotes don't need escaping), the examples assume the following helper
method is defined and the [Newtonsoft.Json (13.x)] package is referenced:

```c#
// #r "nuget: Newtonsoft.Json, 13.0.1"

using Formatting = Newtonsoft.Json.Formatting;
using JToken = Newtonsoft.Json.Linq.JToken;

static class Json
{
    /// <summary>
    /// Takes somewhat non-conforming JSON
    /// (<a href="https://github.com/JamesNK/Newtonsoft.Json/issues/646#issuecomment-356194475">as accepted by Json.NET</a>)
    /// text and re-formats it to be strictly conforming to RFC 7159.
    /// </summary>
    public static string Strictify(string json) =>
        JToken.Parse(json).ToString(Formatting.None);
}
```

Starting simple, here is how to read a JSON number as an `int` in C# (or
`System.Int32`):

```c#
var x = JsonReader.Int32().Read("42");
Console.WriteLine(x);
```

Here is how to read a JSON string as a .NET string:

```c#
var s = JsonReader.String().Read(Json.Strictify("'foobar'"));
Console.WriteLine(s);
```

Note the use of `Json.Strictify` defined and mentioned earlier so the JSON
string `"foobar"` can be expressed in C# as `"'foobar'"` rather than
`"\"foobar\""` or `@"""foobar"""`.

Deserializing simple types like a string or an integer is pretty straightforward
and can be done with [`JsonSerializer.Deserialize`][deserialize] with equal
ease:

```c#
var x = JsonSerializer.Deserialize<int>("42");
Console.WriteLine(x);

var s = JsonSerializer.Deserialize<string>(Json.Strictify("'foobar'"));
Console.WriteLine(s);
```

However, the next example shows the benefits of a compositional API. It
demonstrates how to read a tuple of string and integer expressed in JSON as
an array of two elements:

```c#
var (key, value) =
    JsonReader.Tuple(JsonReader.String(), JsonReader.Int32())
              .Read(Json.Strictify("['foobar', 42]"));
Console.WriteLine($"Key = {key}, Value = {value}");
```

Note how `JsonReader.Tuple` builds on top of the string and integer reader
introduced in previous examples. By supplying those readers as arguments, the
tuple reader then knows how to read each item of the tuple. The tuple reader
also knows to expect only a JSON array of two elements; if more elements are
supplied, as in `["foobar", 42, null]`, then it will produce the following
error:

    Invalid JSON value; JSON array has too many values. See token "Null" at offset 13.

Attempting to do the same with `JsonSerializer.Deserialize` (assuming .NET 6):

```c#
var json = Json.Strictify("['foobar', 42]");
var (key, value) = JsonSerializer.Deserialize<(string, int)>(json);
Console.WriteLine($"Key = {key}, Value = {value}");
```

fails with the error:

    The JSON value could not be converted to System.ValueTuple`2[System.String,System.Int32]. Path: $ | LineNumber: 0 | BytePositionInLine: 1.

and this is where a custom [`JsonConverter`] implementation would be needed that
knows how to deserialize a tuple of a string and an integer from a JSON array.

Once a reader is initialized, it can be reused. In the next example, the reader
is defined outside the loop and then used to read JSON through each iteration:

```c#
var reader = JsonReader.Tuple(JsonReader.String(), JsonReader.Int32());

foreach (var json in new[]
                     {
                         "['foo', 123]",
                         "['bar', 456]",
                         "['baz', 789]",
                     })
{
    json = Json.Strictify(json);
    var (key, value) = reader.Read(json);
    Console.WriteLine($"Key = {key}, Value = {value}");
}
```

Jacob also enables use of LINQ so you can read a value as one type and project
it to a value of another type that may be closer to what the application might
desire:

```c#
var reader =
    from t in JsonReader.Tuple(JsonReader.String(), JsonReader.Int32())
    select KeyValuePair.Create(t.Item1, t.Item2);

var pair = reader.Read(Json.Strictify("['foobar', 42]"));
Console.WriteLine(pair);
```

In the above example, a tuple `(string, int)` is converted to a
`KeyValuePair<string, int>`. Again, this demonstrates how readers just compose
with one another. In the same vein, if an array of key-value pairs is what's
needed, then it's just another composition away (this time, with
`JsonReader.Array`):

```c#
var pairReader =
    from t in JsonReader.Tuple(JsonReader.String(), JsonReader.Int32())
    select KeyValuePair.Create(t.Item1, t.Item2);

const string json = @"[
    ['foo', 123],
    ['bar', 456],
    ['baz', 789]
]";

var pairs = JsonReader.Array(pairReader).Read(Json.Strictify(json));

foreach (var (key, value) in pairs)
    Console.WriteLine($"Key = {key}, Value = {value}");
```

`JsonReader.Either` allows a JSON value to be read in one of two ways. For
example, here a JSON array is read whose elements are either an integer
or a string:

```c#
var reader =
    JsonReader.Either(JsonReader.String().AsObject(),
                      JsonReader.Int32().AsObject());

const string json = @"['foo', 123, 'bar', 456, 'baz', 789]";

var values = JsonReader.Array(reader).Read(Json.Strictify(json));

foreach (var value in values)
    Console.WriteLine($"{value} ({value.GetType().Name})");

/* outputs:

foo (String)
123 (Int32)
bar (String)
456 (Int32)
baz (String)
789 (Int32)
*/
```

`Either` expects either reader to return the same type of value, which is why
`AsObject()` is used to convert each read string or integer to the common super
type `object`. `Either` can also be combined with itself to support reading a
JSON value in more than two ways.

Finally, there's `JsonReader.Object` that takes property definitions via
`JsonReader.Property` and a function to combine the read values into the final
object:

```c#
var pairReader =
    JsonReader.Object(
        JsonReader.Property("key", JsonReader.String()),
        JsonReader.Property("value", JsonReader.Int32()),
        KeyValuePair.Create /*
        above is same as:
        (k, v) => KeyValuePair.Create(k, v) */
    );

const string json = @"[
    { key  : 'foo', value: 123 },
    { value: 456  , key: 'bar' },
    { key  : 'baz', value: 789 },
]";

var pairs = JsonReader.Array(pairReader).Read(Json.Strictify(json));

foreach (var (key, value) in pairs)
    Console.WriteLine($"Key = {key}, Value = {value}");
```

Once more, `JsonReader.Object` builds on top of readers for each property for
a combined effect of creating an object from the constituent parts.


## Limitations

- There is no API at this time for writing JSON data.

- When using `JsonReader.Object` with `JsonReader.Property`, a maximum of 16
  properties of a JSON object can be read.

- No support for asynchronous reading, since `Utf8JsonReader` doesn't support it
  either.

## Benchmarks

You can find all benchmarks under `/bench`. With a benchmark on GeoJSON data
deserialization we exercise deserialization of a real-world format and compare
Jacob's performance to `System.Text.Json`s performance. As the GeoJSON
specification is best represented in C# with type hierarchies (polymorphic
deserialization), it is a good example of how to use `JsonReader.Either` to
express polymorphic deserialization succinctly. We compare this with a naive
approach that is achieving the same thing in `System.Text.Json`. We are aware
that polymorphic deserialization is supported in `System.Text.Json` using custom
converters, but did not want to compare more low-level/extensive deserialization
code with Jacob's high-level API.

By exercising different distribution of elements in the GeoJSON array, in one
case we distribute elements evenly using a round-robin distribution mechanism.
In another configuration, we benchmark the worst-case scenario performance of
`JsonReader.Either` by deserializing an array with elements of type
`MultiPolygon`. We measure the following:

    BenchmarkDotNet=v0.12.1, OS=Windows 10.0.22000
    Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
    .NET Core SDK=6.0.201
      [Host]     : .NET Core 6.0.3 (CoreCLR 6.0.322.12309, CoreFX 6.0.322.12309), X64 RyuJIT
      DefaultJob : .NET Core 6.0.3 (CoreCLR 6.0.322.12309, CoreFX 6.0.322.12309), X64 RyuJIT

|                  Method | NumberOfElements | ElementDistribution |          Mean |        Error |       StdDev | Ratio | RatioSD |     Gen 0 |     Gen 1 |     Gen 2 |   Allocated |
|------------------------ |----------------- |-------------------- |--------------:|-------------:|-------------:|------:|--------:|----------:|----------:|----------:|------------:|
|     **JsonReaderBenchmark** |               **10** |          **RoundRobin** |      **46.12 μs** |     **0.882 μs** |     **1.115 μs** |  **1.32** |    **0.04** |    **1.8921** |         **-** |         **-** |     **7.83 KB** |
| SystemTextJsonBenchmark |               10 |          RoundRobin |      35.06 μs |     0.673 μs |     0.661 μs |  1.00 |    0.00 |    3.7842 |         - |         - |     15.7 KB |
|                         |                  |                     |               |              |              |       |         |           |           |           |             |
|     **JsonReaderBenchmark** |               **10** |    **MultiPolygonOnly** |      **93.75 μs** |     **1.806 μs** |     **1.855 μs** |  **1.06** |    **0.04** |    **4.8828** |    **0.1221** |         **-** |    **20.16 KB** |
| SystemTextJsonBenchmark |               10 |    MultiPolygonOnly |      87.64 μs |     1.749 μs |     3.109 μs |  1.00 |    0.00 |    9.2773 |         - |         - |    38.02 KB |
|                         |                  |                     |               |              |              |       |         |           |           |           |             |
|     **JsonReaderBenchmark** |              **100** |          **RoundRobin** |     **486.69 μs** |     **9.342 μs** |     **9.175 μs** |  **1.32** |    **0.03** |   **20.5078** |    **0.9766** |         **-** |    **83.88 KB** |
| SystemTextJsonBenchmark |              100 |          RoundRobin |     367.92 μs |     5.094 μs |     4.516 μs |  1.00 |    0.00 |   39.0625 |    5.8594 |         - |   161.09 KB |
|                         |                  |                     |               |              |              |       |         |           |           |           |             |
|     **JsonReaderBenchmark** |              **100** |    **MultiPolygonOnly** |     **960.07 μs** |    **16.182 μs** |    **15.137 μs** |  **1.11** |    **0.02** |   **47.8516** |   **15.6250** |         **-** |   **199.16 KB** |
| SystemTextJsonBenchmark |              100 |    MultiPolygonOnly |     862.43 μs |     9.172 μs |     8.580 μs |  1.00 |    0.00 |   83.9844 |   25.3906 |         - |   373.46 KB |
|                         |                  |                     |               |              |              |       |         |           |           |           |             |
|     **JsonReaderBenchmark** |             **1000** |          **RoundRobin** |   **5,007.96 μs** |    **86.742 μs** |    **76.894 μs** |  **1.26** |    **0.02** |  **132.8125** |   **62.5000** |         **-** |   **844.31 KB** |
| SystemTextJsonBenchmark |             1000 |          RoundRobin |   3,968.88 μs |    52.979 μs |    46.965 μs |  1.00 |    0.00 |  257.8125 |  125.0000 |         - |  1609.24 KB |
|                         |                  |                     |               |              |              |       |         |           |           |           |             |
|     **JsonReaderBenchmark** |             **1000** |    **MultiPolygonOnly** |  **10,259.01 μs** |   **201.294 μs** |   **178.442 μs** |  **0.99** |    **0.02** |  **312.5000** |  **156.2500** |         **-** |  **1985.12 KB** |
| SystemTextJsonBenchmark |             1000 |    MultiPolygonOnly |  10,358.38 μs |   174.165 μs |   154.393 μs |  1.00 |    0.00 |  593.7500 |  296.8750 |         - |  3713.12 KB |
|                         |                  |                     |               |              |              |       |         |           |           |           |             |
|     **JsonReaderBenchmark** |            **10000** |          **RoundRobin** |  **60,955.24 μs** | **1,119.826 μs** | **1,149.979 μs** |  **1.00** |    **0.03** | **1444.4444** |  **444.4444** |  **111.1111** |  **8537.14 KB** |
| SystemTextJsonBenchmark |            10000 |          RoundRobin |  61,381.19 μs |   849.500 μs |   709.371 μs |  1.00 |    0.00 | 2888.8889 | 1444.4444 |  444.4444 | 16470.32 KB |
|                         |                  |                     |               |              |              |       |         |           |           |           |             |
|     **JsonReaderBenchmark** |            **10000** |    **MultiPolygonOnly** | **128,842.65 μs** | **2,549.790 μs** | **3,574.449 μs** |  **0.90** |    **0.03** | **3500.0000** | **1500.0000** |  **500.0000** | **19945.18 KB** |
| SystemTextJsonBenchmark |            10000 |    MultiPolygonOnly | 142,806.56 μs | 2,738.989 μs | 3,044.380 μs |  1.00 |    0.00 | 6750.0000 | 3000.0000 | 1000.0000 | 37511.51 KB |


[`Utf8JsonReader`]: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-use-dom-utf8jsonreader-utf8jsonwriter?pivots=dotnet-6-0#use-utf8jsonreader
[`JsonSerializer`]: https://docs.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializer
[`JsonConverter`]: https://docs.microsoft.com/en-us/dotnet/api/system.text.json.serialization.jsonconverter?view=net-6.0
[x-strict-json]: https://github.com/JamesNK/Newtonsoft.Json/issues/646#issuecomment-356194475
[deserialize]: https://docs.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializer.deserialize?view=net-6.0
[Newtonsoft.Json (13.x)]: https://www.nuget.org/packages/Newtonsoft.Json/13.0.1
