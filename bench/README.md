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

    BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.819)
    Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
    .NET SDK=7.0.100
      [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
      Job-IBGVMK : .NET 6.0.11 (6.0.1122.52304), X64 RyuJIT AVX2
      Job-TTWDSU : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
      Job-XACLOO : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2

| Method                  | Runtime       | ObjectCount | SampleSet    |          Mean |        Error |       StdDev | Ratio | RatioSD |      Gen0 |      Gen1 |      Gen2 |   Allocated | Alloc Ratio |
| ----------------------- | ------------- | ----------- | ------------ | ------------: | -----------: | -----------: | ----: | ------: | --------: | --------: | --------: | ----------: | ----------: |
| JsonReaderBenchmark     | .NET 6.0      | 10          | All          |      46.14 μs |     0.883 μs |     1.179 μs |  1.37 |    0.06 |    1.8921 |         - |         - |     7.83 KB |        0.50 |
| SystemTextJsonBenchmark | .NET 6.0      | 10          | All          |      36.19 μs |     0.714 μs |     1.153 μs |  1.09 |    0.06 |    3.7842 |         - |         - |     15.7 KB |        1.00 |
| JsonReaderBenchmark     | .NET 7.0      | 10          | All          |      45.87 μs |     0.907 μs |     2.155 μs |  1.37 |    0.09 |    1.8921 |         - |         - |     7.83 KB |        0.50 |
| SystemTextJsonBenchmark | .NET 7.0      | 10          | All          |      34.14 μs |     0.676 μs |     1.805 μs |  1.02 |    0.06 |    3.8452 |         - |         - |    15.73 KB |        1.00 |
| JsonReaderBenchmark     | NativeAOT 7.0 | 10          | All          |      45.52 μs |     0.910 μs |     1.011 μs |  1.36 |    0.05 |    1.8921 |         - |         - |     7.83 KB |        0.50 |
| SystemTextJsonBenchmark | NativeAOT 7.0 | 10          | All          |      33.32 μs |     0.661 μs |     1.364 μs |  1.00 |    0.00 |    3.8452 |         - |         - |    15.73 KB |        1.00 |
|                         |               |             |              |               |              |              |       |         |           |           |           |             |             |
| JsonReaderBenchmark     | .NET 6.0      | 10          | MultiPolygon |      99.51 μs |     1.948 μs |     3.089 μs |  1.19 |    0.08 |    4.8828 |    0.1221 |         - |    20.16 KB |        0.53 |
| SystemTextJsonBenchmark | .NET 6.0      | 10          | MultiPolygon |      89.99 μs |     1.796 μs |     3.828 μs |  1.08 |    0.07 |    9.2773 |         - |         - |    38.02 KB |        1.00 |
| JsonReaderBenchmark     | .NET 7.0      | 10          | MultiPolygon |      96.79 μs |     1.778 μs |     2.493 μs |  1.17 |    0.08 |    4.8828 |         - |         - |    20.16 KB |        0.53 |
| SystemTextJsonBenchmark | .NET 7.0      | 10          | MultiPolygon |      83.68 μs |     1.667 μs |     4.421 μs |  1.01 |    0.07 |    9.2773 |    0.1221 |         - |    38.05 KB |        1.00 |
| JsonReaderBenchmark     | NativeAOT 7.0 | 10          | MultiPolygon |      97.53 μs |     1.937 μs |     4.931 μs |  1.18 |    0.10 |    4.8828 |         - |         - |    20.16 KB |        0.53 |
| SystemTextJsonBenchmark | NativeAOT 7.0 | 10          | MultiPolygon |      83.18 μs |     1.651 μs |     4.603 μs |  1.00 |    0.00 |    9.2773 |    0.1221 |         - |    38.05 KB |        1.00 |
|                         |               |             |              |               |              |              |       |         |           |           |           |             |             |
| JsonReaderBenchmark     | .NET 6.0      | 100         | All          |     526.45 μs |    10.437 μs |    24.190 μs |  1.46 |    0.11 |   20.5078 |    0.9766 |         - |    83.88 KB |        0.52 |
| SystemTextJsonBenchmark | .NET 6.0      | 100         | All          |     388.72 μs |     7.755 μs |    16.527 μs |  1.07 |    0.08 |   39.0625 |    3.4180 |         - |   161.09 KB |        1.00 |
| JsonReaderBenchmark     | .NET 7.0      | 100         | All          |     525.17 μs |    10.467 μs |    19.915 μs |  1.45 |    0.12 |   20.5078 |    0.9766 |         - |    83.88 KB |        0.52 |
| SystemTextJsonBenchmark | .NET 7.0      | 100         | All          |     364.38 μs |     7.255 μs |    13.083 μs |  1.00 |    0.07 |   39.0625 |    9.7656 |         - |   161.13 KB |        1.00 |
| JsonReaderBenchmark     | NativeAOT 7.0 | 100         | All          |     504.33 μs |    10.086 μs |    27.267 μs |  1.39 |    0.11 |   20.5078 |    0.9766 |         - |    83.88 KB |        0.52 |
| SystemTextJsonBenchmark | NativeAOT 7.0 | 100         | All          |     364.79 μs |     7.257 μs |    20.350 μs |  1.00 |    0.00 |   39.0625 |    9.7656 |         - |   161.13 KB |        1.00 |
|                         |               |             |              |               |              |              |       |         |           |           |           |             |             |
| JsonReaderBenchmark     | .NET 6.0      | 100         | MultiPolygon |   1,019.00 μs |    20.298 μs |    46.637 μs |  1.25 |    0.07 |   46.8750 |   15.6250 |         - |   199.17 KB |        0.53 |
| SystemTextJsonBenchmark | .NET 6.0      | 100         | MultiPolygon |     922.65 μs |    18.362 μs |    48.694 μs |  1.14 |    0.06 |   83.0078 |   25.3906 |         - |   373.45 KB |        1.00 |
| JsonReaderBenchmark     | .NET 7.0      | 100         | MultiPolygon |     990.79 μs |    19.510 μs |    46.369 μs |  1.23 |    0.07 |   47.8516 |   15.6250 |         - |   199.16 KB |        0.53 |
| SystemTextJsonBenchmark | .NET 7.0      | 100         | MultiPolygon |     845.85 μs |    16.686 μs |    33.324 μs |  1.04 |    0.05 |   82.0313 |   37.1094 |         - |   373.48 KB |        1.00 |
| JsonReaderBenchmark     | NativeAOT 7.0 | 100         | MultiPolygon |     994.97 μs |    19.737 μs |    35.083 μs |  1.23 |    0.05 |   47.8516 |   15.6250 |         - |   199.16 KB |        0.53 |
| SystemTextJsonBenchmark | NativeAOT 7.0 | 100         | MultiPolygon |     809.81 μs |    15.948 μs |    22.356 μs |  1.00 |    0.00 |   83.0078 |   33.2031 |         - |   373.48 KB |        1.00 |
|                         |               |             |              |               |              |              |       |         |           |           |           |             |             |
| JsonReaderBenchmark     | .NET 6.0      | 1000        | All          |   5,093.26 μs |    80.493 μs |    71.355 μs |  1.38 |    0.03 |  132.8125 |   62.5000 |         - |   844.31 KB |        0.52 |
| SystemTextJsonBenchmark | .NET 6.0      | 1000        | All          |   4,069.82 μs |    80.943 μs |    79.497 μs |  1.11 |    0.02 |  257.8125 |  125.0000 |         - |  1609.24 KB |        1.00 |
| JsonReaderBenchmark     | .NET 7.0      | 1000        | All          |   4,941.00 μs |    81.365 μs |    76.109 μs |  1.34 |    0.03 |  132.8125 |  125.0000 |         - |   844.31 KB |        0.52 |
| SystemTextJsonBenchmark | .NET 7.0      | 1000        | All          |   3,668.58 μs |    55.642 μs |    49.325 μs |  1.00 |    0.02 |  261.7188 |  257.8125 |         - |  1609.27 KB |        1.00 |
| JsonReaderBenchmark     | NativeAOT 7.0 | 1000        | All          |   4,899.00 μs |    89.187 μs |    83.426 μs |  1.33 |    0.03 |  132.8125 |  125.0000 |         - |   844.31 KB |        0.52 |
| SystemTextJsonBenchmark | NativeAOT 7.0 | 1000        | All          |   3,677.57 μs |    60.946 μs |    57.009 μs |  1.00 |    0.00 |  261.7188 |  257.8125 |         - |  1609.27 KB |        1.00 |
|                         |               |             |              |               |              |              |       |         |           |           |           |             |             |
| JsonReaderBenchmark     | .NET 6.0      | 1000        | MultiPolygon |  10,301.49 μs |   147.689 μs |   130.922 μs |  1.07 |    0.02 |  312.5000 |  156.2500 |         - |  1985.12 KB |        0.53 |
| SystemTextJsonBenchmark | .NET 6.0      | 1000        | MultiPolygon |  10,380.42 μs |   207.354 μs |   221.867 μs |  1.07 |    0.04 |  593.7500 |  296.8750 |         - |  3713.12 KB |        1.00 |
| JsonReaderBenchmark     | .NET 7.0      | 1000        | MultiPolygon |   9,756.26 μs |   189.087 μs |   185.708 μs |  1.01 |    0.03 |  312.5000 |  296.8750 |         - |  1985.12 KB |        0.53 |
| SystemTextJsonBenchmark | .NET 7.0      | 1000        | MultiPolygon |   9,409.72 μs |   181.121 μs |   169.421 μs |  0.98 |    0.03 |  593.7500 |  578.1250 |         - |  3713.15 KB |        1.00 |
| JsonReaderBenchmark     | NativeAOT 7.0 | 1000        | MultiPolygon |   9,895.79 μs |   178.855 μs |   158.550 μs |  1.03 |    0.02 |  312.5000 |  296.8750 |         - |  1985.12 KB |        0.53 |
| SystemTextJsonBenchmark | NativeAOT 7.0 | 1000        | MultiPolygon |   9,680.68 μs |   189.996 μs |   218.800 μs |  1.00 |    0.00 |  593.7500 |  578.1250 |         - |  3713.15 KB |        1.00 |
|                         |               |             |              |               |              |              |       |         |           |           |           |             |             |
| JsonReaderBenchmark     | .NET 6.0      | 10000       | All          |  63,113.40 μs | 1,140.368 μs | 1,066.701 μs |  0.95 |    0.04 | 1375.0000 |  500.0000 |  125.0000 |  8536.96 KB |        0.52 |
| SystemTextJsonBenchmark | .NET 6.0      | 10000       | All          |  60,578.91 μs | 1,211.539 μs | 1,850.148 μs |  0.92 |    0.04 | 2750.0000 | 1250.0000 |  375.0000 | 16469.52 KB |        1.00 |
| JsonReaderBenchmark     | .NET 7.0      | 10000       | All          |  60,089.65 μs | 1,164.006 μs | 1,777.559 μs |  0.91 |    0.04 | 1444.4444 |  555.5556 |  111.1111 |  8537.14 KB |        0.52 |
| SystemTextJsonBenchmark | .NET 7.0      | 10000       | All          |  65,042.87 μs | 1,295.430 μs | 2,091.877 μs |  0.99 |    0.05 | 3000.0000 | 1777.7778 |  555.5556 | 16470.91 KB |        1.00 |
| JsonReaderBenchmark     | NativeAOT 7.0 | 10000       | All          |  60,257.12 μs | 1,120.240 μs | 1,047.873 μs |  0.91 |    0.03 | 1444.4444 |  555.5556 |  111.1111 |  8536.96 KB |        0.52 |
| SystemTextJsonBenchmark | NativeAOT 7.0 | 10000       | All          |  65,941.92 μs | 1,301.190 μs | 2,244.488 μs |  1.00 |    0.00 | 3000.0000 | 1777.7778 |  555.5556 | 16470.68 KB |        1.00 |
|                         |               |             |              |               |              |              |       |         |           |           |           |             |             |
| JsonReaderBenchmark     | .NET 6.0      | 10000       | MultiPolygon | 130,158.13 μs | 2,516.953 μs | 3,918.593 μs |  1.02 |    0.04 | 3500.0000 | 1500.0000 |  500.0000 | 19945.64 KB |        0.53 |
| SystemTextJsonBenchmark | .NET 6.0      | 10000       | MultiPolygon | 146,698.48 μs | 2,909.600 μs | 6,073.419 μs |  1.16 |    0.06 | 6750.0000 | 3000.0000 | 1000.0000 | 37510.12 KB |        1.00 |
| JsonReaderBenchmark     | .NET 7.0      | 10000       | MultiPolygon | 119,218.10 μs | 2,369.504 μs | 3,243.404 μs |  0.94 |    0.03 | 3250.0000 | 1250.0000 |  250.0000 | 19945.11 KB |        0.53 |
| SystemTextJsonBenchmark | .NET 7.0      | 10000       | MultiPolygon | 127,485.37 μs | 2,488.375 μs | 4,292.325 μs |  1.00 |    0.04 | 6500.0000 | 3000.0000 |  750.0000 |  37511.3 KB |        1.00 |
| JsonReaderBenchmark     | NativeAOT 7.0 | 10000       | MultiPolygon | 123,240.28 μs | 2,443.043 μs | 2,508.826 μs |  0.97 |    0.04 | 3600.0000 | 1400.0000 |  400.0000 | 19945.28 KB |        0.53 |
| SystemTextJsonBenchmark | NativeAOT 7.0 | 10000       | MultiPolygon | 127,045.45 μs | 2,512.918 μs | 3,761.218 μs |  1.00 |    0.00 | 6500.0000 | 3000.0000 |  750.0000 | 37510.46 KB |        1.00 |

When benchmarking deserialization a subset of an example payload from the
[GitHub REST API (Merge a branch)], we measure the following:

    BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.819)
    Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
    .NET SDK=7.0.100
      [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
      Job-OVWNPR : .NET 6.0.11 (6.0.1122.52304), X64 RyuJIT AVX2
      Job-KJBBGH : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
      Job-CKYCTW : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2


|                  Method |       Runtime | ObjectCount |         Mean |        Error |       StdDev |       Median | Ratio | RatioSD |       Gen0 |      Gen1 |      Gen2 |   Allocated | Alloc Ratio |
|------------------------ |-------------- |------------ |-------------:|-------------:|-------------:|-------------:|------:|--------:|-----------:|----------:|----------:|------------:|------------:|
|     JsonReaderBenchmark |      .NET 6.0 |          10 |     361.1 us |     11.90 us |     33.17 us |     353.0 us |  1.23 |    0.21 |    19.5313 |    5.8594 |         - |    83.67 KB |        0.85 |
| SystemTextJsonBenchmark |      .NET 6.0 |          10 |     249.3 us |      4.98 us |     13.64 us |     248.7 us |  0.84 |    0.12 |    23.9258 |    7.8125 |         - |     98.9 KB |        1.01 |
|     JsonReaderBenchmark |      .NET 7.0 |          10 |     327.6 us |     10.28 us |     29.32 us |     318.4 us |  1.12 |    0.19 |    20.0195 |    1.9531 |         - |    83.67 KB |        0.85 |
| SystemTextJsonBenchmark |      .NET 7.0 |          10 |     285.3 us |     13.16 us |     35.79 us |     273.4 us |  0.96 |    0.18 |    23.9258 |    7.8125 |         - |    98.07 KB |        1.00 |
|     JsonReaderBenchmark | NativeAOT 7.0 |          10 |     406.9 us |     34.08 us |     94.44 us |     371.2 us |  1.37 |    0.30 |    20.0195 |    1.9531 |         - |    83.67 KB |        0.85 |
| SystemTextJsonBenchmark | NativeAOT 7.0 |          10 |     298.6 us |     16.48 us |     46.50 us |     285.7 us |  1.00 |    0.00 |    23.9258 |    7.8125 |         - |    98.07 KB |        1.00 |
|                         |               |             |              |              |              |              |       |         |            |           |           |             |             |
|     JsonReaderBenchmark |      .NET 6.0 |         100 |   3,867.9 us |    164.67 us |    456.31 us |   3,714.0 us |  1.28 |    0.23 |   132.8125 |   66.4063 |         - |   834.32 KB |        0.85 |
| SystemTextJsonBenchmark |      .NET 6.0 |         100 |   3,363.0 us |    219.58 us |    615.72 us |   3,143.9 us |  1.12 |    0.27 |   160.1563 |   78.1250 |         - |   994.96 KB |        1.01 |
|     JsonReaderBenchmark |      .NET 7.0 |         100 |   3,781.9 us |    131.40 us |    374.89 us |   3,689.5 us |  1.25 |    0.22 |   132.8125 |  128.9063 |         - |   834.32 KB |        0.85 |
| SystemTextJsonBenchmark |      .NET 7.0 |         100 |   3,047.9 us |    154.99 us |    439.68 us |   2,928.2 us |  0.99 |    0.19 |   160.1563 |  156.2500 |         - |   986.39 KB |        1.00 |
|     JsonReaderBenchmark | NativeAOT 7.0 |         100 |   3,733.0 us |    165.12 us |    465.73 us |   3,628.0 us |  1.25 |    0.24 |   132.8125 |  125.0000 |         - |   834.32 KB |        0.85 |
| SystemTextJsonBenchmark | NativeAOT 7.0 |         100 |   3,085.3 us |    185.14 us |    503.69 us |   2,935.6 us |  1.00 |    0.00 |   160.1563 |  156.2500 |         - |   986.39 KB |        1.00 |
|                         |               |             |              |              |              |              |       |         |            |           |           |             |             |
|     JsonReaderBenchmark |      .NET 6.0 |        1000 |  48,127.1 us |  2,503.79 us |  7,102.84 us |  45,877.2 us |  0.98 |    0.21 |  1400.0000 |  500.0000 |  100.0000 |  8336.78 KB |        0.85 |
| SystemTextJsonBenchmark |      .NET 6.0 |        1000 |  44,739.9 us |  1,663.72 us |  4,800.22 us |  43,065.2 us |  0.91 |    0.18 |  1727.2727 |  727.2727 |  272.7273 |  9875.11 KB |        1.01 |
|     JsonReaderBenchmark |      .NET 7.0 |        1000 |  53,535.8 us |  3,335.49 us |  9,676.85 us |  49,938.4 us |  1.09 |    0.25 |  1555.5556 |  888.8889 |  222.2222 |  8336.96 KB |        0.85 |
| SystemTextJsonBenchmark |      .NET 7.0 |        1000 |  45,374.4 us |  1,225.50 us |  3,516.18 us |  44,875.0 us |  0.92 |    0.16 |  1833.3333 | 1083.3333 |  333.3333 |  9789.81 KB |        1.00 |
|     JsonReaderBenchmark | NativeAOT 7.0 |        1000 |  53,250.5 us |  2,668.28 us |  7,569.46 us |  50,447.6 us |  1.09 |    0.26 |  1500.0000 |  875.0000 |  250.0000 |  8337.59 KB |        0.85 |
| SystemTextJsonBenchmark | NativeAOT 7.0 |        1000 |  50,615.8 us |  3,317.44 us |  9,356.91 us |  48,137.7 us |  1.00 |    0.00 |  1818.1818 | 1000.0000 |  272.7273 |  9789.16 KB |        1.00 |
|                         |               |             |              |              |              |              |       |         |            |           |           |             |             |
|     JsonReaderBenchmark |      .NET 6.0 |       10000 | 473,801.2 us | 18,509.33 us | 51,902.14 us | 455,522.1 us |  1.24 |    0.19 | 13000.0000 | 5000.0000 |         - | 83459.95 KB |        0.86 |
| SystemTextJsonBenchmark |      .NET 6.0 |       10000 | 402,131.6 us | 12,635.45 us | 36,049.65 us | 391,502.2 us |  1.05 |    0.12 | 15000.0000 | 5000.0000 |         - | 98321.43 KB |        1.01 |
|     JsonReaderBenchmark |      .NET 7.0 |       10000 | 435,829.0 us | 13,578.17 us | 39,392.74 us | 421,647.1 us |  1.14 |    0.15 | 14000.0000 | 7000.0000 | 1000.0000 | 83460.31 KB |        0.86 |
| SystemTextJsonBenchmark |      .NET 7.0 |       10000 | 378,523.2 us | 12,743.50 us | 37,173.38 us | 362,073.7 us |  1.00 |    0.15 | 16000.0000 | 8000.0000 | 1000.0000 |  97462.8 KB |        1.00 |
|     JsonReaderBenchmark | NativeAOT 7.0 |       10000 | 430,449.1 us | 16,561.16 us | 47,249.91 us | 412,971.2 us |  1.13 |    0.17 | 14000.0000 | 7000.0000 | 1000.0000 | 83462.89 KB |        0.86 |
| SystemTextJsonBenchmark | NativeAOT 7.0 |       10000 | 384,891.2 us | 14,468.88 us | 41,280.54 us | 367,407.8 us |  1.00 |    0.00 | 16000.0000 | 8000.0000 | 1000.0000 | 97462.79 KB |        1.00 |

  [GitHub REST API]: https://docs.github.com/en/rest/branches/branches#merge-a-branch
