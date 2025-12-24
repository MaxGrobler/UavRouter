using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
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

    // GET endpoint, allows the front to get stuff from my DB.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FlightRoute>>> GetUavRoutes()
    {
        var uav_routes = await _context.FlightRoutes.ToListAsync();
        var allRoutes = uav_routes.ToList();
        return allRoutes;
    }

    // POST endpoint, this allows the frontend to send data and do somethig with it. This one triggers the hard bit.
    [HttpPost("PlanRoute")]
    public IActionResult PlanRoute([FromBody] JsonElement myjsonstring)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };//need for case sensitivity, otherwise frontend call fails.
        var points = JsonSerializer.Deserialize<WayPoint[]>(myjsonstring, options);

        if (points == null)
        {
            return BadRequest("No points sent.");
        }

        WayPoint[] newRoute = InnerPlanRoute(points); //put this all in its own method so that i can re use it if I need to.

        return Ok(newRoute); //this is what is sent back to the front end inside an OK result.

    }

    private WayPoint[] InnerPlanRoute(WayPoint[] points)
    {
        //create a big circle around the middle of the route A to B.
        SafteyCircle circleShape = GetSafetyCircle(points);
        //Find anything in my DB in that area (circle above) that has a risk I need to avoid
        List<MapFeature> mapFeatures = GetThingsToAvoid(circleShape.centerLat, circleShape.centerLon, circleShape.radiusMeters);
        //Check the route from A to B and add new points to avoid stuff if anything is in the way.
        //So A to B can become A to A2 to A3 to B.
        WayPoint[] newRoute = CheckAlongRoute(points, mapFeatures, 250.0, 250.0);
        return newRoute;
    }



    private SafteyCircle GetSafetyCircle(WayPoint[] points)
    {

        List<double> lats = new List<double>();
        List<double> longs = new List<double>();
        for (int a = 0; a < points.Length; a++)
        {
            lats.Add(points[a].Latitude);
            longs.Add(points[a].Longitude);

        }
        //Find the middle point alomng my route.
        double centreLat = (lats[1] + lats[0]) / 2;
        double centreLong = (longs[1] + longs[0]) / 2;

        //Find the distance between the middle and the end to give us how big the cricle should be.
        double circleRadius = CalculateHaversineDistance(centreLat, centreLong, lats[1], longs[1]);

        //create my circle object to use later.
        var circleShape = new SafteyCircle();
        circleShape.centerLat = centreLat;
        circleShape.centerLon = centreLong;
        circleShape.radiusMeters = circleRadius;
        return circleShape;
    }

    private static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        //Looked up the formula for this, there are a few versions but this one I can understand and made sense using my
        //ALevel maths.

        var EarthsRadius = 6371008.8; // meters
        //I turn these all into radians as a kind of measure of distance as im using lat longs and its quite tricky.
        var differenceLat = ConvertToRadians(lat2 - lat1);
        var differenceLong = ConvertToRadians(lon2 - lon1);
        var lat1Radians = ConvertToRadians(lat1);
        var lat2Radiabns = ConvertToRadians(lat2);

        //Now work out the distance and turn it into meteres so I can use it in my DB sql search later.
        var haversineDistance = Math.Sin(differenceLat / 2) * Math.Sin(differenceLat / 2) + Math.Sin(differenceLong / 2) * Math.Sin(differenceLong / 2) * Math.Cos(lat1Radians) * Math.Cos(lat2Radiabns);
        var distance = 2 * Math.Asin(Math.Sqrt(haversineDistance));
        return EarthsRadius * distance;
    }

    //this was just so I didnt have to keep writing the same code on each line.
    private static double ConvertToRadians(double angle)
    {
        return Math.PI * angle / 180.0;
    }

    //This bit takes my new circle shape info and goes to the DB and asks for everything that is within the radius distance from the middle.
    private List<MapFeature> GetThingsToAvoid(double centerLat, double centerLon, double radiusMeters)//async makes the code wait for the result - but it can also tell system to carry on and not wait
    {
        //we create the sql string with these bits $lon0 etc that get filled in after
        string sql = @"
            SELECT ogc_fid,
                name,
                type,
                latitude  AS Latitude,
                longitude AS Longitude,
                RiskFactor
            FROM mapfeatures
            WHERE (
            6371008.8 * 2.0 * ASIN(
                SQRT(
                POWER(SIN((latitude  - $lat0) * (PI() / 180.0) / 2.0), 2.0) +
                COS($lat0 * (PI() / 180.0)) *
                COS(latitude * (PI() / 180.0)) *
                POWER(SIN((longitude - $lon0) * (PI() / 180.0) / 2.0), 2.0)
                )
            )
            ) <= $radiusMeters;
            ";

        //gets stuff within the circle, fills in the $lon0 parts and sends to DB. _context is the part that tells the code 
        //what DB to look at and is connected to it. This is part of the Entity Framework stuff.
        var avoidance = _context.MapFeatures
        .FromSqlRaw(sql,
            new SqliteParameter("$lat0", centerLat),
            new SqliteParameter("$lon0", centerLon),
            new SqliteParameter("$radiusMeters", radiusMeters))
        .ToList();

        //I do this just to show in theterminal what it finds as it was not working and this helped ne find out why.
        foreach (MapFeature ting in avoidance)
        {
            Console.WriteLine($"things to avoid: {ting}");
        }

        return avoidance;
    }


    //front end calls this and passes in the route and then we send back the stuff we find in the cirle we create so that
    //we can see it on the map. Otherwise no one knows why we are adding points.
    [HttpPost("GetDanger")]
    public IActionResult PutDataOnMap([FromBody] JsonElement myjsonstring)
    {
        //turns my route from the string i send in into WayPoints
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };//need for case sensitivity 
        var points = JsonSerializer.Deserialize<WayPoint[]>(myjsonstring, options);

        if (points == null)
        {
            return BadRequest("No points sent");
        }

        //get the stuff to return thats inside the circle i build in here.
        List<MapFeature> danger = GetDangerPoints(points);
        return Ok(danger);

    }

    private List<MapFeature> GetDangerPoints(WayPoint[] points)
    {
        //build my circle from the middle of the route 
        SafteyCircle circleShape = GetSafetyCircle(points);
        //get the stuff inside that circle from the DB.
        List<MapFeature> mapFeatures = GetThingsToAvoid(circleShape.centerLat, circleShape.centerLon, circleShape.radiusMeters);
        return mapFeatures;
    }


    //loop through the stuff I find and if its too close then I know I have something to avoid. If not just caryy on.
    public bool CheckAvoidHazard(double latitude, double longitude, List<MapFeature> mapFeatures, double avoidanceDistance)
    {
        for (int i = 0; i < mapFeatures.Count; i++)
        {
            var feature = mapFeatures[i];

            double meters = CalculateHaversineDistance(
                latitude, longitude,
                feature.Latitude, feature.Longitude
            );

            if (meters <= avoidanceDistance)
                return true;
        }
        //nothing found, carry on.
        return false;
    }


    private WayPoint[] CheckAlongRoute(
     WayPoint[] origionalRoute,
     List<MapFeature> mapFeatures,
     double stepDistance,
     double avoidanceDistance
 )
    {
        //I use this bit so that I can add stuff to the route if i need to.
        var currentRoute = origionalRoute.ToList();

        //this bit stops us getting stuck in a forever loop which takes ages and can sometimes crash so i set it to 10 but you could 
        //make this bigger and see hwo big it could go. for me this works now.
        for (int attempt = 1; attempt <= 1000; attempt++)
        {
            //I havent changed anything in the route yet.
            bool changedThisAttempt = false;

            double R = 6378000; // Earth radius (meters)

            //looping through the route which starts as A to B
            for (int routeCount = 1; routeCount < currentRoute.Count; routeCount++)
            {
                // change to radians
                //A part of this section of route
                double lat1 = currentRoute[routeCount - 1].Latitude * (Math.PI / 180);
                double long1 = currentRoute[routeCount - 1].Longitude * (Math.PI / 180);
                //B part of this section of route
                double lat2 = currentRoute[routeCount].Latitude * (Math.PI / 180);
                double long2 = currentRoute[routeCount].Longitude * (Math.PI / 180);

                // middle of section
                double AngleMidlat = (lat1 + lat2) / 2;

                // into meters
                double radlat1 = R * lat1;
                double radlong1 = R * long1 * Math.Cos(AngleMidlat);
                double radlat2 = R * lat2;
                double radlong2 = R * long2 * Math.Cos(AngleMidlat);
                double xs = radlong1 - radlong2;
                double ys = radlat1 - radlat2;

                // distance of this segment in meters
                double pointDistance = Math.Sqrt(xs * xs + ys * ys);

                // stepDistance controls how many attempts along the line we try.
                double RelativeStep = Convert.ToInt32(pointDistance) / stepDistance;
                if (RelativeStep < 1) RelativeStep = 1; //1 is less than 1.

                // Sample points along the segment from start to end
                for (int step = 0; step <= RelativeStep; step++)
                {
                    double t = step / Convert.ToDouble(RelativeStep);
                    double Xcheck = radlong1 + (radlong2 - radlong1) * t;
                    double Ycheck = radlat1 + (radlat2 - radlat1) * t;

                    // Convert back to lat long
                    double latCheck = Ycheck / R * (180 / Math.PI);
                    double lonCheck = Xcheck / (R * Math.Cos(AngleMidlat)) * (180 / Math.PI);

                    // If a  point is within avoidanceDistance of a risk thing we need to find a way around it
                    if (CheckAvoidHazard(latCheck, lonCheck, mapFeatures, avoidanceDistance))
                    {
                        Console.WriteLine(
                            $"Found something to avoid, attempt={attempt} segment start={routeCount - 1} segment end={routeCount}"
                        );

                        //grab the part of the list before the thing to avoid so we can add to it
                        var beforeRoute = currentRoute.Take(routeCount + 1).ToArray();

                        //if we find a way around it, the route will have an extra point added.
                        var afterRouting = FindWayAround(
                            beforeRoute,
                            latCheck,
                            lonCheck,
                            stepDistance,
                            avoidanceDistance,
                            mapFeatures,
                            1
                        );

                        // stick the rest of the route we havent checked yet back on.
                        //this bit grabs from the bad part forwards
                        var remainder = currentRoute.Skip(routeCount + 1);
                        //This sticks that onto the new route I just created.
                        currentRoute = afterRouting.Concat(remainder).ToList();

                        //route has changed 
                        changedThisAttempt = true;
                        break;
                    }
                }

                if (changedThisAttempt)
                    break;
            }

            // If we found nothing to avoid then this part if ok.
            if (!changedThisAttempt)
                return currentRoute.ToArray();
        }

        // If we try more than 10 times we just give back what we go to and stop.
        // return the best-effort route.
        Console.WriteLine(
            "10 attempts done, couldnt finish but have sent back what i can"
        );
        //I turned this into a List earlier so i could do all the grabbing and adding but I need to make it back into Array now.
        return currentRoute.ToArray();
    }

    private WayPoint[] FindWayAround(
     WayPoint[] wholeRoute,
     double thingToAvoidLatitude,
     double thingToAvoidLongitude,
     double stepDistanceMeters,
     double avoidanceDistanceMeters,
     List<MapFeature> mapFeatures,
     int attemptIndex
 )
    {
        // attempts controls how far from the middle of the risk we try to jumo to.
        // attempt=250m
        // attempt=500m
        // up to 10
        if (attemptIndex > 1000)
            return wholeRoute;

        // We only ever try to the route the last section
        // so if we have created A to A2 to B we would only try A2 to B so that we know we are heading to B
        var segmentStart = wholeRoute[wholeRoute.Length - 2];
        var segmentEnd = wholeRoute[wholeRoute.Length - 1];

        double EarthRadiusMeters = 6378000.0;

        // Convert endpoints to radians
        double startLatitudeRad = segmentStart.Latitude * (Math.PI / 180.0);
        double startLongitudeRad = segmentStart.Longitude * (Math.PI / 180.0);
        double endLatitudeRad = segmentEnd.Latitude * (Math.PI / 180.0);
        double endLongitudeRad = segmentEnd.Longitude * (Math.PI / 180.0);

        double midLatitudeRad = (startLatitudeRad + endLatitudeRad) / 2.0;
        double cosMidLatitude = Math.Cos(midLatitudeRad);

        // meters again
        double startXMeters = EarthRadiusMeters * startLongitudeRad * cosMidLatitude;
        double startYMeters = EarthRadiusMeters * startLatitudeRad;
        double endXMeters = EarthRadiusMeters * endLongitudeRad * cosMidLatitude;
        double endYMeters = EarthRadiusMeters * endLatitudeRad;

        double thingToAvoidXMeters =
            EarthRadiusMeters * (thingToAvoidLongitude * Math.PI / 180.0) * cosMidLatitude;

        double thingToAvoidYMeters =
            EarthRadiusMeters * (thingToAvoidLatitude * Math.PI / 180.0);

        double segmentDeltaX = endXMeters - startXMeters;
        double segmentDeltaY = endYMeters - startYMeters;

        double segmentLengthMeters =
            Math.Sqrt(segmentDeltaX * segmentDeltaX + segmentDeltaY * segmentDeltaY);

        //if we get really really really close then we dont want to try anything. its basically the same point.
        if (segmentLengthMeters < 0.000001)
            return wholeRoute;

        // This is the tangent line we want to step along
        double perpendicularUnitX = -segmentDeltaY / segmentLengthMeters;
        double perpendicularUnitY = segmentDeltaX / segmentLengthMeters;

        //working out how far to try jumpimng
        double moveBy25Meters = avoidanceDistanceMeters + 25.0;
        double addFiftyEveryTime = 50.0;

        for (int k = 0; k < 200; k++)
        {
            double addMultiply = moveBy25Meters + addFiftyEveryTime * k;

            // First try a point on one side of the segment
            var newPointA = MakeWaypoint(
                thingToAvoidXMeters + perpendicularUnitX * addMultiply,
                thingToAvoidYMeters + perpendicularUnitY * addMultiply,
                EarthRadiusMeters,
                cosMidLatitude
            );

            if (newPointA != null &&
                IsNewLegSafe(
                    segmentStart,
                    newPointA,
                    segmentEnd,
                    mapFeatures,
                    stepDistanceMeters,
                    avoidanceDistanceMeters
                ))
            {
                return AddTheNewWayPoint(wholeRoute, newPointA);
            }

            // Then try the opposite side
            var newPointB = MakeWaypoint(
                thingToAvoidXMeters - perpendicularUnitX * addMultiply,
                thingToAvoidYMeters - perpendicularUnitY * addMultiply,
                EarthRadiusMeters,
                cosMidLatitude
            );

            if (newPointB != null &&
                IsNewLegSafe(
                    segmentStart,
                    newPointB,
                    segmentEnd,
                    mapFeatures,
                    stepDistanceMeters,
                    avoidanceDistanceMeters
                ))
            {
                return AddTheNewWayPoint(wholeRoute, newPointB);
            }
        }
        // No way found
        return wholeRoute;
    }

    private WayPoint? MakeWaypoint(double xMeters, double yMeters, double R, double cosMid)
    {
        // Convert meters back to radians then degrees using the same projection basis
        double latRad = yMeters / R;
        double lonRad = xMeters / (R * cosMid);

        double latDeg = latRad * (180.0 / Math.PI);
        double lonDeg = lonRad * (180.0 / Math.PI);

        if (double.IsNaN(latDeg) || double.IsNaN(lonDeg) || double.IsInfinity(latDeg) || double.IsInfinity(lonDeg))
            return null;

        return new WayPoint { Latitude = latDeg, Longitude = lonDeg };
    }

    private bool IsNewLegSafe(WayPoint start, WayPoint mid, WayPoint end, List<MapFeature> mapFeatures, double stepDistance, double avoidanceDistance)
    {
        if (CheckAvoidHazard(mid.Latitude, mid.Longitude, mapFeatures, avoidanceDistance))
            return false;

        bool firstLegSafe = SegmentIsSafe(start, mid, mapFeatures, stepDistance, avoidanceDistance);
        if (!firstLegSafe) return false;

        bool secondLegSafe = SegmentIsSafe(mid, end, mapFeatures, stepDistance, avoidanceDistance);

        bool result = secondLegSafe;
        return result;

    }

    //new route part doesnt his anything to avoid?
    private bool SegmentIsSafe(
        WayPoint p1,
        WayPoint p2,
        List<MapFeature> mapFeatures,
        double stepDistance,
        double avoidanceDistance
    )
    {
        double R = 6378000.0;

        double lat1 = p1.Latitude * (Math.PI / 180.0);
        double lon1 = p1.Longitude * (Math.PI / 180.0);
        double lat2 = p2.Latitude * (Math.PI / 180.0);
        double lon2 = p2.Longitude * (Math.PI / 180.0);

        double angleMidLat = (lat1 + lat2) / 2.0;
        double cosMid = Math.Cos(angleMidLat);

        double x1 = R * lon1 * cosMid;
        double y1 = R * lat1;

        double x2 = R * lon2 * cosMid;
        double y2 = R * lat2;

        double dx = x2 - x1;
        double dy = y2 - y1;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        int steps = (int)Math.Ceiling(dist / stepDistance);
        if (steps < 1) steps = 1;

        for (int i = 0; i <= steps; i++)
        {
            double t = i / (double)steps;

            double x = x1 + dx * t;
            double y = y1 + dy * t;

            double latDeg = (y / R) * (180.0 / Math.PI);
            double lonDeg = (x / (R * cosMid)) * (180.0 / Math.PI);

            if (CheckAvoidHazard(latDeg, lonDeg, mapFeatures, avoidanceDistance))
                return false;
        }

        return true;
    }

    private WayPoint[] AddTheNewWayPoint(WayPoint[] wholeRoute, WayPoint detour)
    {
        // Insert it just before the last point (which is whereIAmGoing)
        var newWholeRoute = new WayPoint[wholeRoute.Length + 1];
        for (int i = 0; i < wholeRoute.Length - 1; i++)
            newWholeRoute[i] = wholeRoute[i];

        newWholeRoute[wholeRoute.Length - 1] = detour;
        newWholeRoute[wholeRoute.Length] = wholeRoute[^1];
        return newWholeRoute;
    }
}

//TODO save route function 
