using System;

namespace Services.Interfaces
{
    public interface IVietQRService
    {
        /// <summary>
        /// Tạo VietQR string theo chuẩn EMVCo
        /// </summary>
        string CreateVietQRString(string bankBin, string accountNumber, string accountName, decimal amount, string description);
        
        /// <summary>
        /// Tạo QR code image URL từ VietQR string
        /// </summary>
        string CreateQRCodeBase64(string vietQRString);
        
        /// <summary>
        /// Tạo VietQR cho mobile banking app (trả về cả string và image URL)
        /// </summary>
        (string QrString, string QrDataURL) CreateVietQR(string bankBin, string accountNumber, string accountName, decimal amount, string description);
    }
}