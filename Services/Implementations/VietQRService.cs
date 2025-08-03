using Services.Interfaces;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Services.Implementations
{
    public class VietQRService : IVietQRService
    {
        private readonly ILogger<VietQRService> _logger;

        public VietQRService(ILogger<VietQRService> logger)
        {
            _logger = logger;
        }

        public string CreateVietQRString(string bankBin, string accountNumber, string accountName, decimal amount, string description)
        {
            try
            {
                // VietQR format theo chuẩn EMVCo
                var sb = new StringBuilder();
                
                // Payload Format Indicator
                sb.Append("000201");
                
                // Point of Initiation Method (11 = static, 12 = dynamic)
                sb.Append("010212");
                
                // Merchant Account Information
                var merchantInfo = BuildMerchantAccountInfo(bankBin, accountNumber);
                sb.Append($"38{merchantInfo.Length:D2}{merchantInfo}");
                
                // Merchant Category Code
                sb.Append("52040000");
                
                // Transaction Currency (704 = VND)
                sb.Append("5303704");
                
                // Transaction Amount
                if (amount > 0)
                {
                    var amountStr = amount.ToString("0");
                    sb.Append($"54{amountStr.Length:D2}{amountStr}");
                }
                
                // Country Code
                sb.Append("5802VN");
                
                // Merchant Name
                var merchantName = NormalizeString(accountName);
                sb.Append($"59{merchantName.Length:D2}{merchantName}");
                
                // Merchant City (default)
                sb.Append("6010Ho Chi Minh");
                
                // Additional Data Field Template
                if (!string.IsNullOrEmpty(description))
                {
                    var additionalData = BuildAdditionalData(description);
                    sb.Append($"62{additionalData.Length:D2}{additionalData}");
                }
                
                // CRC16 (4 bytes) - tính sau
                var payload = sb.ToString() + "6304";
                var crc = CalculateCRC16CCITT(payload);
                sb.Append($"6304{crc:X4}");
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo VietQR string");
                throw;
            }
        }

        public string CreateQRCodeBase64(string vietQRString)
        {
            try
            {
                // Trả về URL để tạo QR online cho đơn giản
                var encodedString = Uri.EscapeDataString(vietQRString);
                return $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={encodedString}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo QR code từ VietQR string");
                throw;
            }
        }

        public (string QrString, string QrDataURL) CreateVietQR(string bankBin, string accountNumber, string accountName, decimal amount, string description)
        {
            var qrString = CreateVietQRString(bankBin, accountNumber, accountName, amount, description);
            var qrDataURL = CreateQRCodeBase64(qrString);
            
            return (qrString, qrDataURL);
        }

        #region Private Helper Methods

        private string BuildMerchantAccountInfo(string bankBin, string accountNumber)
        {
            // QRIBFTTA format
            var serviceCode = "0308QRIBFTTA"; // ✅ Fixed: Inter-Bank Fund Transfer to Account
            var payeeBankInfo = $"01{bankBin.Length:D2}{bankBin}02{accountNumber.Length:D2}{accountNumber}{serviceCode}";
            
            return $"0010A000000727{payeeBankInfo}";
        }

        private string BuildAdditionalData(string description)
        {
            var normalizedDesc = NormalizeString(description);
            if (normalizedDesc.Length > 25)
            {
                normalizedDesc = normalizedDesc.Substring(0, 25);
            }
            
            return $"08{normalizedDesc.Length:D2}{normalizedDesc}";
        }

        private string NormalizeString(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            
            // Remove diacritics and convert to uppercase
            var normalizedString = input.ToUpperInvariant();
            
            // Replace Vietnamese characters
            var vietnameseMap = new Dictionary<char, char>
            {
                {'À', 'A'}, {'Á', 'A'}, {'Ả', 'A'}, {'Ã', 'A'}, {'Ạ', 'A'},
                {'Ă', 'A'}, {'Ằ', 'A'}, {'Ắ', 'A'}, {'Ẳ', 'A'}, {'Ẵ', 'A'}, {'Ặ', 'A'},
                {'Â', 'A'}, {'Ầ', 'A'}, {'Ấ', 'A'}, {'Ẩ', 'A'}, {'Ẫ', 'A'}, {'Ậ', 'A'},
                {'È', 'E'}, {'É', 'E'}, {'Ẻ', 'E'}, {'Ẽ', 'E'}, {'Ẹ', 'E'},
                {'Ê', 'E'}, {'Ề', 'E'}, {'Ế', 'E'}, {'Ể', 'E'}, {'Ễ', 'E'}, {'Ệ', 'E'},
                {'Ì', 'I'}, {'Í', 'I'}, {'Ỉ', 'I'}, {'Ĩ', 'I'}, {'Ị', 'I'},
                {'Ò', 'O'}, {'Ó', 'O'}, {'Ỏ', 'O'}, {'Õ', 'O'}, {'Ọ', 'O'},
                {'Ô', 'O'}, {'Ồ', 'O'}, {'Ố', 'O'}, {'Ổ', 'O'}, {'Ỗ', 'O'}, {'Ộ', 'O'},
                {'Ơ', 'O'}, {'Ờ', 'O'}, {'Ớ', 'O'}, {'Ở', 'O'}, {'Ỡ', 'O'}, {'Ợ', 'O'},
                {'Ù', 'U'}, {'Ú', 'U'}, {'Ủ', 'U'}, {'Ũ', 'U'}, {'Ụ', 'U'},
                {'Ư', 'U'}, {'Ừ', 'U'}, {'Ứ', 'U'}, {'Ử', 'U'}, {'Ữ', 'U'}, {'Ự', 'U'},
                {'Ỳ', 'Y'}, {'Ý', 'Y'}, {'Ỷ', 'Y'}, {'Ỹ', 'Y'}, {'Ỵ', 'Y'},
                {'Đ', 'D'}
            };

            var result = new StringBuilder();
            foreach (var c in normalizedString)
            {
                if (vietnameseMap.ContainsKey(c))
                {
                    result.Append(vietnameseMap[c]);
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// ✅ CRC16-CCITT chính xác theo chuẩn EMVCo
        /// Polynomial: 0x1021, Initial: 0xFFFF
        /// </summary>
        private ushort CalculateCRC16CCITT(string data)
        {
            const ushort polynomial = 0x1021;
            ushort crc = 0xFFFF; // ✅ Initial value for CRC16-CCITT
            
            foreach (char c in data)
            {
                byte b = (byte)c;
                crc ^= (ushort)(b << 8);
                
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0)
                    {
                        crc = (ushort)((crc << 1) ^ polynomial);
                    }
                    else
                    {
                        crc <<= 1;
                    }
                }
            }
            
            return crc;
        }

        #endregion
    }
}