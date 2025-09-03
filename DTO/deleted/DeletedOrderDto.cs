using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CCAPI.DTO.deleted
{
    public class DeletedOrderDto
    {
        public int ID { get; set; }
        public int TransId { get; set; }
        public int? IDClient { get; set; }
        public DateTime? Date { get; set; }
        public string? Status { get; set; }
        public decimal? Price { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

    }
}
