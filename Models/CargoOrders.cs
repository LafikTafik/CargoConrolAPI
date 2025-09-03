using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CCAPI.Models
{
    public class CargoOrders
    {
        public int CargoID { get; set; }
        public int OrderID { get; set; }

        public Cargos Cargo { get; set; } = null!;
        public Orders Order { get; set; } = null!;
    }
}
