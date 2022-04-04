// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET6_0_OR_GREATER

#pragma warning disable CA1050

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Jacob;
using Xunit;
using JsonElement = System.Text.Json.JsonElement;
using JsonReader = Jacob.JsonReader;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

public static class GeoJsonBenchmarks
{
    const string Json = @"[
{
        type: 'Point',
        coordinates: [100.0, 0.0]
    },
    {
        type: 'LineString',
        coordinates: [
            [100.0, 0.0],
            [101.0, 1.0]
        ]
    },
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
    },
    {
        type: 'MultiPoint',
        coordinates: [
            [100.0, 0.0],
            [101.0, 1.0]
        ]
    },
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
    },
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
    },
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
    }
]";

    [Fact]
    public static void JacobTest()
    {
        var geometries = JsonReader
                        .Array(JacobGeoJsonReader.GeometryReader.Or(from g in JacobGeoJsonReader
                                .GeometryCollectionReader
                             select (Geometry)g))
                        .Read(Strictify(Json));

        Assert.NotNull(geometries);
    }

    [Fact]
    public static void SystemTextJsonTest()
    {
        var geometries = SystemTextGeoJsonReader.Read(Strictify(Json));
        Assert.NotNull(geometries);
    }

    private static string Strictify(string json) =>
        Newtonsoft.Json.Linq.JToken.Parse(json).ToString(Newtonsoft.Json.Formatting.None);
}

static class SystemTextGeoJsonReader
{
    public static Geometry[] Read(string json) =>
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
        ConvertToPositionsArray(e) is {Length: >= 2} result
            ? result
            : throw new ArgumentException(null, nameof(e));

    private static ImmutableArray<ImmutableArray<Position>>
        ConvertToPolygonPositions(JsonElement e) =>
        ImmutableArray.CreateRange(from jsonElement in e.EnumerateArray()
                                   select ConvertToPositionsArray(jsonElement) is
                                       {Length: >= 4} result
                                       ? result
                                       : throw new ArgumentException(null, nameof(e))) is
            {Length: >= 1} result
            ? result
            : throw new ArgumentException(null, nameof(e));

    private static Geometry ConvertToGeometry(GeometryJson g) =>
        g.Type switch
        {
            "GeometryCollection" => new GeometryCollection(g.Geometries.Select(ConvertToGeometry)
                                                            .ToImmutableArray()),
            "Point"      => new Point(ConvertToPosition(g.Coordinates)),
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
    private sealed record GeometryJson(string Type, GeometryJson[] Geometries,
                                       JsonElement Coordinates);
#pragma warning restore CA1812
}

static class JacobGeoJsonReader
{
    // > A position is an array of numbers.  There MUST be two or more
    // > elements. The first two elements are longitude and latitude, or
    // > easting and nothing, precisely in that order and using decimal
    // > numbers. Altitude or elevation MAY be included as an optional third
    // > element.
    // >
    // > Implementations SHOULD NOT extend positions beyond three elements
    // > because the semantics of extra elements are unspecified and
    // > ambiguous.
    //
    // Source:
    // The GeoJSON Format (RFC 7946), Section 3.1.1
    // https://datatracker.ietf.org/doc/html/rfc7946#section-3.1.1

    private static readonly IJsonReader<Position> PositionReader =
        JsonReader.Either(from t in JsonReader.Tuple(JsonReader.Double(),
                              JsonReader.Double())
                          select new Position(t.Item1,
                              t.Item2),
            from t in JsonReader.Tuple(JsonReader.Double(),
                JsonReader.Double(),
                JsonReader.Double())
            select new Position(t.Item1,
                t.Item2,
                t.Item3));

    private static readonly IJsonReader<Point> PointReader =
        JsonReader.Object(JsonReader.Property("type",
                JsonReader.String().Validate(s => s == "Point")),
            JsonReader.Property("coordinates",
                PositionReader),
            (_, pos) => new Point(pos));

