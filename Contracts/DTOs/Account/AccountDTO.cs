using System;

namespace DTOs.User
{
    public class AccountDTO
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }  
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
} 