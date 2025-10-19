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
        for(int i = 0; i < newLats.Count; i++)
        {
            WayPoint newWaypoint = new WayPoint { Latitude = newLats[i], Longitude = newLongs[i] };
            NewWayPoints[i] = newWaypoint;
            Console.WriteLine($"{newWaypoint.Latitude} {newWaypoint.Longitude}");
        }
        Console.WriteLine(NewWayPoints.Length);
        return Ok(NewWayPoints);
    
    }


}
