using System;
using System.ComponentModel.DataAnnotations;

namespace UavRouter.Data
{
    public abstract class UAV
    {
        [Key]
        public int UavId { get; set; }
        public required string Make { get; set; }
        public required string Model { get; set; }
        public required string Name { get; set; }
        public double WeightKg { get; set; }
        public double FuelCapacityKg { get; set; }
        public double RangeKm { get; set; }
        public double TopSpeedKph { get; set; }
        public double CruiseSpeedKph { get; set; }
        public double MaxAltitudeMeters { get; set; }
        public double PayLoadCapacityKg { get; set; }
    }

    public class RotorUAV : UAV
    {
        public int NumOfRotors { get; set; }
        public Size Size { get; set; }
    }

    public class FixedWingUAV : UAV
    {
        public double? WingSpanMeters { get; set; }
    }

    public enum Size
    {
        Small,
        Medium,
        Large,
        Massive
    }
}
