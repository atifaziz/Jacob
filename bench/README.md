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

When benchmarking deserialization a subset of an example payload from the [GitHub REST API](https://docs.github.com/en/rest/branches/branches#merge-a-branch), we measure the following:

    BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.819)
    Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
    .NET SDK=7.0.100
      [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
      Job-QOSTER : .NET 6.0.11 (6.0.1122.52304), X64 RyuJIT AVX2
      Job-NCTTVP : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
      Job-YSQJJI : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2


| Method                  | Runtime       | ObjectCount |          Mean |        Error |        StdDev |        Median | Ratio | RatioSD |      Gen0 |      Gen1 |     Gen2 |   Allocated | Alloc Ratio |
| ----------------------- | ------------- | ----------- | ------------: | -----------: | ------------: | ------------: | ----: | ------: | --------: | --------: | -------: | ----------: | ----------: |
| JsonReaderBenchmark     | .NET 6.0      | 10          |     202.34 us |    20.552 us |     60.598 us |     190.91 us |  1.99 |    0.64 |    2.4414 |         - |        - |    11.41 KB |        0.67 |
| SystemTextJsonBenchmark | .NET 6.0      | 10          |     117.39 us |     4.646 us |     13.553 us |     113.90 us |  1.13 |    0.17 |    4.1504 |    0.2441 |        - |    17.25 KB |        1.02 |
| JsonReaderBenchmark     | .NET 7.0      | 10          |     112.36 us |     3.832 us |     11.118 us |     110.38 us |  1.08 |    0.17 |    2.6855 |         - |        - |    11.41 KB |        0.67 |
| SystemTextJsonBenchmark | .NET 7.0      | 10          |      98.76 us |     2.991 us |      8.773 us |      96.94 us |  0.95 |    0.14 |    4.1504 |         - |        - |    16.97 KB |        1.00 |
| JsonReaderBenchmark     | NativeAOT 7.0 | 10          |     168.08 us |    25.396 us |     74.880 us |     132.77 us |  1.63 |    0.68 |    2.4414 |         - |        - |    11.41 KB |        0.67 |
| SystemTextJsonBenchmark | NativeAOT 7.0 | 10          |     105.50 us |     4.485 us |     12.797 us |     104.44 us |  1.00 |    0.00 |    4.1504 |         - |        - |    16.97 KB |        1.00 |
|                         |               |             |               |              |               |               |       |         |           |           |          |             |             |
| JsonReaderBenchmark     | .NET 6.0      | 100         |   1,240.36 us |    36.970 us |    106.667 us |   1,196.85 us |  1.08 |    0.15 |   25.3906 |    7.8125 |        - |   111.67 KB |        0.69 |
| SystemTextJsonBenchmark | .NET 6.0      | 100         |   1,144.78 us |    37.520 us |    108.255 us |   1,109.01 us |  1.00 |    0.13 |   37.1094 |   11.7188 |        - |   164.66 KB |        1.02 |
| JsonReaderBenchmark     | .NET 7.0      | 100         |   1,185.21 us |    40.298 us |    114.973 us |   1,145.08 us |  1.04 |    0.17 |   25.3906 |    7.8125 |        - |   111.67 KB |        0.69 |
| SystemTextJsonBenchmark | .NET 7.0      | 100         |   1,109.15 us |    35.183 us |    102.073 us |   1,068.34 us |  0.97 |    0.14 |   35.1563 |    9.7656 |        - |   161.56 KB |        1.00 |
| JsonReaderBenchmark     | NativeAOT 7.0 | 100         |   1,114.53 us |    29.102 us |     80.156 us |   1,083.46 us |  0.97 |    0.13 |   23.4375 |    7.8125 |        - |   111.67 KB |        0.69 |
| SystemTextJsonBenchmark | NativeAOT 7.0 | 100         |   1,159.15 us |    52.550 us |    153.290 us |   1,103.81 us |  1.00 |    0.00 |   33.2031 |    9.7656 |        - |   161.56 KB |        1.00 |
|                         |               |             |               |              |               |               |       |         |           |           |          |             |             |
| JsonReaderBenchmark     | .NET 6.0      | 1000        |  13,449.47 us |   462.489 us |  1,341.763 us |  13,214.85 us |  0.95 |    0.15 |  171.8750 |   78.1250 |        - |  1110.12 KB |        0.70 |
| SystemTextJsonBenchmark | .NET 6.0      | 1000        |  15,895.95 us |   942.387 us |  2,748.988 us |  15,849.49 us |  1.13 |    0.27 |  250.0000 |  125.0000 |        - |  1619.96 KB |        1.02 |
| JsonReaderBenchmark     | .NET 7.0      | 1000        |  15,377.16 us |   479.259 us |  1,320.019 us |  15,327.51 us |  1.08 |    0.17 |  171.8750 |  156.2500 |        - |  1110.12 KB |        0.70 |
| SystemTextJsonBenchmark | .NET 7.0      | 1000        |  14,739.72 us |   508.641 us |  1,459.387 us |  14,526.50 us |  1.04 |    0.15 |  250.0000 |  234.3750 |        - |  1588.75 KB |        1.00 |
| JsonReaderBenchmark     | NativeAOT 7.0 | 1000        |  18,554.27 us | 1,701.470 us |  4,963.269 us |  16,851.99 us |  1.32 |    0.40 |  171.8750 |  156.2500 |        - |  1110.12 KB |        0.70 |
| SystemTextJsonBenchmark | NativeAOT 7.0 | 1000        |  14,366.94 us |   629.301 us |  1,835.700 us |  13,511.67 us |  1.00 |    0.00 |  250.0000 |  218.7500 |        - |  1588.76 KB |        1.00 |
|                         |               |             |               |              |               |               |       |         |           |           |          |             |             |
| JsonReaderBenchmark     | .NET 6.0      | 10000       | 168,778.99 us | 6,104.380 us | 17,903.090 us | 160,740.15 us |  1.20 |    0.19 | 2000.0000 | 1000.0000 | 250.0000 | 11194.13 KB |        0.68 |
| SystemTextJsonBenchmark | .NET 6.0      | 10000       | 168,772.76 us | 6,891.284 us | 20,102.200 us | 159,809.92 us |  1.19 |    0.21 | 2500.0000 | 1000.0000 |        - | 16672.09 KB |        1.02 |
| JsonReaderBenchmark     | .NET 7.0      | 10000       | 131,258.49 us | 3,662.383 us | 10,148.446 us | 129,675.98 us |  0.93 |    0.12 | 2000.0000 | 1000.0000 | 250.0000 | 11194.13 KB |        0.68 |
| SystemTextJsonBenchmark | .NET 7.0      | 10000       | 138,900.25 us | 5,720.888 us | 16,778.376 us | 133,623.48 us |  0.99 |    0.17 | 2500.0000 | 1250.0000 | 250.0000 | 16359.59 KB |        1.00 |
| JsonReaderBenchmark     | NativeAOT 7.0 | 10000       | 130,683.21 us | 4,577.537 us | 13,207.241 us | 126,413.93 us |  0.93 |    0.15 | 2000.0000 | 1000.0000 | 250.0000 | 11194.13 KB |        0.68 |
| SystemTextJsonBenchmark | NativeAOT 7.0 | 10000       | 142,635.96 us | 6,687.071 us | 18,970.107 us | 135,900.80 us |  1.00 |    0.00 | 2500.0000 | 1250.0000 | 250.0000 |  16360.5 KB |        1.00 |
