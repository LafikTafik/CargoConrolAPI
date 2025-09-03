using System.ComponentModel.DataAnnotations;

namespace CCAPI.Models
{
    public class TransportationCompany
    {
        [Key]
        public int ID { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public ICollection<TransComp> TransComp { get; set; } = new List<TransComp>();
    }
}
