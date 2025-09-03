using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CCAPI.Models
{
    public class Transportation
    {
        [Key]
        public int ID { get; set; }
        public int CargoID { get; set; }
        public int VehicleId { get; set; }

        public string StartPoint { get; set; } = string.Empty;
        public string EndPoint { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<TransComp> TransComp { get; set; } = new List<TransComp>();
    }
}