// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace Jacob.Examples.GeoJson;

using System.Collections.Immutable;
using Jacob;

public sealed record Position(double Longitude, double Latitude, double Altitude = 0);
public abstract record Geometry;
public sealed record Point(Position Coordinates) : Geometry;
public sealed record LineString(ImmutableArray<Position> Coordinates) : Geometry;
public sealed record Polygon(ImmutableArray<ImmutableArray<Position>> Coordinates) : Geometry;
public sealed record MultiPoint(ImmutableArray<Position> Coordinates) : Geometry;
public sealed record MultiLineString(ImmutableArray<ImmutableArray<Position>> Coordinates) : Geometry;
public sealed record MultiPolygon(ImmutableArray<ImmutableArray<ImmutableArray<Position>>> Coordinates) : Geometry;
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public sealed record GeometryCollection(ImmutableArray<Geometry> Geometries) : Geometry;
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix

public static class GeoJsonReaders
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

    public static readonly IJsonReader<Position> Position =
        JsonReader.Either(from t in JsonReader.Tuple(JsonReader.Double(), JsonReader.Double())
                          select new Position(t.Item1, t.Item2),
                          from t in JsonReader.Tuple(JsonReader.Double(), JsonReader.Double(), JsonReader.Double())
                          select new Position(t.Item1, t.Item2, t.Item3));

    public static readonly IJsonReader<Point> Point =
        JsonReader.Object(JsonReader.Property("type", JsonReader.String().Validate(s => s == "Point")),
                          JsonReader.Property("coordinates", Position),
                          (_, pos) => new Point(pos));

    static readonly IJsonReader<ImmutableArray<Position>> PositionsArray =
        JsonReader.Array(Position, ImmutableArray.CreateRange);

    public static readonly IJsonReader<MultiPoint> MultiPoint =
        JsonReader.Object(JsonReader.Property("type", JsonReader.String().Validate(s => s == "MultiPoint")),
                          JsonReader.Property("coordinates", PositionsArray),
                          (_, coords) => new MultiPoint(coords));

    static readonly IJsonReader<ImmutableArray<Position>> LineStringPositions =
        PositionsArray.Validate(coords => coords.Length >= 2);

    public static readonly IJsonReader<MultiLineString> MultiLineString =
        JsonReader.Object(JsonReader.Property("type", JsonReader.String().Validate(s => s == "MultiLineString")),
                          JsonReader.Property("coordinates", JsonReader.Array(LineStringPositions, ImmutableArray.CreateRange)),
                          (_, coords) => new MultiLineString(coords));

    static readonly IJsonReader<ImmutableArray<ImmutableArray<Position>>> PolygonPositions =
            JsonReader.Array(PositionsArray.Validate(coords => coords.Length >= 4), ImmutableArray.CreateRange)
                      .Validate(rings => rings.Length >= 1);

    public static readonly IJsonReader<Polygon> Polygon =
        JsonReader.Object(JsonReader.Property("type", JsonReader.String().Validate(s => s == "Polygon")),
                          JsonReader.Property("coordinates", PolygonPositions),
                          (_, coords) => new Polygon(coords));

    public static readonly IJsonReader<LineString> LineString =
        JsonReader.Object(JsonReader.Property("type", JsonReader.String().Validate(s => s == "LineString")),
                          JsonReader.Property("coordinates", LineStringPositions),
                          (_, coords) => new LineString(coords));

    public static readonly IJsonReader<MultiPolygon> MultiPolygon =
        JsonReader.Object(JsonReader.Property("type", JsonReader.String().Validate(s => s == "MultiPolygon")),
                          JsonReader.Property("coordinates", JsonReader.Array(PolygonPositions, ImmutableArray.CreateRange)),
                          (_, coords) => new MultiPolygon(coords));

    static readonly JsonReaderRef<GeometryCollection> GeometryCollectionRef = new();

    public static readonly IJsonReader<Geometry> Geometry =
        JsonReader.Either(from g in Point select (Geometry)g,
                          from g in LineString select (Geometry)g)
                  .Or(from g in Polygon select (Geometry)g)
                  .Or(from g in MultiPoint select (Geometry)g)
                  .Or(from g in MultiLineString select (Geometry)g)
                  .Or(from g in MultiPolygon select (Geometry)g)
                  .Or(from g in GeometryCollectionRef select (Geometry)g);

    public static readonly IJsonReader<GeometryCollection> GeometryCollection = GeometryCollectionRef.Reader =
        JsonReader.Object(JsonReader.Property("type", JsonReader.String().Validate(s => s == "GeometryCollection")),
                          JsonReader.Property("geometries", JsonReader.Array(Geometry, ImmutableArray.CreateRange)),
                          (_, coords) => new GeometryCollection(coords));
}
