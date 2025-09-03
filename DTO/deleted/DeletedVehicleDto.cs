namespace CCAPI.DTO.deleted
{
    public class DeletedVehicleDto
    {
        public int ID { get; set; }
        public int TransportationCompanyId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Capacity { get; set; } = string.Empty;
        public string VehicleNum { get; set; } = string.Empty;
        public int DriverId { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

}
