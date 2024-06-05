# gtfs-to-osm

Converts a gtfs schedule feed into .osm files.

## Usage

Requires [.NET 7.0 to run.](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)

Download a gtfs file online. Then, extract its contents into `/gtfs`.

Run gtfs-reader. If you are on Windows, double-click the executable.

If you are on Linux, open a terminal where the app is and run `./gtfs-reader`.

Afterward, all variants of the route will be given as .osm files in `/out`.

My recommendation is to open it in JOSM as a separate layer to compare existing routes with GTFS.

## Building

Install [.NET SDK 7.0.](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)

Clone the repo, and inside the repo run `dotnet build --configuration Release gtfs-reader.sln`

Navigate to `bin/Release/net7.0`, and create two folders `gtfs` and `out`.