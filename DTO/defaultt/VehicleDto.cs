namespace CCAPI.DTO.defaultt
{
    public class VehicleDto
    {
        public int ID { get; set; }
        public int TransportationCompanyId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Capacity { get; set; } = string.Empty;
        public int? DriverId { get; set; }
        public string VehicleNum { get; set; } = string.Empty;
    
    }
}
