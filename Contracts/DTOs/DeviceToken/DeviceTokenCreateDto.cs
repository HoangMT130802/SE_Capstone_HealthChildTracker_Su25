using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.DeviceToken
{
    public class DeviceTokenCreateDto
    {
        [Required(ErrorMessage = "Device token is required")]
        public string Token { get; set; }
        
        [Required(ErrorMessage = "Device type is required")]
        public string DeviceType { get; set; } // "android", "ios", "web"
        
        public string DeviceInfo { get; set; } // JSON string với thông tin device
    }
}
