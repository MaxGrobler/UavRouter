using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UavRouter.Data;

[ApiController]
[Route("api/[controller]")]
public class UAVController : ControllerBase
{
    private readonly UAVRouterContext _context;

    public UAVController(UAVRouterContext context)
    {
        _context = context;
    }

    // GET: api/uavs
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UAV>>> GetUAVs()
    {
        var fixed_wing_uavs = await _context.FixedWingUAVs.ToListAsync();
        var rotor_uavs = await _context.RotorUAVs.ToListAsync();
        var allUavs = rotor_uavs.Cast<UAV>()
                          .Concat(fixed_wing_uavs.Cast<UAV>())
                          .ToList();
        return allUavs;
    }
}
