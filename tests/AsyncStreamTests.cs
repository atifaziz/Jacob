// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Jacob.Tests;

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using JsonException = System.Text.Json.JsonException;

public class AsyncStreamTests
{
    readonly ITestOutputHelper testOutputHelper;

    public AsyncStreamTests(ITestOutputHelper testOutputHelper) =>
        this.testOutputHelper = testOutputHelper;

    void WriteLine(object? value) => this.testOutputHelper.WriteLine(value?.ToString());

    readonly record struct Coordinates(double Latitude, double Longitude);

    sealed record Airport
    {
        public required string Icao { get; init; }
        public string? Iata { get; init; }
        public required string Name { get; init; }
        public required string City { get; init; }
        public string? State { get; init; }
        public required CountryCode Country { get; init; }
        public required int Elevation { get; init; }
        public required Coordinates Coordinates { get; init; }
        public required string TimeZone { get; init; }
    }

    static readonly IJsonReader<Airport> AirportReader =
        //
        // Example:
        //
        // {
        //     "icao": "00AK",
        //     "iata": "",
        //     "name": "Lowell Field",
        //     "city": "Anchor Point",
        //     "state": "Alaska",
        //     "country": "US",
        //     "elevation": 450,
        //     "lat": 59.94919968,
        //     "lon": -151.695999146,
        //     "tz": "America\/Anchorage"
        // }
        //
        JsonReader.Object(JsonReader.Property("icao", JsonReader.String()),
                          JsonReader.Property("iata", from s in JsonReader.String().OrNull()
                                                      select s is { Length: > 0 } ? s : null),
                          JsonReader.Property("name", JsonReader.String()),
                          JsonReader.Property("city", JsonReader.String()),
                          JsonReader.Property("state", from s in JsonReader.String()
                                                       select (string?)s, (true, null)),
                          JsonReader.Property("country", JsonReader.String().AsEnum<CountryCode>()),
                          JsonReader.Property("elevation", JsonReader.Int32()),
                          JsonReader.Property("lat", JsonReader.Double()),
                          JsonReader.Property("lon", JsonReader.Double()),
                          JsonReader.Property("tz", JsonReader.String()),
                          (icao, iata, name, city, state, ctry, elevation, lat, @long, tz) =>
                              new Airport
                              {
                                  Icao = icao, Iata = iata, Name = name,
                                  City = city, State = state, Country = ctry,
                                  Coordinates = new(lat, @long), Elevation = elevation,
                                  TimeZone = tz,
                              });

    [Fact]
    public async Task GetAsyncEnumerator_With_Airports_Data()
    {
        var jsonReader = AirportReader;
        Airport? lastGoodAirport = null;
        var actual = new List<Airport>();

        try
        {
            var count = 0;

            await using var stream = File.OpenRead(Path.Join("..", "..", "..", "data", "airports", "airports.json"));
            await using var enumerator = jsonReader.GetAsyncEnumerator(stream, initialBufferSize: 10);
            while (await enumerator.MoveNextAsync())
            {
                lastGoodAirport = enumerator.Current;
                if (count++ % 5_000 is 0)
                    actual.Add(lastGoodAirport);
            }
        }
        catch (JsonException) when (lastGoodAirport is not null)
        {
            WriteLine($"Last Good Airport = {lastGoodAirport}");
            throw;
        }

        var expected = new[]
        {
            new Airport
            {
                Icao = "00AK", Iata = null, Name = "Lowell Field", City = "Anchor Point",
                State = "Alaska", Country = CountryCode.US, Elevation = 450,
                Coordinates = new(59.94919968, -151.695999146), TimeZone = "America/Anchorage"
            },
            new Airport
            {
                Icao = "8TE0", Iata = null, Name = "Gillingham Airport", City = "Floresville",
                State = "Texas", Country = CountryCode.US, Elevation = 450,
                Coordinates = new(29.1390991211, -98.1132965088), TimeZone = "America/Chicago"
            },
            new Airport
            {
                Icao = "FXMF", Iata = "MFC", Name = "Mafeteng Airport", City = "Mafeteng",
                State = "Mafeteng", Country = CountryCode.LS, Elevation = 5350,
                Coordinates = new(-29.8010997772, 27.2436008453), TimeZone = "Africa/Maseru"
            },
            new Airport
            {
                Icao = "LA61", Iata = null, Name = "Kenan Airstrip", City = "Kaplan",
                State = "Louisiana", Country = CountryCode.US, Elevation = 20,
                Coordinates = new(30.00839996, -92.24120331), TimeZone = "America/Chicago"
            },
            new Airport
            {
                Icao = "SA27", Iata = null, Name = "Puerto Rosales Airport",
                City = "Puerto Rosales", State = "Buenos-Aires", Country = CountryCode.AR,
                Elevation = 16, Coordinates = new(-38.8970985413, -62.0102996826),
                TimeZone = "America/Argentina/Buenos_Aires"
            },
            new Airport
            {
                Icao = "TX98", Iata = null, Name = "Hawkins Private Airport", City = "Godley",
                State = "Texas", Country = CountryCode.US, Elevation = 975,
                Coordinates = new(32.4751014709, -97.5009002686), TimeZone = "America/Chicago"
            },
        };

        Assert.Equal(expected, actual);
    }

