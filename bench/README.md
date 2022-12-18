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

    BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.963)
    Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
    .NET SDK=7.0.101
      [Host]     : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
      Job-YZVNIW : .NET 6.0.12 (6.0.1222.56807), X64 RyuJIT AVX2
      Job-MRCBRH : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
      Job-DTLXOA : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2


|                  Method |       Runtime | ObjectCount |    SampleSet |          Mean |         Error |        StdDev |        Median | Ratio | RatioSD |      Gen0 |      Gen1 |      Gen2 |   Allocated | Alloc Ratio |
|------------------------ |-------------- |------------ |------------- |--------------:|--------------:|--------------:|--------------:|------:|--------:|----------:|----------:|----------:|------------:|------------:|
|     JsonReaderBenchmark |      .NET 6.0 |          10 |          All |     107.64 us |      4.443 us |     12.604 us |     106.22 us |  1.80 |    0.30 |    2.4414 |         - |         - |    10.02 KB |        0.61 |
| SystemTextJsonBenchmark |      .NET 6.0 |          10 |          All |      64.51 us |      3.668 us |     10.525 us |      64.49 us |  1.09 |    0.25 |    4.0283 |         - |         - |    16.52 KB |        1.00 |
|     JsonReaderBenchmark |      .NET 7.0 |          10 |          All |     115.35 us |      7.387 us |     21.666 us |     109.83 us |  1.96 |    0.51 |    2.4414 |         - |         - |    10.02 KB |        0.61 |
| SystemTextJsonBenchmark |      .NET 7.0 |          10 |          All |      89.88 us |      5.434 us |     16.023 us |      90.87 us |  1.51 |    0.33 |    4.0283 |         - |         - |    16.55 KB |        1.00 |
|     JsonReaderBenchmark | NativeAOT 7.0 |          10 |          All |     126.09 us |      7.309 us |     21.437 us |     126.91 us |  2.12 |    0.42 |    2.4414 |         - |         - |    10.02 KB |        0.61 |
| SystemTextJsonBenchmark | NativeAOT 7.0 |          10 |          All |      60.37 us |      2.882 us |      8.452 us |      58.99 us |  1.00 |    0.00 |    4.0283 |         - |         - |    16.55 KB |        1.00 |
|                         |               |             |              |               |               |               |               |       |         |           |           |           |             |             |
|     JsonReaderBenchmark |      .NET 6.0 |          10 | MultiPolygon |     277.33 us |     16.455 us |     47.475 us |     267.42 us |  1.69 |    0.38 |    6.3477 |         - |         - |    26.72 KB |        0.65 |
| SystemTextJsonBenchmark |      .NET 6.0 |          10 | MultiPolygon |     185.33 us |     18.201 us |     53.094 us |     173.14 us |  1.13 |    0.38 |    9.7656 |         - |         - |    41.39 KB |        1.00 |
|     JsonReaderBenchmark |      .NET 7.0 |          10 | MultiPolygon |     229.31 us |     16.622 us |     47.958 us |     221.21 us |  1.40 |    0.36 |    6.3477 |         - |         - |    26.72 KB |        0.65 |
| SystemTextJsonBenchmark |      .NET 7.0 |          10 | MultiPolygon |     164.54 us |     10.203 us |     29.764 us |     161.83 us |  1.00 |    0.23 |   10.0098 |         - |         - |    41.42 KB |        1.00 |
|     JsonReaderBenchmark | NativeAOT 7.0 |          10 | MultiPolygon |     273.79 us |     26.971 us |     78.675 us |     252.71 us |  1.67 |    0.52 |    6.3477 |         - |         - |    26.72 KB |        0.65 |
| SystemTextJsonBenchmark | NativeAOT 7.0 |          10 | MultiPolygon |     168.58 us |      9.231 us |     26.487 us |     164.39 us |  1.00 |    0.00 |   10.0098 |         - |         - |    41.42 KB |        1.00 |
|                         |               |             |              |               |               |               |               |       |         |           |           |           |             |             |
|     JsonReaderBenchmark |      .NET 6.0 |         100 |          All |   1,155.46 us |     63.990 us |    186.662 us |   1,143.43 us |  1.71 |    0.42 |   25.3906 |         - |         - |   107.19 KB |        0.63 |
| SystemTextJsonBenchmark |      .NET 6.0 |         100 |          All |     733.39 us |     51.815 us |    144.440 us |     701.48 us |  1.10 |    0.29 |   41.0156 |    3.9063 |         - |   170.35 KB |        1.00 |
|     JsonReaderBenchmark |      .NET 7.0 |         100 |          All |   1,266.09 us |    103.432 us |    303.347 us |   1,215.61 us |  1.90 |    0.61 |   25.3906 |    3.9063 |         - |   107.19 KB |        0.63 |
| SystemTextJsonBenchmark |      .NET 7.0 |         100 |          All |     646.42 us |     34.145 us |     96.308 us |     621.46 us |  0.96 |    0.21 |   41.0156 |    2.9297 |         - |   170.38 KB |        1.00 |
|     JsonReaderBenchmark | NativeAOT 7.0 |         100 |          All |   1,416.92 us |    117.686 us |    346.999 us |   1,317.38 us |  2.12 |    0.63 |   25.3906 |    3.9063 |         - |   107.19 KB |        0.63 |
| SystemTextJsonBenchmark | NativeAOT 7.0 |         100 |          All |     697.47 us |     41.941 us |    119.659 us |     676.30 us |  1.00 |    0.00 |   41.0156 |    2.9297 |         - |   170.38 KB |        1.00 |
|                         |               |             |              |               |               |               |               |       |         |           |           |           |             |             |
|     JsonReaderBenchmark |      .NET 6.0 |         100 | MultiPolygon |   2,296.71 us |    118.628 us |    344.162 us |   2,209.99 us |  1.13 |    0.29 |   62.5000 |   19.5313 |         - |   264.79 KB |        0.65 |
| SystemTextJsonBenchmark |      .NET 6.0 |         100 | MultiPolygon |   1,749.16 us |    107.134 us |    305.659 us |   1,664.00 us |  0.88 |    0.26 |   82.0313 |   27.3438 |         - |   407.76 KB |        1.00 |
|     JsonReaderBenchmark |      .NET 7.0 |         100 | MultiPolygon |   2,339.49 us |    121.793 us |    353.343 us |   2,277.38 us |  1.16 |    0.30 |   62.5000 |   19.5313 |         - |   264.79 KB |        0.65 |
| SystemTextJsonBenchmark |      .NET 7.0 |         100 | MultiPolygon |   1,588.36 us |     70.990 us |    202.538 us |   1,579.53 us |  0.80 |    0.21 |   85.9375 |   35.1563 |         - |   407.79 KB |        1.00 |
|     JsonReaderBenchmark | NativeAOT 7.0 |         100 | MultiPolygon |   2,559.98 us |    180.068 us |    525.266 us |   2,515.30 us |  1.27 |    0.42 |   64.4531 |   21.4844 |         - |   264.79 KB |        0.65 |
| SystemTextJsonBenchmark | NativeAOT 7.0 |         100 | MultiPolygon |   2,154.43 us |    174.058 us |    513.215 us |   2,077.98 us |  1.00 |    0.00 |   87.8906 |   33.2031 |         - |   407.79 KB |        1.00 |
|                         |               |             |              |               |               |               |               |       |         |           |           |           |             |             |
|     JsonReaderBenchmark |      .NET 6.0 |        1000 |          All |  13,797.56 us |  1,010.837 us |  2,948.659 us |  13,126.70 us |  1.82 |    0.40 |  156.2500 |   62.5000 |         - |     1081 KB |        0.63 |
| SystemTextJsonBenchmark |      .NET 6.0 |        1000 |          All |   8,510.29 us |    476.227 us |  1,358.704 us |   8,289.47 us |  1.12 |    0.23 |  265.6250 |  125.0000 |         - |   1704.2 KB |        1.00 |
|     JsonReaderBenchmark |      .NET 7.0 |        1000 |          All |  11,832.44 us |    764.299 us |  2,217.369 us |  11,243.20 us |  1.57 |    0.37 |  171.8750 |  156.2500 |         - |     1081 KB |        0.63 |
| SystemTextJsonBenchmark |      .NET 7.0 |        1000 |          All |   7,787.50 us |    372.270 us |  1,080.023 us |   7,676.07 us |  1.03 |    0.19 |  265.6250 |  250.0000 |         - |  1704.21 KB |        1.00 |
|     JsonReaderBenchmark | NativeAOT 7.0 |        1000 |          All |  12,580.42 us |    840.195 us |  2,437.559 us |  12,080.19 us |  1.68 |    0.48 |  171.8750 |  156.2500 |         - |     1081 KB |        0.63 |
| SystemTextJsonBenchmark | NativeAOT 7.0 |        1000 |          All |   7,734.19 us |    484.599 us |  1,413.597 us |   7,465.44 us |  1.00 |    0.00 |  265.6250 |  250.0000 |         - |  1704.23 KB |        1.00 |
|                         |               |             |              |               |               |               |               |       |         |           |           |           |             |             |
|     JsonReaderBenchmark |      .NET 6.0 |        1000 | MultiPolygon |  28,557.35 us |  1,731.647 us |  5,051.297 us |  27,777.35 us |  1.51 |    0.37 |  406.2500 |  187.5000 |         - |  2641.37 KB |        0.65 |
| SystemTextJsonBenchmark |      .NET 6.0 |        1000 | MultiPolygon |  21,377.49 us |  1,370.007 us |  3,841.647 us |  20,630.76 us |  1.13 |    0.24 |  656.2500 |  312.5000 |         - |  4056.82 KB |        1.00 |
|     JsonReaderBenchmark |      .NET 7.0 |        1000 | MultiPolygon |  26,085.62 us |  1,267.862 us |  3,637.734 us |  25,941.45 us |  1.38 |    0.28 |  406.2500 |  375.0000 |         - |  2641.37 KB |        0.65 |
| SystemTextJsonBenchmark |      .NET 7.0 |        1000 | MultiPolygon |  20,511.24 us |  1,108.834 us |  3,199.238 us |  20,036.13 us |  1.08 |    0.22 |  656.2500 |  625.0000 |         - |  4056.86 KB |        1.00 |
|     JsonReaderBenchmark | NativeAOT 7.0 |        1000 | MultiPolygon |  28,741.63 us |  2,139.559 us |  6,274.956 us |  27,943.37 us |  1.52 |    0.40 |  375.0000 |  312.5000 |         - |  2641.38 KB |        0.65 |
| SystemTextJsonBenchmark | NativeAOT 7.0 |        1000 | MultiPolygon |  19,308.50 us |    973.496 us |  2,824.287 us |  18,594.40 us |  1.00 |    0.00 |  656.2500 |  625.0000 |         - |  4056.85 KB |        1.00 |
|                         |               |             |              |               |               |               |               |       |         |           |           |           |             |             |
|     JsonReaderBenchmark |      .NET 6.0 |       10000 |          All | 140,908.96 us |  6,766.913 us | 19,739.404 us | 136,317.40 us |  1.33 |    0.24 | 2000.0000 |  750.0000 |  250.0000 | 10904.08 KB |        0.63 |
| SystemTextJsonBenchmark |      .NET 6.0 |       10000 |          All | 131,826.77 us |  8,006.933 us | 23,608.621 us | 127,007.23 us |  1.24 |    0.30 | 3000.0000 | 1500.0000 |  500.0000 | 17418.09 KB |        1.00 |
|     JsonReaderBenchmark |      .NET 7.0 |       10000 |          All | 157,915.71 us | 14,169.957 us | 41,780.437 us | 146,523.96 us |  1.50 |    0.47 | 2000.0000 |  750.0000 |  250.0000 |  10902.9 KB |        0.63 |
| SystemTextJsonBenchmark |      .NET 7.0 |       10000 |          All | 116,689.56 us |  6,214.653 us | 18,029.835 us | 118,253.00 us |  1.10 |    0.21 | 3200.0000 | 1800.0000 |  600.0000 | 17419.33 KB |        1.00 |
|     JsonReaderBenchmark | NativeAOT 7.0 |       10000 |          All | 154,409.44 us | 12,657.378 us | 37,320.565 us | 145,850.53 us |  1.45 |    0.39 | 1666.6667 |  666.6667 |         - | 10902.25 KB |        0.63 |
| SystemTextJsonBenchmark | NativeAOT 7.0 |       10000 |          All | 107,816.94 us |  5,196.811 us | 15,159.340 us | 105,062.48 us |  1.00 |    0.00 | 3000.0000 | 1750.0000 |  500.0000 | 17418.92 KB |        1.00 |
|                         |               |             |              |               |               |               |               |       |         |           |           |           |             |             |
|     JsonReaderBenchmark |      .NET 6.0 |       10000 | MultiPolygon | 293,468.74 us | 14,792.433 us | 41,963.671 us | 292,364.05 us |  1.36 |    0.26 | 4000.0000 | 1000.0000 |         - | 26506.64 KB |        0.65 |
| SystemTextJsonBenchmark |      .NET 6.0 |       10000 | MultiPolygon | 259,996.72 us | 17,096.448 us | 49,599.895 us | 253,629.60 us |  1.20 |    0.26 | 6500.0000 | 2500.0000 |  500.0000 | 40948.99 KB |        1.00 |
|     JsonReaderBenchmark |      .NET 7.0 |       10000 | MultiPolygon | 269,429.68 us | 13,348.577 us | 39,149.066 us | 260,418.40 us |  1.24 |    0.21 | 4000.0000 | 1500.0000 |         - | 26506.64 KB |        0.65 |
| SystemTextJsonBenchmark |      .NET 7.0 |       10000 | MultiPolygon | 240,378.36 us | 10,937.142 us | 31,730.633 us | 236,796.45 us |  1.11 |    0.20 | 7500.0000 | 3500.0000 | 1000.0000 | 40951.48 KB |        1.00 |
|     JsonReaderBenchmark | NativeAOT 7.0 |       10000 | MultiPolygon | 281,075.48 us | 17,166.275 us | 50,345.712 us | 270,001.27 us |  1.30 |    0.28 | 4333.3333 | 1666.6667 |  333.3333 | 26508.55 KB |        0.65 |
| SystemTextJsonBenchmark | NativeAOT 7.0 |       10000 | MultiPolygon | 219,534.95 us |  9,294.446 us | 26,667.507 us | 216,748.60 us |  1.00 |    0.00 | 7000.0000 | 3333.3333 |  666.6667 | 40949.07 KB |        1.00 |


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