    private static readonly IJsonReader<ImmutableArray<Position>> PositionsArrayReader =
        JsonReader.Array(PositionReader,
            ImmutableArray.CreateRange);

    private static readonly IJsonReader<MultiPoint> MultiPointReader =
        JsonReader.Object(JsonReader.Property("type",
                JsonReader.String().Validate(s => s == "MultiPoint")),
            JsonReader.Property("coordinates",
                PositionsArrayReader),
            (_, coords) => new MultiPoint(coords));

    private static readonly IJsonReader<ImmutableArray<Position>> LineStringPositionsReader =
        PositionsArrayReader.Validate(coords => coords.Length >= 2);

    private static readonly IJsonReader<MultiLineString> MultiLineReader =
        JsonReader.Object(JsonReader.Property("type",
                JsonReader.String()
                          .Validate(s => s == "MultiLineString")),
            JsonReader.Property("coordinates",
                JsonReader.Array(LineStringPositionsReader,
                    ImmutableArray.CreateRange)),
            (_, coords) => new MultiLineString(coords));

    private static readonly IJsonReader<ImmutableArray<ImmutableArray<Position>>>
        PolygonPositionsReader =
            JsonReader.Array(PositionsArrayReader.Validate(coords => coords.Length >= 4),
                           ImmutableArray.CreateRange)
                      .Validate(rings => rings.Length >= 1);

    private static readonly IJsonReader<Polygon> PolygonReader =
        JsonReader.Object(JsonReader.Property("type",
                JsonReader.String().Validate(s => s == "Polygon")),
            JsonReader.Property("coordinates",
                PolygonPositionsReader),
            (_, coords) => new Polygon(coords));

    private static readonly IJsonReader<LineString> LineStringReader =
        JsonReader.Object(JsonReader.Property("type",
                JsonReader.String().Validate(s => s == "LineString")),
            JsonReader.Property("coordinates",
                LineStringPositionsReader),
            (_, coords) => new LineString(coords));

    private static readonly IJsonReader<MultiPolygon> MultiPolygonReader =
        JsonReader.Object(JsonReader.Property("type",
                JsonReader.String()
                          .Validate(s => s == "MultiPolygon")),
            JsonReader.Property("coordinates",
                JsonReader.Array(PolygonPositionsReader,
                    ImmutableArray.CreateRange)),
            (_, coords) => new MultiPolygon(coords));

    public static readonly IJsonReader<Geometry> GeometryReader =
        JsonReader.Either(from g in PointReader select (Geometry)g,
                       from g in LineStringReader select (Geometry)g)
                  .Or(from g in PolygonReader select (Geometry)g)
                  .Or(from g in MultiPointReader select (Geometry)g)
                  .Or(from g in MultiLineReader select (Geometry)g)
                  .Or(from g in MultiPolygonReader select (Geometry)g);

    public static readonly IJsonReader<GeometryCollection> GeometryCollectionReader =
        JsonReader.Object(JsonReader.Property("type",
                JsonReader.String()
                          .Validate(s => s == "GeometryCollection")),
            JsonReader.Property("geometries",
                JsonReader.Array(GeometryReader,
                    ImmutableArray.CreateRange)),
            (_, coords) => new GeometryCollection(coords));
}

public sealed record Position(double Longitude, double Latitude, double Altitude = 0);

public abstract record Geometry;

public sealed record Point(Position Coordinates) : Geometry;

public sealed record LineString(ImmutableArray<Position> Coordinates) : Geometry;

public sealed record Polygon(ImmutableArray<ImmutableArray<Position>> Coordinates) : Geometry;

public sealed record MultiPoint(ImmutableArray<Position> Coordinates) : Geometry;

public sealed record MultiLineString
    (ImmutableArray<ImmutableArray<Position>> Coordinates) : Geometry;

public sealed record MultiPolygon(
    ImmutableArray<ImmutableArray<ImmutableArray<Position>>> Coordinates) : Geometry;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public sealed record GeometryCollection(ImmutableArray<Geometry> Geometries) : Geometry;
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix

#endif // NET6_0_OR_GREATER
