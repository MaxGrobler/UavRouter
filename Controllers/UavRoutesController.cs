using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.Xml;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ObjectPool;
using UavRouter.Data;

[ApiController]
[Route("api/[controller]")]
public class UavRouteController : ControllerBase
{
    private readonly UAVRouterContext _context;

    public UavRouteController(UAVRouterContext context)
    {
        _context = context;
    }

    // GET endpoint
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FlightRoute>>> GetUavRoutes()
    {
        var uav_routes = await _context.FlightRoutes.ToListAsync();
        var allRoutes = uav_routes.ToList();
        return allRoutes;
    }


    [HttpPost("PlanRoute")]
    public IActionResult PlanRoute([FromBody] JsonElement myjsonstring)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };//need for case sensitivity 
        var points = JsonSerializer.Deserialize<WayPoint[]>(myjsonstring, options);

        if (points == null)
        {
            return BadRequest("empty data");
        }

        WayPoint[] newRoute = InnerPlanRoute(points);

        return Ok(newRoute);

    }

    private WayPoint[] InnerPlanRoute(WayPoint[] points)
    {

        SafteyBox box = GetSafetyBox(points);
        List<MapFeature> mapFeatures = GetThingsToAvoid(box.minimumLat, box.maximumLat, box.minimumLon, box.maximumLon);
        WayPoint[] newRoute = CheckAlongRoute(points, mapFeatures, 10, 250.0);
        return newRoute;
    }



    private SafteyBox GetSafetyBox(WayPoint[] points)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };//need for case sensitivity 

        Console.WriteLine("Received waypoints:");
        foreach (var p in points)
        {
            Console.WriteLine($"Lat: {p.Latitude}, Lng: {p.Longitude}, Alt: {p.AltitudeMeters}");
        }

        List<double> lats = new List<double>();
        List<double> longs = new List<double>();

        for (int a = 0; a < points.Length; a++)
        {
            lats.Add(points[a].Latitude);
            longs.Add(points[a].Longitude);

        }

        List<double> newLats = new List<double>();
        List<double> newLongs = new List<double>();
        double R = 6378;//radius of earth in km
        double DistanceKM = 10;

        newLats.Add(lats[0]);
        newLongs.Add(longs[0]);
        for (int b = 1; b < lats.Count; b++)
        {
            double lat1 = lats[b - 1] * (Math.PI / 180);
            double long1 = longs[b - 1] * (Math.PI / 180);
            double lat2 = lats[b] * (Math.PI / 180);
            double long2 = longs[b] * (Math.PI / 180);


            double AngleMidlat = (lat1 + lat2) / 2;
            //double AngleMidlong = (long1 + long2) / 2; - not actually used

            // makes cartesian coords - lats get closer as get bigger/smaller
            // gives a length from arc length formula
            double radlat1 = R * lat1;
            double radlong1 = R * long1 * Math.Cos(AngleMidlat);
            double radlat2 = R * lat2;
            double radlong2 = R * long2 * Math.Cos(AngleMidlat);

            double midCoordY = (radlat1 + radlat2) / 2;
            double midCoordX = (radlong1 + radlong2) / 2;

            double grad = (radlat2 - radlat1) / (radlong2 - radlong1);
            double perpgrad;

            if (double.IsInfinity(grad))
            {
                perpgrad = 0;
            }
            else if (Math.Abs(grad) < 1e-12)//basically if its close to 0 treat it as 0
            {
                perpgrad = double.PositiveInfinity;
            }
            else
            {
                perpgrad = -1 / grad;
            }

            // finds where circle of r=10 meets line
            double sqrtTerm = Math.Sqrt(1 + Math.Pow(perpgrad, 2));
            double X1 = midCoordX + DistanceKM / sqrtTerm;
            double Y1 = midCoordY + (perpgrad * (DistanceKM / sqrtTerm));
            double X2 = midCoordX - DistanceKM / sqrtTerm;
            double Y2 = midCoordY - (perpgrad * (DistanceKM / sqrtTerm));

            double latY1 = Y1 / R;
            double longX1 = X1 / (R * Math.Cos(AngleMidlat));
            double latY2 = Y2 / R;
            double longX2 = X2 / (R * Math.Cos(AngleMidlat));

            double lat1y = latY1 * (180 / Math.PI);
            double long1x = longX1 * (180 / Math.PI);
            double lat2y = latY2 * (180 / Math.PI);
            double long2x = longX2 * (180 / Math.PI);

            newLats.Add(lat1y);
            newLongs.Add(long1x);
            newLats.Add(lat2y);
            newLongs.Add(long2x);

        }
        newLats.Add(lats[^1]);
        newLongs.Add(longs[^1]);

        var list_latlons = new List<(double lat, double lon)>();

        for (int i = 0; i < lats.Count; i++)
        {
            list_latlons.Add((lats[i], longs[i]));
        }

        var minLong = list_latlons.MinBy(p => p.lon);
        var maxLong = list_latlons.MaxBy(p => p.lon);
        var minLat = list_latlons.MinBy(p => p.lat);
        var maxLat = list_latlons.MaxBy(p => p.lat);

        var safetybox = new SafteyBox();
        safetybox.maximumLat = maxLat.lat;
        safetybox.minimumLat = minLat.lat;
        safetybox.maximumLon = maxLong.lon;
        safetybox.minimumLon = minLong.lon;

        return safetybox;
    }


    private List<MapFeature> GetThingsToAvoid(double minimumLat, double maximumLat, double minimumLon, double maximumLon)//async makes the code wait for the result - but it can also tell system to carry on and not wait
    {
        string sql = @"
        SELECT ogc_fid, name, type, 
               latitude AS Latitude, longitude AS Longitude, RiskFactor
        FROM mapfeatures
        WHERE latitude BETWEEN $minLat AND $maxLat
          AND longitude BETWEEN $minLon AND $maxLon
    ";
        //gets stuff imbetween those lats and longs 
        var Avoidance = _context.MapFeatures
            .FromSqlRaw(sql,
                new SqliteParameter("$minLat", minimumLat),
                new SqliteParameter("$maxLat", maximumLat),
                new SqliteParameter("$minLon", minimumLon),
                new SqliteParameter("$maxLon", maximumLon))
            .ToList();//adds objects to avoid to list

        return Avoidance;
    }
    public bool CheckAvoidHazard(double latitude, double longitude, List<MapFeature> mapFeatures, double avoidanceDistance)
    {
        for (int i = 0; i < mapFeatures.Count; i++)
        {
            var feature = mapFeatures[i];
            double objectLat = feature.Latitude;
            double objectLong = feature.Longitude;

            if (Math.Abs(latitude - objectLat) < 0.005 && (Math.Abs(longitude - objectLong) < 0.005))//use avoidanceDistance here not 0.005 - but not same units
            {
                return true;
            }
        }

        return false;
    }

    private WayPoint[] CheckAlongRoute(WayPoint[] origionalRoute, List<MapFeature> mapFeatures, int stepDistance, double avoidanceDistance)
    {

        List<double> lats = new List<double>();
        List<double> longs = new List<double>();


        for (int a = 0; a < origionalRoute.Length; a++)
        {
            lats.Add(origionalRoute[a].Latitude);
            longs.Add(origionalRoute[a].Longitude);
        }


        List<double> newLats = new List<double>();
        List<double> newLongs = new List<double>();

        double R = 6378000;//radius of earth in m

        newLats.Add(lats[0]);
        newLongs.Add(longs[0]);
        for (int b = 1; b < lats.Count; b++)
        {
            double lat1 = lats[b - 1] * (Math.PI / 180);
            double long1 = longs[b - 1] * (Math.PI / 180);
            double lat2 = lats[b] * (Math.PI / 180);
            double long2 = longs[b] * (Math.PI / 180);

            double AngleMidlat = (lat1 + lat2) / 2;

            // makes cartesian coords - lats get closer as get bigger/smaller
            // gives a length from arc length formula
            double radlat1 = R * lat1;
            double radlong1 = R * long1 * Math.Cos(AngleMidlat);
            double radlat2 = R * lat2;
            double radlong2 = R * long2 * Math.Cos(AngleMidlat);

            double xs = radlong1 - radlong2;
            double ys = radlat1 - radlat2;

            double pointDistance = Math.Sqrt(Math.Pow(xs, 2) + Math.Pow(ys, 2));

            int RelativeStep = Convert.ToInt32(pointDistance) / stepDistance;

            for (int a = 0; a < RelativeStep; a++)
            {
                double RelativeCheck = a / RelativeStep;
                double t = a / Convert.ToDouble(RelativeStep);

                double Xcheck = radlong1 + (radlong2 - radlong1) * t;
                double Ycheck = radlat1 + (radlat2 - radlat1) * t;

                double latCheck = Ycheck / R * (180 / Math.PI);//turn catesian back into lat/long to do avoiding

                double lonCheck = Xcheck / (R * Math.Cos(AngleMidlat)) * (180 / Math.PI);


                if (CheckAvoidHazard(latCheck, lonCheck, mapFeatures, avoidanceDistance))
                {
                    // hazard detected along route — return a reroute signal (replace with actual reroute logic)
                    WayPoint[] newRoute = FindWayAround(origionalRoute, latCheck, lonCheck, stepDistance, avoidanceDistance, mapFeatures ,1);
                    return newRoute;
                }
            }
        }

        // no hazards found along route
        return origionalRoute;
    }

    private WayPoint[] FindWayAround(WayPoint[] wholeRoute, double latAvoid, double longAvoid, int stepDistance, double avoidanceDistance, List<MapFeature> mapFeatures, int attempts)
    {
        //take the route given and try adding an extra point a distance of attempts*distance (so 1*5 to start) the route where the avoidance coords are
        //if it works return the new route including that new point
        //if it fails, try going the same distance above 
        //if that fails then do attemps * distance again (attempts now at 2) and try above and below the route with that distance 
        //keep trying until attempts > 10 where it stops and displays - 'no safe aerial route'

        if (attempts > 10)
        {
            return wholeRoute;//TODO have a failure thing
        }

        var whereIAmGoing = wholeRoute[wholeRoute.Length - 1];
        var whereIAm = wholeRoute[wholeRoute.Length - 2];

        WayPoint[] points = [whereIAm, whereIAmGoing];

        List<double> lats = new List<double>();
        List<double> longs = new List<double>();


        for (int a = 0; a < points.Length; a++)
        {
            lats.Add(points[a].Latitude);
            longs.Add(points[a].Longitude);

        }
        double R = 6378;
        double DistanceKM = stepDistance * attempts;

        for (int b = 1; b < lats.Count; b++)
        {
            double lat1 = lats[b - 1] * (Math.PI / 180);//have to use radians for equation
            double long1 = longs[b - 1] * (Math.PI / 180);
            double lat2 = lats[b] * (Math.PI / 180);
            double long2 = longs[b] * (Math.PI / 180);

            double AngleMidlat = (lat1 + lat2) / 2;

            // makes cartesian coords - lats get closer as get bigger/smaller
            // gives a length from arc length formula
            double radlat1 = R * lat1;
            double radlong1 = R * long1 * Math.Cos(AngleMidlat);
            double radlat2 = R * lat2;
            double radlong2 = R * long2 * Math.Cos(AngleMidlat);

            double latAvoidInRadians = latAvoid * (Math.PI / 180);
            double longAvoidInRadians = longAvoid * (Math.PI / 180);

            double midCoordX = R * longAvoidInRadians * Math.Cos(AngleMidlat);
            double midCoordY = R * latAvoidInRadians;

            double grad = (radlat2 - radlat1) / (radlong2 - radlong1);
            double perpgrad;

            if (double.IsInfinity(grad))
            {
                perpgrad = 0;
            }
            else if (Math.Abs(grad) < 1e-12)//basically if its close to 0 treat it as 0
            {
                perpgrad = double.PositiveInfinity;
            }
            else
            {
                perpgrad = -1 / grad;
            }

            // finds where circle of r=10 meets line
            double sqrtTerm = Math.Sqrt(1 + Math.Pow(perpgrad, 2));
            double X1 = midCoordX + DistanceKM / sqrtTerm;
            double Y1 = midCoordY + (perpgrad * (DistanceKM / sqrtTerm));
            double X2 = midCoordX - DistanceKM / sqrtTerm;
            double Y2 = midCoordY - (perpgrad * (DistanceKM / sqrtTerm));

            double latY1 = Y1 / R;
            double longX1 = X1 / (R * Math.Cos(AngleMidlat));
            double latY2 = Y2 / R;
            double longX2 = X2 / (R * Math.Cos(AngleMidlat));

            double lat1y = latY1 * (180 / Math.PI);
            double long1x = longX1 * (180 / Math.PI);

            double lat2y = latY2 * (180 / Math.PI);
            double long2x = longX2 * (180 / Math.PI);

            if (!CheckAvoidHazard(lat1y, long1x, mapFeatures, avoidanceDistance))
            {
                WayPoint result = new WayPoint();
                result.Latitude = lat1y;
                result.Longitude = long1x;
                WayPoint[] newWholeRoute = new WayPoint[wholeRoute.Length + 1];
                for (int i = 0; i < wholeRoute.Length - 1; i++)
                {
                    newWholeRoute[i] = wholeRoute[i];
                }
                newWholeRoute[wholeRoute.Length - 1] = result;
                newWholeRoute[wholeRoute.Length] = wholeRoute[^1];
                return newWholeRoute;
            }
            else
            {
                if (!CheckAvoidHazard(lat2y, long2x, mapFeatures, avoidanceDistance))
                {
                    WayPoint result = new WayPoint();
                    result.Latitude = lat2y;
                    result.Longitude = long2x;
                    WayPoint[] newWholeRoute = new WayPoint[wholeRoute.Length + 1];
                    for (int i = 0; i < wholeRoute.Length - 1; i++)
                    {
                        newWholeRoute[i] = wholeRoute[i];
                    }
                    newWholeRoute[wholeRoute.Length - 1] = result;
                    newWholeRoute[wholeRoute.Length] = wholeRoute[^1];
                    return newWholeRoute;
                }
                int newAttempts = attempts + 1;
                return FindWayAround(wholeRoute, latAvoid, longAvoid, stepDistance, avoidanceDistance, mapFeatures, newAttempts );
            }
        }
        return wholeRoute;//TODO fix this
    }
}



//TODO save route function 
//TODO Calculations
 