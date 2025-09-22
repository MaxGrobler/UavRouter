using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UavRouter.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FixedWingUAVs",
                columns: table => new
                {
                    UavId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WingSpanMeters = table.Column<double>(type: "REAL", nullable: true),
                    Make = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    WeightKg = table.Column<double>(type: "REAL", nullable: false),
                    FuelCapacityKg = table.Column<double>(type: "REAL", nullable: false),
                    RangeKm = table.Column<double>(type: "REAL", nullable: false),
                    TopSpeedKph = table.Column<double>(type: "REAL", nullable: false),
                    CruiseSpeedKph = table.Column<double>(type: "REAL", nullable: false),
                    MaxAltitudeMeters = table.Column<double>(type: "REAL", nullable: false),
                    PayLoadCapacityKg = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedWingUAVs", x => x.UavId);
                });

            migrationBuilder.CreateTable(
                name: "FlightRoutes",
                columns: table => new
                {
                    RouteId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StartLong = table.Column<double>(type: "REAL", nullable: false),
                    StartLat = table.Column<double>(type: "REAL", nullable: false),
                    EndLong = table.Column<double>(type: "REAL", nullable: false),
                    EndLat = table.Column<double>(type: "REAL", nullable: false),
                    DistanceKm = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightRoutes", x => x.RouteId);
                });

            migrationBuilder.CreateTable(
                name: "RotorUAVs",
                columns: table => new
                {
                    UavId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NumOfRotors = table.Column<int>(type: "INTEGER", nullable: false),
                    Size = table.Column<int>(type: "INTEGER", nullable: false),
                    Make = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    WeightKg = table.Column<double>(type: "REAL", nullable: false),
                    FuelCapacityKg = table.Column<double>(type: "REAL", nullable: false),
                    RangeKm = table.Column<double>(type: "REAL", nullable: false),
                    TopSpeedKph = table.Column<double>(type: "REAL", nullable: false),
                    CruiseSpeedKph = table.Column<double>(type: "REAL", nullable: false),
                    MaxAltitudeMeters = table.Column<double>(type: "REAL", nullable: false),
                    PayLoadCapacityKg = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RotorUAVs", x => x.UavId);
                });

            migrationBuilder.CreateTable(
                name: "WayPoints",
                columns: table => new
                {
                    WaypointId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RouteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    AltitudeMeters = table.Column<double>(type: "REAL", nullable: false),
                    FlightRouteRouteId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WayPoints", x => x.WaypointId);
                    table.ForeignKey(
                        name: "FK_WayPoints_FlightRoutes_FlightRouteRouteId",
                        column: x => x.FlightRouteRouteId,
                        principalTable: "FlightRoutes",
                        principalColumn: "RouteId");
                });

            migrationBuilder.InsertData(
                table: "FixedWingUAVs",
                columns: new[] { "UavId", "CruiseSpeedKph", "FuelCapacityKg", "Make", "MaxAltitudeMeters", "Model", "Name", "PayLoadCapacityKg", "RangeKm", "TopSpeedKph", "WeightKg", "WingSpanMeters" },
                values: new object[,]
                {
                    { 4, 40.0, 2.0, "Autel", 300.0, "Evo Lite", "RiverScout", 5.0, 10000.0, 50.0, 3.0, 2.5 },
                    { 5, 35.0, 1.5, "Yuneec", 250.0, "Typhoon H", "BathDrone1", 4.0, 8000.0, 45.0, 2.5, 2.2000000000000002 }
                });

            migrationBuilder.InsertData(
                table: "FlightRoutes",
                columns: new[] { "RouteId", "DistanceKm", "EndLat", "EndLong", "StartLat", "StartLong" },
                values: new object[,]
                {
                    { 1, 2.0, 51.381500000000003, -2.3580000000000001, 51.381300000000003, -2.359 },
                    { 2, 3.5, 51.378, -2.3620000000000001, 51.3795, -2.3599999999999999 },
                    { 3, 4.0, 51.384999999999998, -2.3599999999999999, 51.384, -2.3639999999999999 },
                    { 4, 2.5, 51.377499999999998, -2.3584999999999998, 51.375999999999998, -2.355 },
                    { 5, 3.0, 51.381999999999998, -2.3584999999999998, 51.383000000000003, -2.3624999999999998 },
                    { 6, 3.2000000000000002, 51.3825, -2.359, 51.380000000000003, -2.3559999999999999 },
                    { 7, 2.7999999999999998, 51.380000000000003, -2.3540000000000001, 51.378999999999998, -2.3570000000000002 },
                    { 8, 4.0, 51.384, -2.3639999999999999, 51.381999999999998, -2.3610000000000002 },
                    { 9, 3.5, 51.383000000000003, -2.3599999999999999, 51.381, -2.363 },
                    { 10, 2.7000000000000002, 51.3795, -2.3570000000000002, 51.378, -2.359 }
                });

            migrationBuilder.InsertData(
                table: "RotorUAVs",
                columns: new[] { "UavId", "CruiseSpeedKph", "FuelCapacityKg", "Make", "MaxAltitudeMeters", "Model", "Name", "NumOfRotors", "PayLoadCapacityKg", "RangeKm", "Size", "TopSpeedKph", "WeightKg" },
                values: new object[,]
                {
                    { 1, 15.0, 0.0, "DJI", 120.0, "Phantom 4", "SkyEye", 4, 2.0, 5000.0, 1, 20.0, 1.5 },
                    { 2, 12.0, 0.0, "Parrot", 100.0, "Anafi", "BathFlyer", 4, 1.0, 4000.0, 0, 15.0, 0.80000000000000004 },
                    { 3, 12.0, 0.0, "DJI", 120.0, "Mavic Air 2", "BathDrone2", 4, 1.0, 6000.0, 0, 18.0, 1.2 }
                });

            migrationBuilder.InsertData(
                table: "WayPoints",
                columns: new[] { "WaypointId", "AltitudeMeters", "FlightRouteRouteId", "Latitude", "Longitude", "RouteId" },
                values: new object[,]
                {
                    { 1, 50.0, null, 51.380000000000003, -2.3610000000000002, 2 },
                    { 2, 55.0, null, 51.378999999999998, -2.3614999999999999, 2 },
                    { 3, 60.0, null, 51.384500000000003, -2.363, 3 },
                    { 4, 60.0, null, 51.384799999999998, -2.3614999999999999, 3 },
                    { 5, 60.0, null, 51.384900000000002, -2.3605, 3 },
                    { 6, 50.0, null, 51.381, -2.3574999999999999, 6 },
                    { 7, 55.0, null, 51.381999999999998, -2.3580000000000001, 6 },
                    { 8, 60.0, null, 51.383000000000003, -2.3620000000000001, 8 },
                    { 9, 60.0, null, 51.383499999999998, -2.363, 8 },
                    { 10, 60.0, null, 51.383800000000001, -2.3635000000000002, 8 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_WayPoints_FlightRouteRouteId",
                table: "WayPoints",
                column: "FlightRouteRouteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FixedWingUAVs");

            migrationBuilder.DropTable(
                name: "RotorUAVs");

            migrationBuilder.DropTable(
                name: "WayPoints");

            migrationBuilder.DropTable(
                name: "FlightRoutes");
        }
    }
}
