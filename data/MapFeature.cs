using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UavRouter.Data
{
    [Table("mapfeatures")]
    public class MapFeature
    {
        [Key]
        [Column("ogc_fid")]
        public int Id { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("type")]
        public string? Type { get; set; }

        [Column("latitude")]
        public double Latitude { get; set; }

        [Column("longitude")]
        public double Longitude { get; set; }

        [Column("RiskFactor")]
        public double RiskFactor { get; set; }
    }
}
