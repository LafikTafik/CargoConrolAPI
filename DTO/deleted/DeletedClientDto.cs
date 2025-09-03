namespace CCAPI.DTO.deleted
{
    public class DeletedClientDto
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Adress { get; set; } = string.Empty;


        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
