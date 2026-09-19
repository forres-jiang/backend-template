using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace My.XXX.Shared.Common
{
    public class AESHelper
    {
        private static byte[] GetKey()
        {
            var encoded = Environment.GetEnvironmentVariable("APP_ENCRYPTION_KEY");
            if (string.IsNullOrWhiteSpace(encoded))
                throw new InvalidOperationException("APP_ENCRYPTION_KEY must contain a Base64-encoded 32-byte key.");
            var key = Convert.FromBase64String(encoded);
            if (key.Length != 32) throw new InvalidOperationException("APP_ENCRYPTION_KEY must be 32 bytes.");
            return key;
        }

        public static void ValidateKey()
        {
            var key = GetKey();
            CryptographicOperations.ZeroMemory(key);
        }

        public static string Encrypt(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            var key = GetKey();
            try
            {
                var nonce = RandomNumberGenerator.GetBytes(12);
                var plaintext = Encoding.UTF8.GetBytes(text);
                var ciphertext = new byte[plaintext.Length];
                var tag = new byte[16];
                using var aes = new AesGcm(key, 16);
                aes.Encrypt(nonce, plaintext, ciphertext, tag);
                var payload = new byte[28 + ciphertext.Length];
                nonce.CopyTo(payload, 0);
                tag.CopyTo(payload, 12);
                ciphertext.CopyTo(payload, 28);
                return "enc:v1:" + Convert.ToBase64String(payload);
            }
            finally { CryptographicOperations.ZeroMemory(key); }
        }

        public static string Decrypt(string text)
        {
            if (text == null || !text.StartsWith("enc:v1:", StringComparison.Ordinal))
                throw new CryptographicException("Unsupported ciphertext format. Migrate legacy encrypted settings before deployment.");
            var payload = Convert.FromBase64String(text.Substring(7));
            if (payload.Length < 28) throw new CryptographicException("Invalid ciphertext.");
            var key = GetKey();
            try
            {
                var plaintext = new byte[payload.Length - 28];
                using var aes = new AesGcm(key, 16);
                aes.Decrypt(payload.AsSpan(0, 12), payload.AsSpan(28), payload.AsSpan(12, 16), plaintext);
                return Encoding.UTF8.GetString(plaintext);
            }
            finally { CryptographicOperations.ZeroMemory(key); }
        }

        // Explicit-key overloads are retained only for offline legacy-data migration.
        public static string Encrypt(string text, string key, string iv)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(iv))
            {
                return null;
            }

            using Aes des = Aes.Create();
            des.Key = Convert.FromBase64String(key);
            des.IV = Convert.FromBase64String(iv);
            byte[] data = Encoding.UTF8.GetBytes(text);
            using MemoryStream memory = new(data);
            using CryptoStream crypto = new(memory, des.CreateEncryptor(), CryptoStreamMode.Read);
            using MemoryStream memoryStream = new();
            crypto.CopyTo(memoryStream);
            crypto.Flush();
            byte[] outData = memoryStream.ToArray();

            return Convert.ToBase64String(outData).Replace('+', '-').Replace('/', '_'); ;
        }

        public static string Decrypt(string text, string key, string iv)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(iv))
            {
                return null;
            }

            text = text.Replace('-', '+').Replace('_', '/');
            using Aes des = Aes.Create();
            des.Key = Convert.FromBase64String(key);
            des.IV = Convert.FromBase64String(iv);
            byte[] data = Convert.FromBase64String(text);
            using MemoryStream memory = new(data);
            using CryptoStream crypto = new(memory, des.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader streamReader = new(crypto);

            return streamReader.ReadToEnd();
        }
    }
}