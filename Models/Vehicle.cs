using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CCAPI.Models
{
    public class Vehicle
    {
        [Key]
        public int ID { get; set; }
        public int TransportationCompanyId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Capacity { get; set; } = string.Empty;
        public int DriverId { get; set; }
        public string VehicleNum { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }


        [ForeignKey("TransportationCompanyId")]
        public TransportationCompany Company { get; set; } = null!;

        [ForeignKey("DriverId")]
        public Driver Driver { get; set; } = null!;
    }
}