using CCAPI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCAPI.DTO.defaultt
{
    public class OrderDto
    {
        public int ID { get; set; }
        public int TransId { get; set; }
        public int? IDClient { get; set; }
        public DateTime? Date { get; set; }
        public string? Status { get; set; } 
        public decimal? Price { get; set; }

    }
}
