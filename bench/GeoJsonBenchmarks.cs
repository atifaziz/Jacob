// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CA1050

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Jacob.Examples.GeoJson;
using JsonElement = System.Text.Json.JsonElement;
using JsonReader = Jacob.JsonReader;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;
using static MoreLinq.Extensions.RepeatExtension;

[MemoryDiagnoser]
public class GeoJsonBenchmarks
{
    const string PointJsonSnippet = @"
    {
        type: 'Point',
        coordinates: [100.0, 0.0]
    }";

    const string LineStringJsonSnippet = @"
    {
        type: 'LineString',
        coordinates: [
            [100.0, 0.0],
            [101.0, 1.0]
        ]
    }";

    const string PolygonJsonSnippet = @"
    {
        type: 'Polygon',
        coordinates: [
            [
                [100.0, 0.0],
                [101.0, 0.0],
                [101.0, 1.0],
                [100.0, 1.0],
                [100.0, 0.0]
            ],
            [
                [100.8, 0.8],
                [100.8, 0.2],
                [100.2, 0.2],
                [100.2, 0.8],
                [100.8, 0.8]
            ]
        ]
    }";

    const string MultiPointJsonSnippet = @"
    {
        type: 'MultiPoint',
        coordinates: [
            [100.0, 0.0],
            [101.0, 1.0]
        ]
    }";

    const string MultiLineStringJsonSnippet = @"
    {
        type: 'MultiLineString',
        coordinates: [
            [
                [100.0, 0.0],
                [101.0, 1.0]
            ],
            [
                [102.0, 2.0],
                [103.0, 3.0]
            ]
        ]
    }";

    const string MultiPolygonJsonSnippet = @"
    {
        type: 'MultiPolygon',
        coordinates: [
            [
                [
                    [102.0, 2.0],
                    [103.0, 2.0],
                    [103.0, 3.0],
                    [102.0, 3.0],
                    [102.0, 2.0]
                ]
            ],
            [
                [
                    [100.0, 0.0],
                    [101.0, 0.0],
                    [101.0, 1.0],
                    [100.0, 1.0],
                    [100.0, 0.0]
                ],
                [
                    [100.2, 0.2],
                    [100.2, 0.8],
                    [100.8, 0.8],
                    [100.8, 0.2],
                    [100.2, 0.2]
                ]
            ]
        ]
    }";

    const string GeometryCollectionJsonSnippet = @"
    {
        type: 'GeometryCollection',
        geometries: [{
            type: 'Point',
            coordinates: [100.0, 0.0]
        }, {
            type: 'LineString',
            coordinates: [
                [101.0, 0.0],
                [102.0, 1.0]
            ]
        }]
    }";

    private static readonly string[] JsonSnippet = new[]
    {
        PointJsonSnippet, LineStringJsonSnippet, PolygonJsonSnippet,
        MultiPointJsonSnippet, MultiLineStringJsonSnippet,
        MultiPolygonJsonSnippet, GeometryCollectionJsonSnippet
    };

    private byte[] _jsonDataBytes = Array.Empty<byte>();

    [Params(10, 100, 1000, 10000)] public int NumberOfElements { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var jsonBuilder = new StringBuilder("[");
        _ = jsonBuilder.Append(string.Join(',', JsonSnippet.Repeat().Take(NumberOfElements)));
        _ = jsonBuilder.Append(']');

        this._jsonDataBytes = Encoding.UTF8.GetBytes(Strictify(jsonBuilder.ToString()));
    }

    [Benchmark]
    public Geometry[] JsonReaderBenchmark()
    {
        return JsonReader.Array(GeoJsonReaders.Geometry).Read(this._jsonDataBytes);
    }

    [Benchmark(Baseline = true)]
    public Geometry[] SystemTextJsonBenchmark()
    {
        return SystemTextGeoJsonReader.Read(this._jsonDataBytes);
    }

    private static string Strictify(string json) =>
        Newtonsoft.Json.Linq.JToken.Parse(json).ToString(Newtonsoft.Json.Formatting.None);
}

static class SystemTextGeoJsonReader
{
    public static Geometry[] Read(byte[] json) =>
        JsonSerializer.Deserialize<GeometryJson[]>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!.Select(ConvertToGeometry).ToArray();

    private static Position ConvertToPosition(JsonElement e) =>
        e.GetArrayLength() is var len and (2 or 3)
            ? new Position(e[0].GetDouble(), e[1].GetDouble(), len is 2 ? 0 : e[2].GetDouble())
            : throw new ArgumentException(null, nameof(e));

    private static ImmutableArray<Position> ConvertToPositionsArray(JsonElement e) =>
        ImmutableArray.CreateRange(from jsonElement in e.EnumerateArray()
                                   select ConvertToPosition(jsonElement));

    private static ImmutableArray<Position> ConvertToLineStringPositions(JsonElement e) =>
        ConvertToPositionsArray(e) is { Length: >= 2 } result
            ? result
            : throw new ArgumentException(null, nameof(e));

    private static ImmutableArray<ImmutableArray<Position>>
        ConvertToPolygonPositions(JsonElement e) =>
        ImmutableArray.CreateRange(from jsonElement in e.EnumerateArray()
                                   select ConvertToPositionsArray(jsonElement) is
                                   { Length: >= 4 } result
                                       ? result
                                       : throw new ArgumentException(null, nameof(e))) is
        { Length: >= 1 } result
            ? result
            : throw new ArgumentException(null, nameof(e));

    private static Geometry ConvertToGeometry(GeometryJson g) =>
        g.Type switch
        {
            "GeometryCollection" => new GeometryCollection(g.Geometries.Select(ConvertToGeometry)
                                                            .ToImmutableArray()),
            "Point" => new Point(ConvertToPosition(g.Coordinates)),
            "LineString" => new LineString(ConvertToLineStringPositions(g.Coordinates)),
            "MultiPoint" => new MultiPoint(ConvertToPositionsArray(g.Coordinates)),
            "MultiLineString" => new MultiLineString(ImmutableArray.CreateRange(
                from el in g.Coordinates.EnumerateArray()
                select ConvertToLineStringPositions(el))),
            "Polygon" => new Polygon(ConvertToPolygonPositions(g.Coordinates)),
            "MultiPolygon" => new MultiPolygon(g.Coordinates.EnumerateArray()
                                                .Select(ConvertToPolygonPositions)
                                                .ToImmutableArray()),
            _ => throw new InvalidOperationException($"Type {g.Type} is not supported.")
        };

#pragma warning disable CA1812
    private record struct GeometryJson(string Type, GeometryJson[] Geometries,
                                       JsonElement Coordinates);
#pragma warning restore CA1812
}
