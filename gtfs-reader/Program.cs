
using System.Globalization;
using System.Xml;
using CsvHelper;
using gtfs_reader;

// read in routes
var route_reader = new StreamReader("gtfs/routes.txt");
var route_csv = new CsvReader(route_reader, CultureInfo.InvariantCulture);
var routes = route_csv.GetRecords<Routes>().ToArray();
Console.WriteLine("Select line.");
for (int i = 0; i < routes.Length; i++)
    Console.WriteLine($"[{i}] - {routes[i].route_short_name} - {routes[i].route_long_name}");

var input = Console.ReadLine();
Routes route = routes[int.Parse(input)];


// read in trips

var trip_reader = new StreamReader("gtfs/trips.txt");
var trip_csv = new CsvReader(trip_reader, CultureInfo.InvariantCulture);
var trips = trip_csv.GetRecords<Trips>().Where(trip => trip.route_id == route.route_id).ToArray();

List<int> trip_ids = new List<int>();
foreach (var trip in trips)
    trip_ids.Add(trip.trip_id);

// read in stop times

var stop_times_reader = new StreamReader("gtfs/stop_times.txt");
var stop_times_csv = new CsvReader(stop_times_reader, CultureInfo.InvariantCulture);
var stop_times = stop_times_csv.GetRecords<StopTimes>().Where(x => trip_ids.Contains(x.trip_id)).ToArray();

// Assemble list of unique trips
List<Trip> unique_trips = new List<Trip>();

foreach (var trip in trips)
{
    var trip_stop_times = stop_times.Where(x => x.trip_id == trip.trip_id).ToArray();
    Trip ntrip = new Trip();
    ntrip.stops = new List<string>();
    ntrip.shape_id = trip.shape_id;
    ntrip.trip_id = trip.trip_id;
    foreach (var trip_stop_time in trip_stop_times)
        ntrip.stops.Add(trip_stop_time.stop_id);
    // check if unique_trips already contains this trip
    bool identicalFound = false;
    
    foreach (var utrip in unique_trips)
    {
        if (Enumerable.SequenceEqual(utrip.stops, ntrip.stops))
        {
            identicalFound = true;
            break;
        }
    }
    // end check
    if(!identicalFound)
        unique_trips.Add(ntrip);
}

// get stops for future use

var stops_reader = new StreamReader("gtfs/stops.txt");
var stops_csv = new CsvReader(stops_reader, CultureInfo.InvariantCulture);
var stops = stops_csv.GetRecords<Stops>().ToArray();

// get shape for future use
var shapes = new CsvReader(new StreamReader("gtfs/shapes.txt"), CultureInfo.InvariantCulture).GetRecords<Shapes>().ToArray();

XmlWriterSettings settings = new XmlWriterSettings();
settings.Indent = true;
settings.IndentChars = ("   ");

int idval = -1;

// for each unique trip, write out a new osm file w info
foreach (var utrip in unique_trips)
{
    XmlWriter xmlwriter = XmlWriter.Create($"out/{route.route_short_name} - {route.route_long_name} - {utrip.trip_id}.osm", settings);

    xmlwriter.WriteStartElement("osm");
    xmlwriter.WriteAttributeString("version","0.6");

    for (int i = 0; i < utrip.stops.Count; i++)
    {
        Stops stop = stops.First(x => x.stop_id == utrip.stops[i]);
        
        xmlwriter.WriteStartElement("node");
        xmlwriter.WriteAttributeString("id",$"{idval}");
        idval--;
        xmlwriter.WriteAttributeString("action","modify");
        xmlwriter.WriteAttributeString("visible","true");
        xmlwriter.WriteAttributeString("lat",stop.stop_lat.ToString(CultureInfo.InvariantCulture));
        xmlwriter.WriteAttributeString("lon",stop.stop_lon.ToString(CultureInfo.InvariantCulture));
        
        xmlwriter.WriteStartElement("tag");
        xmlwriter.WriteAttributeString("k","highway");
        xmlwriter.WriteAttributeString("v","bus_stop");
        xmlwriter.WriteEndElement();
        
        xmlwriter.WriteStartElement("tag");
        xmlwriter.WriteAttributeString("k","public_transport");
        xmlwriter.WriteAttributeString("v","platform");
        xmlwriter.WriteEndElement();
        
        xmlwriter.WriteStartElement("tag");
        xmlwriter.WriteAttributeString("k","name");
        xmlwriter.WriteAttributeString("v",stop.stop_name);
        xmlwriter.WriteEndElement();
        
        xmlwriter.WriteEndElement();
    }

    List<int> path = new List<int>();
    Shapes[] tripShape = shapes.Where(x => x.shape_id == utrip.shape_id).OrderBy(x => x.shape_pt_sequence).ToArray();
    for (int i = 0; i < tripShape.Length; i++)
    {
        xmlwriter.WriteStartElement("node");
        xmlwriter.WriteAttributeString("id",$"{idval}");
        path.Add(idval);
        idval--;
        xmlwriter.WriteAttributeString("action","modify");
        xmlwriter.WriteAttributeString("visible","true");
        xmlwriter.WriteAttributeString("lat",tripShape[i].shape_pt_lat.ToString(CultureInfo.InvariantCulture));
        xmlwriter.WriteAttributeString("lon",tripShape[i].shape_pt_lon.ToString(CultureInfo.InvariantCulture));
        xmlwriter.WriteEndElement();
    }
    
    xmlwriter.WriteStartElement("way");
    xmlwriter.WriteAttributeString("id",$"{idval}");
    idval--;
    xmlwriter.WriteAttributeString("action","modify");
    xmlwriter.WriteAttributeString("visible","true");
    for (int i = 0; i < tripShape.Length; i++)
    {
        xmlwriter.WriteStartElement("nd");
        xmlwriter.WriteAttributeString("ref", $"{path[i]}");
        xmlwriter.WriteEndElement();
    }
    xmlwriter.WriteEndElement();
    
    xmlwriter.WriteEndElement();

    xmlwriter.Flush();
}