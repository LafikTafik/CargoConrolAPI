namespace CCAPI.DTO.deleted
{
    public class DeletedTransportationDto
    {
        public int ID { get; set; }
        public int CargoId { get; set; }
        public int VehicleId { get; set; }
        public string StartPoint { get; set; } = string.Empty;
        public string EndPoint { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
