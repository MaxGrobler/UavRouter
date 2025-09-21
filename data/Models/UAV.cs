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
        public double Weight { get; set; }
        public double FuelCapacity { get; set; }
        public double Range { get; set; }
        public double TopSpeed { get; set; }
        public double CruiseSpeed { get; set; }
        public double MaxAltitude { get; set; }
        public double PayLoadCapacity { get; set; }
    }

    public class RotorUAV : UAV
    {
        public int NumOfRotors { get; set; }
        public Size Size { get; set; }
    }

    public class FixedWingUAV : UAV
    {
        public double WingSpan { get; set; }
    }

    public enum Size
    {
        Small,
        Medium,
        Large,
        Massive
    }
}
