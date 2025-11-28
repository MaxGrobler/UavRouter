using System.ComponentModel.DataAnnotations;
namespace UavRouter.Data
{
    public class Risk
    {
        [Key]
        public int Id { get; set; }

        public string? Type { get; set; }
        public string? Example { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public double RiskFactor { get; set; }
    }

    public class AvoidObjectDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double RiskFactor { get; set; }
    }

}