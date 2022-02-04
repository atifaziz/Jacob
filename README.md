# Jacob

Jacob provides a succinct JSON reading combinator API over `Utf8JsonReader` for
deserializing/reading JSON data.

.NET Core 3.0 introduced [`Utf8JsonReader`], a high-performance, low-allocation,
forward-only reader for JSON text encoded in UTF-8. It is a low-level type for
building custom parsers and deserializers and because it has _low-level_
semantics, its API is not very straightforward to use. It's far more common
instead to see developers create simple classes that mirror the JSON data and
then use [`JsonSerializer`] to deserialize/read the JSON data into instances of
those classes. However, when your application or domain object model is far
richer than the values types available in JSON, you need to customize the
deserialization by writing and registering a [`JsonConverter`] that requires a
non-trivial amount of coding and understanding, not to mention having to work
with `Utf8JsonReader` and writing unit tests for each converter. Moreover, the
converters you create are not immediately reusable and composable (in an ad-hoc
manner or otherwise) without considerable effort. Jacob provides most of the
benefits of `Utf8JsonReader`, such as low-allocation, without all the ceremony
and complexity associated.


[`Utf8JsonReader`]: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-use-dom-utf8jsonreader-utf8jsonwriter?pivots=dotnet-6-0#use-utf8jsonreader
[`JsonSerializer`]: https://docs.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializer
[`JsonConverter`]: https://docs.microsoft.com/en-us/dotnet/api/system.text.json.serialization.jsonconverter?view=net-6.0
