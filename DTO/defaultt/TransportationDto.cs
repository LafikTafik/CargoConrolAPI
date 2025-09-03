namespace CCAPI.DTO.defaultt
{
    public class TransportationDto
    {
        public int ID { get; set; }
        public int CargoId { get; set; }
        public int VehicleId { get; set; }
        public string StartPoint { get; set; } = string.Empty;
        public string EndPoint { get; set; } = string.Empty;
    }
}