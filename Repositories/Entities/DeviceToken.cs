// Thêm entity mới cho device tokens
using System;

namespace Repositories.Entities
{
    public partial class DeviceToken
    {
        public int DeviceTokenId { get; set; }
        
        public int AccountId { get; set; }
        
        public string Token { get; set; }
        
        public string DeviceType { get; set; } // "android", "ios", "web"
        
        public string DeviceInfo { get; set; } // JSON với thông tin device
        
        public bool IsActive { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public DateTime? LastUsedAt { get; set; }
        
        public virtual Account Account { get; set; }
    }
}
