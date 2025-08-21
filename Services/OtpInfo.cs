using System;

namespace Services
{
    public class OtpInfo
    {
        public string Email { get; set; }
        public string OtpCode { get; set; }
        public string Type { get; set; } // "Registration", "ForgotPassword"
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public int? AccountId { get; set; }
        
        // Thông tin đăng ký tạm thời (chỉ cho Registration)
        public string AccountName { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
    }
}
