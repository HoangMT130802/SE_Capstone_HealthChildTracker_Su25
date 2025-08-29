using System;

namespace Contracts.DTOs.DeviceToken
{
    public class DeviceTokenResponseDto
    {
        public int DeviceTokenId { get; set; }
        public string Token { get; set; }
        public string DeviceType { get; set; }
        public string DeviceInfo { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
    }
}
