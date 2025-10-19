using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        return Ok(Points);

    }


}
