using System;
using System.ComponentModel.DataAnnotations;

namespace UavRouter.Data
{
    public class Route
    {
        [Key]
        public int RouteId { get; set; }
        public double StartLong { get; set; }
        public double StartLat { get; set; }
        public double EndLong { get; set; }
        public double EndLat { get; set; }
        public double Distance { get; set; }

        public ICollection<WayPoint>? Waypoints { get; set; }
    }

    public class WayPoint
    {
        [Key]
        public int WaypointId { get; set; }
        public int RouteId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }

    }
}