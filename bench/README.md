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

    BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.963)
    Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
    .NET SDK=7.0.101
      [Host]     : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
      Job-GBHHXP : .NET 6.0.12 (6.0.1222.56807), X64 RyuJIT AVX2
      Job-JZANEJ : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
      Job-JTAKIM : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2


|                  Method |       Runtime | ObjectCount |           Mean |        Error |        StdDev |         Median | Ratio | RatioSD |       Gen0 |      Gen1 |      Gen2 |   Allocated | Alloc Ratio |
|------------------------ |-------------- |------------ |---------------:|-------------:|--------------:|---------------:|------:|--------:|-----------:|----------:|----------:|------------:|------------:|
|     JsonReaderBenchmark |      .NET 6.0 |          10 |       829.7 us |     32.92 us |      97.06 us |       814.4 us |  1.79 |    0.29 |    20.5078 |    1.9531 |         - |    84.61 KB |        0.86 |
| SystemTextJsonBenchmark |      .NET 6.0 |          10 |       495.0 us |     19.81 us |      58.11 us |       488.3 us |  1.07 |    0.18 |    23.9258 |    7.8125 |         - |     98.9 KB |        1.01 |
|     JsonReaderBenchmark |      .NET 7.0 |          10 |       763.3 us |     26.06 us |      75.18 us |       750.6 us |  1.64 |    0.24 |    20.5078 |    1.9531 |         - |    84.61 KB |        0.86 |
| SystemTextJsonBenchmark |      .NET 7.0 |          10 |       437.4 us |     14.41 us |      42.02 us |       425.0 us |  0.94 |    0.13 |    23.4375 |    7.8125 |         - |    98.07 KB |        1.00 |
|     JsonReaderBenchmark | NativeAOT 7.0 |          10 |       730.1 us |     20.06 us |      56.92 us |       731.0 us |  1.57 |    0.18 |    20.5078 |    1.9531 |         - |    84.61 KB |        0.86 |
| SystemTextJsonBenchmark | NativeAOT 7.0 |          10 |       469.7 us |     17.84 us |      50.32 us |       455.1 us |  1.00 |    0.00 |    23.9258 |    7.8125 |         - |    98.07 KB |        1.00 |
|                         |               |             |                |              |               |                |       |         |            |           |           |             |             |
|     JsonReaderBenchmark |      .NET 6.0 |         100 |     8,242.5 us |    230.19 us |     664.16 us |     8,186.2 us |  1.75 |    0.18 |   132.8125 |   62.5000 |         - |    843.7 KB |        0.86 |
| SystemTextJsonBenchmark |      .NET 6.0 |         100 |     5,236.2 us |    204.64 us |     590.42 us |     5,115.8 us |  1.11 |    0.15 |   156.2500 |   78.1250 |         - |   994.96 KB |        1.01 |
|     JsonReaderBenchmark |      .NET 7.0 |         100 |     7,875.5 us |    260.36 us |     755.36 us |     7,716.2 us |  1.67 |    0.21 |   132.8125 |  125.0000 |         - |    843.7 KB |        0.86 |
| SystemTextJsonBenchmark |      .NET 7.0 |         100 |     4,802.9 us |    146.16 us |     426.35 us |     4,760.6 us |  1.02 |    0.11 |   156.2500 |  148.4375 |         - |    986.4 KB |        1.00 |
|     JsonReaderBenchmark | NativeAOT 7.0 |         100 |     7,769.7 us |    254.58 us |     734.51 us |     7,622.5 us |  1.65 |    0.21 |   132.8125 |  125.0000 |         - |    843.7 KB |        0.86 |
| SystemTextJsonBenchmark | NativeAOT 7.0 |         100 |     4,752.8 us |    126.98 us |     368.39 us |     4,679.4 us |  1.00 |    0.00 |   156.2500 |  148.4375 |         - |    986.4 KB |        1.00 |
|                         |               |             |                |              |               |                |       |         |            |           |           |             |             |
|     JsonReaderBenchmark |      .NET 6.0 |        1000 |    91,705.6 us |  2,731.55 us |   7,924.72 us |    90,147.4 us |  1.18 |    0.26 |  1500.0000 |  666.6667 |  166.6667 |  8431.46 KB |        0.86 |
| SystemTextJsonBenchmark |      .NET 6.0 |        1000 |    66,529.9 us |  2,066.72 us |   6,028.71 us |    65,676.7 us |  0.85 |    0.18 |  1777.7778 |  777.7778 |  222.2222 |  9875.75 KB |        1.01 |
|     JsonReaderBenchmark |      .NET 7.0 |        1000 |    94,818.1 us |  3,732.08 us |  10,827.44 us |    92,959.7 us |  1.23 |    0.33 |  1400.0000 |  800.0000 |  200.0000 |  8431.39 KB |        0.86 |
| SystemTextJsonBenchmark |      .NET 7.0 |        1000 |    71,909.2 us |  2,588.82 us |   7,510.64 us |    70,775.3 us |  0.93 |    0.21 |  1857.1429 | 1000.0000 |  285.7143 |  9789.17 KB |        1.00 |
|     JsonReaderBenchmark | NativeAOT 7.0 |        1000 |    91,905.9 us |  3,260.01 us |   9,509.61 us |    89,695.9 us |  1.18 |    0.29 |  1500.0000 |  833.3333 |  166.6667 |  8430.87 KB |        0.86 |
| SystemTextJsonBenchmark | NativeAOT 7.0 |        1000 |    82,566.5 us |  6,914.74 us |  20,388.27 us |    72,473.2 us |  1.00 |    0.00 |  1750.0000 | 1000.0000 |  250.0000 |  9789.55 KB |        1.00 |
|                         |               |             |                |              |               |                |       |         |            |           |           |             |             |
|     JsonReaderBenchmark |      .NET 6.0 |       10000 | 1,092,464.8 us | 42,949.12 us | 125,284.58 us | 1,067,990.2 us |  1.16 |    0.31 | 13000.0000 | 5000.0000 |         - | 84397.45 KB |        0.87 |
| SystemTextJsonBenchmark |      .NET 6.0 |       10000 |   634,041.7 us | 30,142.52 us |  87,927.16 us |   629,761.8 us |  0.68 |    0.23 | 15000.0000 | 5000.0000 |         - | 98321.43 KB |        1.01 |
|     JsonReaderBenchmark |      .NET 7.0 |       10000 |   775,164.9 us | 31,426.50 us |  90,168.53 us |   750,062.1 us |  0.82 |    0.23 | 14000.0000 | 7000.0000 | 1000.0000 | 84397.85 KB |        0.87 |
| SystemTextJsonBenchmark |      .NET 7.0 |       10000 |   533,456.7 us | 18,085.20 us |  52,468.43 us |   521,280.2 us |  0.57 |    0.16 | 16000.0000 | 8000.0000 | 1000.0000 |  97466.7 KB |        1.00 |
|     JsonReaderBenchmark | NativeAOT 7.0 |       10000 | 1,066,155.4 us | 48,754.39 us | 142,218.84 us | 1,038,770.2 us |  1.13 |    0.33 | 14000.0000 | 7000.0000 | 1000.0000 |  84399.3 KB |        0.87 |
| SystemTextJsonBenchmark | NativeAOT 7.0 |       10000 | 1,005,954.9 us | 83,038.53 us | 244,840.98 us | 1,008,079.6 us |  1.00 |    0.00 | 16000.0000 | 8000.0000 | 1000.0000 | 97462.72 KB |        1.00 |

  [GitHub REST API]: https://docs.github.com/en/rest/branches/branches#merge-a-branch
