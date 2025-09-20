namespace CCAPI.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;      // "Admin", "Moderator", "User"
        public string? Description { get; set; }              // "Полный доступ", "Может регистрировать пользователей" и т.д.
    }
}
