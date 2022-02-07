// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CA1050 // Declare types in namespaces

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Formatting = Newtonsoft.Json.Formatting;
using JToken = Newtonsoft.Json.Linq.JToken;

[assembly: CLSCompliant(false)]

[MemoryDiagnoser]
public class JsonBenchmarks
{
    byte[]? jsonDataBytes;

    public static readonly string WeatherMeasurementDataJson = Strictify(@"{
        date: '2022-01-31',
        id: 'Zuerich Uetliberg',
        location: { latitude: 47.3494991, longitude: 8.4832738 },
        measurements: [
            { temperature: 6.35, precipitation: 0.23 },
            { temperature: 6.35, precipitation: 0.23 },
            { temperature: 6.35, precipitation: 0.23 },
            { temperature: 6.35, precipitation: 0.23 },
            { temperature: 6.35, precipitation: 0.23 },
            { temperature: 6.35, precipitation: 0.23 },
            { temperature: 6.35, precipitation: 0.23 },
            { temperature: 6.35, precipitation: 0.23 },
            { temperature: 6.35, precipitation: 0.23 },
            { temperature: 6.35, precipitation: 0.23 },
            { temperature: 6.35, precipitation: 0.23 },
            { temperature: 6.35, precipitation: 0.23 }
        ]
    }");

    [GlobalSetup]
    public void Setup()
    {
        this.jsonDataBytes = Encoding.UTF8.GetBytes(WeatherMeasurementDataJson);
    }

    [Benchmark]
    public void JsonReaderBenchmark()
    {
        var reader = new Utf8JsonReader(this.jsonDataBytes);
        _ = reader.Read();
        _ = StationReportReader.Read(ref reader);
    }

    [Benchmark(Baseline = true)]
    public void SystemTextBenchmark()
    {
        var data = JsonSerializer.Deserialize<WeatherMeasurementData>(this.jsonDataBytes);
        _ = Transform(data!);
    }

    [Benchmark]
    public void SourceGeneratedBenchmark()
    {
        var data = JsonSerializer.Deserialize(this.jsonDataBytes, SourceGenerationContext.Default.WeatherMeasurementData);
        _ = Transform(data!);
    }

    static StationReport Transform(WeatherMeasurementData data)
    {
        var measurements =
            from md in data.Measurements!
            from m in new[]
            {
                new Measurement(MeasurementKind.Temperature, md.TemperatureCelsius),
                new Measurement(MeasurementKind.Precipitation, md.PrecipitationMm)
            }
            select m;

        return new StationReport(data.Date,
                                 new Station(data.StationId!, (data.Location!.Longitude, data.Location.Latitude)),
                                 measurements.ToList());
    }

    static string Strictify(string json) =>
        JToken.Parse(json).ToString(Formatting.None);

    static readonly IJsonReader<(double, double), JsonReadResult<(double, double)>> LocationReader =
        JsonReader.Object(
            JsonReader.Property("latitude", JsonReader.Double()),
            JsonReader.Property("longitude", JsonReader.Double()),
            ValueTuple.Create);

    static readonly IJsonReader<(Measurement, Measurement), JsonReadResult<(Measurement, Measurement)>> MeasurementReader =
        JsonReader.Object(
            JsonReader.Property("temperature", JsonReader.Double()),
            JsonReader.Property("precipitation", JsonReader.Double()),
            (temperature, precipitation) => (new Measurement(MeasurementKind.Temperature, temperature), new Measurement(MeasurementKind.Precipitation, precipitation)));

    static readonly IJsonReader<StationReport, JsonReadResult<StationReport>> StationReportReader =
        JsonReader.Object(
            JsonReader.Property("date", JsonReader.DateTime()),
            JsonReader.Property("id", JsonReader.String()),
            JsonReader.Property("location", LocationReader),
            JsonReader.Property("measurements", JsonReader.Array(MeasurementReader)),
            (date, id, location, measurements) => new StationReport(date, new Station(id, location), measurements.SelectMany(m => new[] { m.Item1, m.Item2 }).ToList()));

    static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(JsonBenchmarks).Assembly).Run(args);
}

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(WeatherMeasurementData))]
partial class SourceGenerationContext : JsonSerializerContext
{ }

class WeatherMeasurementData
{
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("id")]
    public string? StationId { get; set; }

    [JsonPropertyName("location")]
    public LocationData? Location { get; set; }

    [JsonPropertyName("measurements")]
    public IList<MeasurementData>? Measurements { get; set; }
}

class LocationData
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}

class MeasurementData
{
    [JsonPropertyName("temperature")]
    public double TemperatureCelsius { get; set; }

    [JsonPropertyName("precipitation")]
    public double PrecipitationMm { get; set; }
}

class StationReport
{
    public DateTime Date { get; }
    internal Station Station { get; }
    internal List<Measurement> Measurements { get; }

    public StationReport(DateTime date, Station station, List<Measurement> measurements)
    {
        Date = date;
        Station = station;
        Measurements = measurements;
    }
}

class Station
{
    internal string Id { get; }
    internal (double, double) Location { get; }

    public Station(string id, (double, double) location)
    {
        Id = id;
        Location = location;
    }
}

record struct Measurement(MeasurementKind Kind, double Value);

enum MeasurementKind
{
    Temperature, Precipitation
}
