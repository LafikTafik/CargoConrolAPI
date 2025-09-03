using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CCAPI.Models
{
    [Table("Drivers")]
    public class Driver
    {
        [Key]
        public int ID { get; set; }
        [Column ("Name")]
        public string FirstName { get; set; } = string.Empty;
        [Column("Surname")]
        public string LastName { get; set; } = string.Empty;
        [Column("LicenceNum")]
        public string LicenseNumber { get; set; } = string.Empty;
        [Column("Phone")]
        public string PhoneNumber { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    }
}