using System.Linq;
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

    // GET: api/uavroutes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FlightRoute>>> GetUavRoutes()
    {
        var uav_routes = await _context.FlightRoutes.ToListAsync();

        var allRoutes = uav_routes.ToList();
        return allRoutes;
    }
}
