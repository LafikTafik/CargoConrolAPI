using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CCAPI.Models
{
    [Table("Order")]
    public class Orders
    {
        [Key]
        public int ID { get; set; }
        public int TransId { get; set; } 
        public int? IDClient { get; set; }
        public DateTime? Date { get; set; }
        public string? Status { get; set; }
        public decimal? Price { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }


        public Transportation Transportation { get; set; } = null!;
        [ForeignKey("IDClient")]
        public Client Client { get; set; } = null!;
        public ICollection<CargoOrders> Cargos { get; set; } = new List<CargoOrders>();
    }
}