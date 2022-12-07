# Airports

A JSON collection of 28k+ entries with basic information about nearly every
airport and landing strip in the world. Each entry contains IATA code, airport
name, city, two-letter ISO country code, elevation above sea level in feet,
coordinates in decimal degrees and time zone:

```json
{
    "icao": "KOSH",
    "iata": "OSH",
    "name": "Wittman Regional Airport",
    "city": "Oshkosh",
    "state": "Wisconsin",
    "country": "US",
    "elevation": 808,
    "lat": 43.9844017029,
    "lon": -88.5569992065,
    "tz": "America\/Chicago"
}
```

Time zones initially sourced from [TimeZoneDB](https://timezonedb.com) and
updated using [TimeAPI](https://www.timeapi.io/).

## Source

Commit [`6dbaa83`][src-commit], repo [mwgg/Airports][src-repo].

  [src-repo]: https://github.com/mwgg/Airports
  [src-commit]: https://github.com/mwgg/Airports/tree/6dbaa83ab5efd193aa6d70f4f5bb1eb93ee821f8

## License

The MIT License (MIT)

Copyright &copy; 2014 mwgg

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
