using Microsoft.EntityFrameworkCore;
using UavRouter.Data;

public class UAVRouterContext : DbContext
{
    public UAVRouterContext(DbContextOptions<UAVRouterContext> options) : base(options) { }
    public DbSet<RotorUAV> RotorUAVs { get; set; }
    public DbSet<FixedWingUAV> FixedWingUAVs { get; set; }
    public DbSet<FlightRoute> FlightRoutes { get; set; }
    public DbSet<WayPoint> WayPoints { get; set; }
    public DbSet<Risk> Risks { get; set; }
    public DbSet<MapFeature> MapFeatures { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed UAVs
        modelBuilder.Entity<RotorUAV>().HasData(
            new RotorUAV
            {
                UavId = 1,
                Make = "DJI",
                Model = "Phantom 4",
                Name = "SkyEye",
                WeightKg = 1.5,
                FuelCapacityKg = 0,
                RangeKm = 5000,
                TopSpeedKph = 20,
                CruiseSpeedKph = 15,
                MaxAltitudeMeters = 120,
                PayLoadCapacityKg = 2,
                NumOfRotors = 4,
                Size = Size.Medium
            },
            new RotorUAV
            {
                UavId = 2,
                Make = "Parrot",
                Model = "Anafi",
                Name = "BathFlyer",
                WeightKg = 0.8,
                FuelCapacityKg = 0,
                RangeKm = 4000,
                TopSpeedKph = 15,
                CruiseSpeedKph = 12,
                MaxAltitudeMeters = 100,
                PayLoadCapacityKg = 1,
                NumOfRotors = 4,
                Size = Size.Small
            },
            new RotorUAV
            {
                UavId = 3,
                Make = "DJI",
                Model = "Mavic Air 2",
                Name = "BathDrone2",
                WeightKg = 1.2,
                FuelCapacityKg = 0,
                RangeKm = 6000,
                TopSpeedKph = 18,
                CruiseSpeedKph = 12,
                MaxAltitudeMeters = 120,
                PayLoadCapacityKg = 1,
                NumOfRotors = 4,
                Size = Size.Small
            }
        );

        modelBuilder.Entity<FixedWingUAV>().HasData(
            new FixedWingUAV
            {
                UavId = 4,
                Make = "Autel",
                Model = "Evo Lite",
                Name = "RiverScout",
                WeightKg = 3,
                FuelCapacityKg = 2,
                RangeKm = 10000,
                TopSpeedKph = 50,
                CruiseSpeedKph = 40,
                MaxAltitudeMeters = 300,
                PayLoadCapacityKg = 5,
                WingSpanMeters = 2.5
            },
            new FixedWingUAV
            {
                UavId = 5,
                Make = "Yuneec",
                Model = "Typhoon H",
                Name = "BathDrone1",
                WeightKg = 2.5,
                FuelCapacityKg = 1.5,
                RangeKm = 8000,
                TopSpeedKph = 45,
                CruiseSpeedKph = 35.0,
                MaxAltitudeMeters = 250.0,
                PayLoadCapacityKg = 4,
                WingSpanMeters = 2.2
            }
        );

        // Seed FlightRoutes
        modelBuilder.Entity<FlightRoute>().HasData(
            new FlightRoute { RouteId = 1, StartLat = 51.3813, StartLong = -2.3590, EndLat = 51.3815, EndLong = -2.3580, DistanceKm = 2.0 },
            new FlightRoute { RouteId = 2, StartLat = 51.3795, StartLong = -2.3600, EndLat = 51.3780, EndLong = -2.3620, DistanceKm = 3.5 },
            new FlightRoute { RouteId = 3, StartLat = 51.3840, StartLong = -2.3640, EndLat = 51.3850, EndLong = -2.3600, DistanceKm = 4.0 },
            new FlightRoute { RouteId = 4, StartLat = 51.3760, StartLong = -2.3550, EndLat = 51.3775, EndLong = -2.3585, DistanceKm = 2.5 },
            new FlightRoute { RouteId = 5, StartLat = 51.3830, StartLong = -2.3625, EndLat = 51.3820, EndLong = -2.3585, DistanceKm = 3.0 },
            new FlightRoute { RouteId = 6, StartLat = 51.3800, StartLong = -2.3560, EndLat = 51.3825, EndLong = -2.3590, DistanceKm = 3.2 },
            new FlightRoute { RouteId = 7, StartLat = 51.3790, StartLong = -2.3570, EndLat = 51.3800, EndLong = -2.3540, DistanceKm = 2.8 },
            new FlightRoute { RouteId = 8, StartLat = 51.3820, StartLong = -2.3610, EndLat = 51.3840, EndLong = -2.3640, DistanceKm = 4.0 },
            new FlightRoute { RouteId = 9, StartLat = 51.3810, StartLong = -2.3630, EndLat = 51.3830, EndLong = -2.3600, DistanceKm = 3.5 },
            new FlightRoute { RouteId = 10, StartLat = 51.3780, StartLong = -2.3590, EndLat = 51.3795, EndLong = -2.3570, DistanceKm = 2.7 }
        );

        // Seed WayPoints (some routes with 2–3 extra waypoints)
        modelBuilder.Entity<WayPoint>().HasData(
            // Route 2
            new WayPoint { WaypointId = 1, RouteId = 2, Latitude = 51.3800, Longitude = -2.3610, AltitudeMeters = 50 },
            new WayPoint { WaypointId = 2, RouteId = 2, Latitude = 51.3790, Longitude = -2.3615, AltitudeMeters = 55 },

            // Route 3
            new WayPoint { WaypointId = 3, RouteId = 3, Latitude = 51.3845, Longitude = -2.3630, AltitudeMeters = 60 },
            new WayPoint { WaypointId = 4, RouteId = 3, Latitude = 51.3848, Longitude = -2.3615, AltitudeMeters = 60 },
            new WayPoint { WaypointId = 5, RouteId = 3, Latitude = 51.3849, Longitude = -2.3605, AltitudeMeters = 60 },

            // Route 6
            new WayPoint { WaypointId = 6, RouteId = 6, Latitude = 51.3810, Longitude = -2.3575, AltitudeMeters = 50 },
            new WayPoint { WaypointId = 7, RouteId = 6, Latitude = 51.3820, Longitude = -2.3580, AltitudeMeters = 55 },

            // Route 8
            new WayPoint { WaypointId = 8, RouteId = 8, Latitude = 51.3830, Longitude = -2.3620, AltitudeMeters = 60 },
            new WayPoint { WaypointId = 9, RouteId = 8, Latitude = 51.3835, Longitude = -2.3630, AltitudeMeters = 60 },
            new WayPoint { WaypointId = 10, RouteId = 8, Latitude = 51.3838, Longitude = -2.3635, AltitudeMeters = 60 }
        );
    }
}
