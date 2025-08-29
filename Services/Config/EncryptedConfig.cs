
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Services.Config
{
    public static class EncryptedConfig
    {
        private static readonly string EncryptionKey = "KidTracker2024!@"; // 16 chars for AES
        private static readonly string ConfigFilePath = "../config/encrypted-firebase.dat";

        public static string GetFirebaseCredentials()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var encryptedData = File.ReadAllBytes(ConfigFilePath);
                    return DecryptString(encryptedData);
                }
                else
                {
                    // Create encrypted file with real credentials if not exists
                    CreateDefaultEncryptedFile();
                    return GetRealCredentials();
                }
            }
            catch (Exception)
            {
                // Fallback to real credentials if decryption fails
                return GetRealCredentials();
            }
        }

        public static void EncryptAndSaveCredentials(string jsonCredentials)
        {
            var encryptedData = EncryptString(jsonCredentials);
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
            File.WriteAllBytes(ConfigFilePath, encryptedData);
        }

        private static byte[] EncryptString(string plainText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                aes.IV = new byte[16]; // Use zero IV for simplicity

                using (var encryptor = aes.CreateEncryptor())
                {
                    var plainBytes = Encoding.UTF8.GetBytes(plainText);
                    return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                }
            }
        }

        private static string DecryptString(byte[] encryptedData)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                aes.IV = new byte[16]; // Use zero IV for simplicity

                using (var decryptor = aes.CreateDecryptor())
                {
                    var decryptedBytes = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }

        private static void CreateDefaultEncryptedFile()
        {
            var realCreds = GetRealCredentials();
            EncryptAndSaveCredentials(realCreds);
        }

        private static string GetRealCredentials()
        {
            // ✅ Real Firebase Service Account credentials
            return @"{
  ""type"": ""service_account"",
  ""project_id"": ""kidtrack-78a49"",
  ""private_key_id"": ""386cf7ed0d9b538d6502d1fdc6f6131d6cfa35c6"",
  ""private_key"": ""-----BEGIN PRIVATE KEY-----\nMIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQDQ0m2gtvDf7tCh\n+6oxImYmAo5CElE6cntI/z0A/kWsvZuAtepOlkVPP1oVTPnge14+OmesYmI0ImZn\n4rVZ0cbwoxgt8XPsQQTpnHpaAfxAU3pxTh0iaeSuSJZq86HO5MSOGsq5b77vHKZB\nOAKU6+QgthuUsehyYR5akHa8r/Eou/fVhF7DpQBmTDYSUnXB6gcNvHRDcVzb9fOh\nsDN8E0clZjFNArPiAkcPlWf81jeJYsEYCemQntWrJ1iU8xLKYZ2NJ5GPYOUzP0es\nvni5ncIe27XloTmQnd/wMMw/pXOkELp2NK2VMLniyzDVQXgICXxr1wy5CpKmYy/M\nxRO3foW9AgMBAAECggEATfldPSdCf2Ol3O5jqRAus1+97fb4BMqNtX61MUNBEhUE\n1UVYVfrvq3087no9TfDTCop1ft2HzO7RbVYuoHjf/6fu1ez0e9H1eyPWXfii0AQ7\n0sY3w8tlvBxXql0J3P74VBW2ABM1aQS6Id0/vYrttrc5Skc6REd2dZu+8osCElKc\nrqIPpbX2HTtaPDNGPgAApfcVRfHgkxGUv0o+B2Tyq9vRHct8T64BszxUd+UvoS3P\nD33hRH24EgSCe8Vsqrv+dELLEDCGxc/SHnc9FV68/IG94mPpsmHd5s7V5gcLrKYQ\nsEniBBDQEPPlMKScQ+67EyDctXb5lUH9CqELOZpdMwKBgQD6GvGt1sGhsA7sWUYz\nklJsF0nVf51nAUI47PwKN8WgPBjHcFI8ezNghGX5PdUMEXDslPx91GI/r2DFNM04\nW6o2RcZPWhD96gZkFjWpilns+h8TLoPGBdw2I8Na/G7JZ1SP0VsOt74QdQ17TQNx\n85R/D8wXW9DVZeR2VAEvsNqb9wKBgQDVvmTeTDgvgQDClySo5quS+IA88/KbKXUK\n9b2mvzIneG096IvEgPaF/saCqBPNBCf0H1Ow3oCWgmI4zM21UI4kh1+Xh5EzzPFb\nPVlw0u0gehZEkvP2JXiS0EZUze8mhNYDNlEcH0QZDouFL4NNvQMlarBnOaErC2Nk\nQQWmqh726wKBgQCo061WioQ0r9KzCmRQBbKrkmDdxIIs+PWJ1bcg8prt2gNkBVcN\nyqBYw6bOQ0XgGpneqYdzLP2RPcKV/FmXdJEGh70g7YxQyju8Lh3VLzYauJBnc1uy\nPVx0E1oYvhPO0niLiGfuHGwpUcpi9A6iSilwR+qdzfW/R0Ob+ILAfaJj6wKBgQCy\n7ZVm+gs2yRknzHrl4WPTvq8rV1PKTCQsrpa6leeYXxmj753BO+wjM6peCfG5eDcy\nB195+mlOlYs/3UJ+/BZhwell4hjNckzBglPzPL9AprMpaJNNhQSwciXOLC584kp9\nmeTAU/QfvatSLPoQA6A0nGFbqESg0gG8FSpz4InXTQKBgHtzbI7XQMB+EtJ0nuDs\nABqc0vC6ViqIIFAleY88md2DlfyfB8P3whGIAvMDbVU/0oLc5SI5Rl4JfJAR/vEB\n+mPOYnyYZEqTtmCGeATSzTJBdFtT+6zvE4Od7LVc504/OKhUwNgcVQdy9Uz2yrq6\nkuS3r0dIcmP+Xv2UlIPICMw0\n-----END PRIVATE KEY-----\n"",
  ""client_email"": ""firebase-adminsdk-fbsvc@kidtrack-78a49.iam.gserviceaccount.com"",
  ""client_id"": ""103329568390591779450"",
  ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
  ""token_uri"": ""https://oauth2.googleapis.com/token"",
  ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
  ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/firebase-adminsdk-fbsvc%40kidtrack-78a49.iam.gserviceaccount.com"",
  ""universe_domain"": ""googleapis.com""
}";
        }

        private static string GetDefaultCredentials()
        {
            // Fallback credentials (should not be used in production)
            return @"{
                ""type"": ""service_account"",
                ""project_id"": ""kidtrack-78a49"",
                ""private_key_id"": ""fallback_key_id"",
                ""private_key"": ""-----BEGIN PRIVATE KEY-----\nfallback_private_key\n-----END PRIVATE KEY-----\n"",
                ""client_email"": ""firebase-adminsdk-fbsvc@kidtrack-78a49.iam.gserviceaccount.com"",
                ""client_id"": ""fallback_client_id"",
                ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
                ""token_uri"": ""https://oauth2.googleapis.com/token"",
                ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
                ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/firebase-adminsdk-fbsvc%40kidtrack-78a49.iam.gserviceaccount.com"",
                ""universe_domain"": ""googleapis.com""
            }";
        }
    }
}

