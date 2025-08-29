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
                    // Create default encrypted file if not exists
                    CreateDefaultEncryptedFile();
                    return GetDefaultCredentials();
                }
            }
            catch (Exception)
            {
                // Fallback to default if decryption fails
                return GetDefaultCredentials();
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
            var defaultCreds = GetDefaultCredentials();
            EncryptAndSaveCredentials(defaultCreds);
        }

        private static string GetDefaultCredentials()
        {
            return @"{
                ""type"": ""service_account"",
                ""project_id"": ""kidtrack-78a49"",
                ""private_key_id"": ""placeholder_key_id"",
                ""private_key"": ""placeholder_private_key"",
                ""client_email"": ""firebase-adminsdk-fbsvc@kidtrack-78a49.iam.gserviceaccount.com"",
                ""client_id"": ""placeholder_client_id"",
                ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
                ""token_uri"": ""https://oauth2.googleapis.com/token"",
                ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
                ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/firebase-adminsdk-fbsvc%40kidtrack-78a49.iam.gserviceaccount.com"",
                ""universe_domain"": ""googleapis.com""
            }";
        }
    }
}

