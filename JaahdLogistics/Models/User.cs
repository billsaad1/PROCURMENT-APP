using System;

namespace JaahdLogistics.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Position { get; set; }
        public byte[]? SignatureImage { get; set; }
    }
}
