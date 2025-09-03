using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CCAPI.Models
{
    public class TransComp
    {
        public int TransportationID { get; set; }
        public int CompanyID { get; set; }

        [ForeignKey("TransportationID")]
        public Transportation Transportation { get; set; } = null!;

        [ForeignKey("CompanyID")]
        public TransportationCompany Company { get; set; } = null!;
    }
}