    enum CountryCode // ISO 3166-1
    {
        AF =   4, // Afghanistan
        AL =   8, // Albania
        DZ =  12, // Algeria
        AS =  16, // American Samoa
        AD =  20, // Andorra
        AO =  24, // Angola
        AI = 660, // Anguilla
        AQ =  10, // Antarctica
        AG =  28, // Antigua and Barbuda
        AR =  32, // Argentina
        AM =  51, // Armenia
        AW = 533, // Aruba
        AU =  36, // Australia
        AT =  40, // Austria
        AZ =  31, // Azerbaijan
        BS =  44, // Bahamas
        BH =  48, // Bahrain
        BD =  50, // Bangladesh
        BB =  52, // Barbados
        BY = 112, // Belarus
        BE =  56, // Belgium
        BZ =  84, // Belize
        BJ = 204, // Benin
        BM =  60, // Bermuda
        BT =  64, // Bhutan
        BO =  68, // Bolivia, Plurinational State of
        BQ = 535, // Bonaire, Sint Eustatius and Saba
        BA =  70, // Bosnia and Herzegovina
        BW =  72, // Botswana
        BV =  74, // Bouvet Island
        BR =  76, // Brazil
        IO =  86, // British Indian Ocean Territory
        BN =  96, // Brunei Darussalam
        BG = 100, // Bulgaria
        BF = 854, // Burkina Faso
        BI = 108, // Burundi
        CV = 132, // Cabo Verde
        KH = 116, // Cambodia
        CM = 120, // Cameroon
        CA = 124, // Canada
        KY = 136, // Cayman Islands
        CF = 140, // Central African Republic
        TD = 148, // Chad
        CL = 152, // Chile
        CN = 156, // China
        CX = 162, // Christmas Island
        CC = 166, // Cocos (Keeling) Islands
        CO = 170, // Colombia
        KM = 174, // Comoros
        CG = 178, // Congo
        CD = 180, // Congo, the Democratic Republic of the
        CK = 184, // Cook Islands
        CR = 188, // Costa Rica
        CI = 384, // Côte d'Ivoire
        HR = 191, // Croatia
        CU = 192, // Cuba
        CW = 531, // Curaçao
        CY = 196, // Cyprus
        CZ = 203, // Czechia
        DK = 208, // Denmark
        DJ = 262, // Djibouti
        DM = 212, // Dominica
        DO = 214, // Dominican Republic
        EC = 218, // Ecuador
        EG = 818, // Egypt
        SV = 222, // El Salvador
        GQ = 226, // Equatorial Guinea
        ER = 232, // Eritrea
        EE = 233, // Estonia
        SZ = 748, // Eswatini
        ET = 231, // Ethiopia
        FK = 238, // Falkland Islands (Malvinas)
        FO = 234, // Faroe Islands
        FJ = 242, // Fiji
        FI = 246, // Finland
        FR = 250, // France
        GF = 254, // French Guiana
        PF = 258, // French Polynesia
        TF = 260, // French Southern Territories
        GA = 266, // Gabon
        GM = 270, // Gambia
        GE = 268, // Georgia
        DE = 276, // Germany
        GH = 288, // Ghana
        GI = 292, // Gibraltar
        GR = 300, // Greece
        GL = 304, // Greenland
        GD = 308, // Grenada
        GP = 312, // Guadeloupe
        GU = 316, // Guam
        GT = 320, // Guatemala
        GG = 831, // Guernsey
        GN = 324, // Guinea
        GW = 624, // Guinea-Bissau
        GY = 328, // Guyana
        HT = 332, // Haiti
        HM = 334, // Heard Island and McDonald Islands
        VA = 336, // Holy See
        HN = 340, // Honduras
        HK = 344, // Hong Kong
        HU = 348, // Hungary
        IS = 352, // Iceland
        IN = 356, // India
        ID = 360, // Indonesia
        IR = 364, // Iran, Islamic Republic of
        IQ = 368, // Iraq
        IE = 372, // Ireland
        IM = 833, // Isle of Man
        IL = 376, // Israel
        IT = 380, // Italy
        JM = 388, // Jamaica
        JP = 392, // Japan
        JE = 832, // Jersey
        JO = 400, // Jordan
        KZ = 398, // Kazakhstan
        KE = 404, // Kenya
        KI = 296, // Kiribati
        KP = 408, // Korea, Democratic People's Republic of
        KR = 410, // Korea, Republic of
        KW = 414, // Kuwait
        KG = 417, // Kyrgyzstan
        LA = 418, // Lao People's Democratic Republic
        LV = 428, // Latvia
        LB = 422, // Lebanon
        LS = 426, // Lesotho
        LR = 430, // Liberia
        LY = 434, // Libya
        LI = 438, // Liechtenstein
        LT = 440, // Lithuania
        LU = 442, // Luxembourg
        MO = 446, // Macao
        MG = 450, // Madagascar
        MW = 454, // Malawi
        MY = 458, // Malaysia
        MV = 462, // Maldives
        ML = 466, // Mali
        MT = 470, // Malta
        MH = 584, // Marshall Islands
        MQ = 474, // Martinique
        MR = 478, // Mauritania
        MU = 480, // Mauritius
        YT = 175, // Mayotte
        MX = 484, // Mexico
        FM = 583, // Micronesia, Federated States of
        MD = 498, // Moldova, Republic of
        MC = 492, // Monaco
        MN = 496, // Mongolia
        ME = 499, // Montenegro
        MS = 500, // Montserrat
        MA = 504, // Morocco
        MZ = 508, // Mozambique
        MM = 104, // Myanmar
        NA = 516, // Namibia
        NR = 520, // Nauru
        NP = 524, // Nepal
        NL = 528, // Netherlands
        NC = 540, // New Caledonia
        NZ = 554, // New Zealand
        NI = 558, // Nicaragua
        NE = 562, // Niger
        NG = 566, // Nigeria
        NU = 570, // Niue
        NF = 574, // Norfolk Island
        MP = 580, // Northern Mariana Islands
        MK = 807, // North Macedonia
        NO = 578, // Norway
        OM = 512, // Oman
        PK = 586, // Pakistan
        PW = 585, // Palau
        PS = 275, // Palestine, State of
        PA = 591, // Panama
        PG = 598, // Papua New Guinea
        PY = 600, // Paraguay
        PE = 604, // Peru
        PH = 608, // Philippines
        PN = 612, // Pitcairn
        PL = 616, // Poland
        PT = 620, // Portugal
        PR = 630, // Puerto Rico
        QA = 634, // Qatar
        RE = 638, // Réunion
        RO = 642, // Romania
        RU = 643, // Russian Federation
        RW = 646, // Rwanda
        BL = 652, // Saint Barthélemy
        SH = 654, // Saint Helena, Ascension and Tristan da Cunha
        KN = 659, // Saint Kitts and Nevis
        LC = 662, // Saint Lucia
        MF = 663, // Saint Martin (French part)
        PM = 666, // Saint Pierre and Miquelon
        VC = 670, // Saint Vincent and the Grenadines
        WS = 882, // Samoa
        SM = 674, // San Marino
        ST = 678, // Sao Tome and Principe
        SA = 682, // Saudi Arabia
        SN = 686, // Senegal
        RS = 688, // Serbia
        SC = 690, // Seychelles
        SL = 694, // Sierra Leone
        SG = 702, // Singapore
        SX = 534, // Sint Maarten (Dutch part)
        SK = 703, // Slovakia
        SI = 705, // Slovenia
        SB =  90, // Solomon Islands
        SO = 706, // Somalia
        ZA = 710, // South Africa
        GS = 239, // South Georgia and the South Sandwich Islands
        SS = 728, // South Sudan
        ES = 724, // Spain
        LK = 144, // Sri Lanka
        SD = 729, // Sudan
        SR = 740, // Suriname
        SJ = 744, // Svalbard and Jan Mayen
        SE = 752, // Sweden
        CH = 756, // Switzerland
        SY = 760, // Syrian Arab Republic
        TW = 158, // Taiwan, Province of China
        TJ = 762, // Tajikistan
        TZ = 834, // Tanzania, United Republic of
        TH = 764, // Thailand
        TL = 626, // Timor-Leste
        TG = 768, // Togo
        TK = 772, // Tokelau
        TO = 776, // Tonga
        TT = 780, // Trinidad and Tobago
        TN = 788, // Tunisia
        TR = 792, // Turkey
        TM = 795, // Turkmenistan
        TC = 796, // Turks and Caicos Islands
        TV = 798, // Tuvalu
        UG = 800, // Uganda
        UA = 804, // Ukraine
        AE = 784, // United Arab Emirates
        GB = 826, // United Kingdom of Great Britain and Northern Ireland
        US = 840, // United States of America
        UM = 581, // United States Minor Outlying Islands
        UY = 858, // Uruguay
        UZ = 860, // Uzbekistan
        VU = 548, // Vanuatu
        VE = 862, // Venezuela, Bolivarian Republic of
        VN = 704, // Viet Nam
        VG =  92, // Virgin Islands, British
        VI = 850, // Virgin Islands, U.S.
        WF = 876, // Wallis and Futuna
        EH = 732, // Western Sahara
        YE = 887, // Yemen
        ZM = 894, // Zambia
        ZW = 716, // Zimbabwe
        AX = 248, // Åland Islands

        XK =  -1, // Kosovo; https://en.wikipedia.org/wiki/XK_(user_assigned_code)
        KS = XK,
    }
}
