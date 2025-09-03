namespace CCAPI.DTO.defaultt
{
    public class CreateOrderDto
    {
        public bool IsNewClient { get; set; }
        public string NewClientName { get; set; } = string.Empty;
        public string NewClientSurname { get; set; } = string.Empty;
        public string NewClientPhone { get; set; } = string.Empty;
        public string NewClientEmail { get; set; } = string.Empty;
        public string NewClientAddress { get; set; } = string.Empty;

        public int ClientId { get; set; }

        public int TransportationCompanyId { get; set; }
        public int CargoId { get; set; }
        public int VehicleId { get; set; }
        public string StartPoint { get; set; } = string.Empty;
        public string EndPoint { get; set; } = string.Empty;
    }

}
