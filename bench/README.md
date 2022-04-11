# Benchmarks

With a benchmark on GeoJSON data deserialization we exercise deserialization
of a real-world format and compare Jacob's performance to `System.Text.Json`s
performance. As the GeoJSON specification is best represented in C# with type
hierarchies (polymorphic deserialization), it is a good example of how to use
`JsonReader.Either` to express polymorphic deserialization succinctly. We
compare this with a naive approach that is achieving the same thing in
`System.Text.Json`. We are aware that polymorphic deserialization is supported
in `System.Text.Json` using custom converters, but did not want to compare
more low-level/extensive deserialization code with Jacob's high-level API.

By exercising different sample sets of geometries in the GeoJSON array, in one
case we distribute all geometries using a round-robin distribution mechanism.
In another configuration, we benchmark the worst-case scenario performance of
`JsonReader.Either` by deserializing an array with elements of type
`MultiPolygon`. We measure the following:

    BenchmarkDotNet=v0.12.1, OS=Windows 10.0.22000
    Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
    .NET Core SDK=6.0.201
      [Host]     : .NET Core 6.0.3 (CoreCLR 6.0.322.12309, CoreFX 6.0.322.12309), X64 RyuJIT
      DefaultJob : .NET Core 6.0.3 (CoreCLR 6.0.322.12309, CoreFX 6.0.322.12309), X64 RyuJIT

|                  Method | ObjectCount |    SampleSet |          Mean |        Error |       StdDev | Ratio | RatioSD |     Gen 0 |     Gen 1 |     Gen 2 |   Allocated |
|------------------------ |------------ |--------------|--------------:|-------------:|-------------:|------:|--------:|----------:|----------:|----------:|------------:|
|     **JsonReaderBenchmark** |          **10** |          **All** |      **46.12 μs** |     **0.882 μs** |     **1.115 μs** |  **1.32** |    **0.04** |    **1.8921** |         **-** |         **-** |     **7.83 KB** |
| SystemTextJsonBenchmark |          10 |          All |      35.06 μs |     0.673 μs |     0.661 μs |  1.00 |    0.00 |    3.7842 |         - |         - |     15.7 KB |
|                         |             |                     |               |              |              |       |         |           |           |           |             |
|     **JsonReaderBenchmark** |          **10** | **MultiPolygon** |      **93.75 μs** |     **1.806 μs** |     **1.855 μs** |  **1.06** |    **0.04** |    **4.8828** |    **0.1221** |         **-** |    **20.16 KB** |
| SystemTextJsonBenchmark |          10 | MultiPolygon |      87.64 μs |     1.749 μs |     3.109 μs |  1.00 |    0.00 |    9.2773 |         - |         - |    38.02 KB |
|                         |             |                     |               |              |              |       |         |           |           |           |             |
|     **JsonReaderBenchmark** |         **100** |          **All** |     **486.69 μs** |     **9.342 μs** |     **9.175 μs** |  **1.32** |    **0.03** |   **20.5078** |    **0.9766** |         **-** |    **83.88 KB** |
| SystemTextJsonBenchmark |         100 |          All |     367.92 μs |     5.094 μs |     4.516 μs |  1.00 |    0.00 |   39.0625 |    5.8594 |         - |   161.09 KB |
|                         |             |                     |               |              |              |       |         |           |           |           |             |
|     **JsonReaderBenchmark** |         **100** | **MultiPolygon** |     **960.07 μs** |    **16.182 μs** |    **15.137 μs** |  **1.11** |    **0.02** |   **47.8516** |   **15.6250** |         **-** |   **199.16 KB** |
| SystemTextJsonBenchmark |         100 | MultiPolygon |     862.43 μs |     9.172 μs |     8.580 μs |  1.00 |    0.00 |   83.9844 |   25.3906 |         - |   373.46 KB |
|                         |             |                     |               |              |              |       |         |           |           |           |             |
|     **JsonReaderBenchmark** |        **1000** |          **All** |   **5,007.96 μs** |    **86.742 μs** |    **76.894 μs** |  **1.26** |    **0.02** |  **132.8125** |   **62.5000** |         **-** |   **844.31 KB** |
| SystemTextJsonBenchmark |        1000 |          All |   3,968.88 μs |    52.979 μs |    46.965 μs |  1.00 |    0.00 |  257.8125 |  125.0000 |         - |  1609.24 KB |
|                         |             |                     |               |              |              |       |         |           |           |           |             |
|     **JsonReaderBenchmark** |        **1000** | **MultiPolygon** |  **10,259.01 μs** |   **201.294 μs** |   **178.442 μs** |  **0.99** |    **0.02** |  **312.5000** |  **156.2500** |         **-** |  **1985.12 KB** |
| SystemTextJsonBenchmark |        1000 | MultiPolygon |  10,358.38 μs |   174.165 μs |   154.393 μs |  1.00 |    0.00 |  593.7500 |  296.8750 |         - |  3713.12 KB |
|                         |             |                     |               |              |              |       |         |           |           |           |             |
|     **JsonReaderBenchmark** |       **10000** |          **All** |  **60,955.24 μs** | **1,119.826 μs** | **1,149.979 μs** |  **1.00** |    **0.03** | **1444.4444** |  **444.4444** |  **111.1111** |  **8537.14 KB** |
| SystemTextJsonBenchmark |       10000 |          All |  61,381.19 μs |   849.500 μs |   709.371 μs |  1.00 |    0.00 | 2888.8889 | 1444.4444 |  444.4444 | 16470.32 KB |
|                         |             |                     |               |              |              |       |         |           |           |           |             |
|     **JsonReaderBenchmark** |       **10000** | **MultiPolygon** | **128,842.65 μs** | **2,549.790 μs** | **3,574.449 μs** |  **0.90** |    **0.03** | **3500.0000** | **1500.0000** |  **500.0000** | **19945.18 KB** |
| SystemTextJsonBenchmark |       10000 | MultiPolygon | 142,806.56 μs | 2,738.989 μs | 3,044.380 μs |  1.00 |    0.00 | 6750.0000 | 3000.0000 | 1000.0000 | 37511.51 KB |
