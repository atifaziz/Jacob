// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[assembly: System.CLSCompliant(false)]

namespace Jacob.Benchmarks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[MemoryDiagnoser]
public class JsonBenchmarks
{
    byte[]? jsonDataBytes;

    public static readonly string WeatherMeasurementDataJson = Strictify(@"{
        date: '2022-01-31',
        id: 'Zuerich Uetliberg',
        location: {latitude: 47.3494991, longitude: 8.4832738},
        measurements: [{temperature: 6.35, precipitation: 0.23}]
    }");

    [GlobalSetup]
    public void Setup()
    {
        this.jsonDataBytes = Encoding.UTF8.GetBytes(WeatherMeasurementDataJson);
    }

    [Benchmark]
    public void JsonReaderBenchmark()
    {
        var reader = new Utf8JsonReader(jsonDataBytes);
        _ = reader.Read();
        _ = StationReportReader.Read(ref reader);
    }

    [Benchmark(Baseline = true)]
    public void SystemTextBenchmark()
    {
        var weatherMeasurementData = System.Text.Json.JsonSerializer.Deserialize<WeatherMeasurementData>(jsonDataBytes);
        var measurements = weatherMeasurementData!.Measurements!.Select(m => (new Measurement(MeasurementKind.Temperature, m.TemperatureCelsius), new Measurement(MeasurementKind.Precipitation, m.PrecipitationMm)));
        _ = new StationReport(weatherMeasurementData!.Date, new Station(weatherMeasurementData!.StationId!, (weatherMeasurementData!.Location!.Latitude, weatherMeasurementData!.Location.Longitude)), measurements.SelectMany(m => new[] { m.Item1, m.Item2 }).ToList());
    }

    [Benchmark]
    public void SourceGeneratedBenchmark()
    {
        var weatherMeasurementData = System.Text.Json.JsonSerializer.Deserialize(jsonDataBytes, SourceGenerationContext.Default.WeatherMeasurementData);
        var measurements = weatherMeasurementData!.Measurements!.Select(m => (new Measurement(MeasurementKind.Temperature, m.TemperatureCelsius), new Measurement(MeasurementKind.Precipitation, m.PrecipitationMm)));
        _ = new StationReport(weatherMeasurementData!.Date, new Station(weatherMeasurementData!.StationId!, (weatherMeasurementData!.Location!.Latitude, weatherMeasurementData!.Location.Longitude)), measurements.SelectMany(m => new[] { m.Item1, m.Item2 }).ToList());
    }

    private static string Strictify(string json) =>
        JToken.Parse(json).ToString(Formatting.None);

    internal static readonly IJsonReader<(double, double), JsonReadResult<(double, double)>> LocationReader =
    Jacob.JsonReader.Object(
        Jacob.JsonReader.Property("latitude", Jacob.JsonReader.Double()),
        Jacob.JsonReader.Property("longitude", Jacob.JsonReader.Double()),
        ValueTuple.Create);

    internal static readonly IJsonReader<(Measurement, Measurement), JsonReadResult<(Measurement, Measurement)>> MeasurementReader =
        Jacob.JsonReader.Object(
            Jacob.JsonReader.Property("temperature", Jacob.JsonReader.Double()),
            Jacob.JsonReader.Property("precipitation", Jacob.JsonReader.Double()),
            (temperature, precipitation) => (new Measurement(MeasurementKind.Temperature, temperature), new Measurement(MeasurementKind.Precipitation, precipitation)));

    internal static readonly IJsonReader<StationReport, JsonReadResult<StationReport>> StationReportReader =
        Jacob.JsonReader.Object(
            Jacob.JsonReader.Property("date", Jacob.JsonReader.DateTime()),
            Jacob.JsonReader.Property("id", Jacob.JsonReader.String()),
            Jacob.JsonReader.Property("location", LocationReader),
            Jacob.JsonReader.Property("measurements", Jacob.JsonReader.Array(MeasurementReader)),
            (date, id, location, measurements) => new StationReport(date, new Station(id, location), measurements.SelectMany(m => new[] { m.Item1, m.Item2 }).ToList()));

    static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(JsonBenchmarks).Assembly).Run(args);
}

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(WeatherMeasurementData))]
internal partial class SourceGenerationContext : JsonSerializerContext
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
