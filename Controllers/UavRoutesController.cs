using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
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
    /*
    [HttpPost("PlanRoute")]
    public IActionResult PlanRoute([FromBody] JsonElement myjsonstring)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var Points = JsonSerializer.Deserialize<WayPoint[]>(myjsonstring, options);

        //expect structure:"[{\"lat\":51.37949539652476,\"lng\":-2.3742485046386723},{\"lat\":51.389565827780466,\"lng\":-2.353906631469727}]"
        Console.WriteLine("Received waypoints:");
        foreach (var p in Points)
        {
            Console.WriteLine($"Lat: {p.Latitude}, Lng: {p.Longitude}, Alt: {p.AltitudeMeters}");
        }

        List<double> lats = new List<double>();
        List<double> longs = new List<double>();


        for (int a = 0; a < Points.Length; a++)
        {
            lats.Add(Points[a].Latitude);
            longs.Add(Points[a].Longitude);

        }

        List<double> newLats = new List<double>();
        List<double> newLongs = new List<double>();
        for (int b = 1; b < lats.Count; b++)
        {
            newLats.Add(lats[b - 1]);
            newLongs.Add(longs[b - 1]);

            newLats.Add(lats[b - 1] + (lats[b] - lats[b - 1]) / 2);
            newLongs.Add(longs[b - 1] + (longs[b] - longs[b - 1]) / 2);
        }
        newLats.Add(lats[^1]);
        newLongs.Add(longs[^1]);

        WayPoint[] NewWayPoints = new WayPoint[newLats.Count];
        for (int i = 0; i < newLats.Count; i++)
        {
            WayPoint newWaypoint = new WayPoint { Latitude = newLats[i], Longitude = newLongs[i] };
            NewWayPoints[i] = newWaypoint;
            Console.WriteLine($"{newWaypoint.Latitude} {newWaypoint.Longitude}");
        }
        Console.WriteLine(NewWayPoints.Length);
        return Ok(NewWayPoints);

    }*/




    [HttpPost("PlanRoute")]
    public IActionResult PlanRoute([FromBody] JsonElement myjsonstring)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };//need for case sensitivity 
        var Points = JsonSerializer.Deserialize<WayPoint[]>(myjsonstring, options);

        Console.WriteLine("Received waypoints:");
        foreach (var p in Points)
        {
            Console.WriteLine($"Lat: {p.Latitude}, Lng: {p.Longitude}, Alt: {p.AltitudeMeters}");
        }

        List<double> lats = new List<double>();
        List<double> longs = new List<double>();


        for (int a = 0; a < Points.Length; a++)
        {
            lats.Add(Points[a].Latitude);
            longs.Add(Points[a].Longitude);

        }

        List<double> newLats = new List<double>();
        List<double> newLongs = new List<double>();
        double R = 6378;
        double DistanceKM = 5;

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
            //newLats.Add(lat2y);
            //newLongs.Add(long2x);
            if (lats.Count >= b) {
                newLats.Add(lats[b]);
            newLongs.Add(longs[b]);
            }
            
        }
        newLats.Add(lats[^1]);
        newLongs.Add(longs[^1]);

        WayPoint[] NewWayPoints = new WayPoint[newLats.Count];
        for (int i = 0; i < newLats.Count; i++)
        {
            WayPoint newWaypoint = new WayPoint { Latitude = newLats[i], Longitude = newLongs[i] };
            NewWayPoints[i] = newWaypoint;
            Console.WriteLine($"{newWaypoint.Latitude} {newWaypoint.Longitude}");
        }
        Console.WriteLine(NewWayPoints.Length);
        return Ok(NewWayPoints);


    }
}


