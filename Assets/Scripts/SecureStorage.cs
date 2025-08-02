/***************************************************************************
 *  SecureStorage (2025-06-12 - NEW)
 *  -----------------------------------------------------------------------
 *  • A static utility class for encrypting and decrypting sensitive data
 *    using AES-256 encryption.
 *  • It uses a combination of a hardcoded secret key and a dynamic,
 *    per-user salt (derived from account creation time) to generate
 *    a unique encryption key for each user. This makes it significantly
 *    harder for a cheater to apply a single hack to all users.
 ***************************************************************************/

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class SecureStorage
{
    // IMPORTANT: This key should be unique and secret for your project.
    // In a real production environment, consider using an obfuscator or
    // a more advanced key management system to protect this key.
    private const string SECRET_KEY = "Your-Super-Secret-And-Long-Key-Goes-Here-123!@#";
    private const int KEY_SIZE = 256;
    private const int BLOCK_SIZE = 128;
    private const int ITERATIONS = 10000; // PBKDF2 iterations, higher is more secure

    /// <summary>
    /// Encrypts a given plaintext string using a password and a salt.
    /// </summary>
    /// <param name="plainText">The string to encrypt.</param>
    /// <param name="saltString">The dynamic salt (e.g., user's createdAt timestamp).</param>
    /// <returns>A Base64 encoded encrypted string.</returns>
    public static string Encrypt(string plainText, string saltString)
    {
        try
        {
            byte[] salt = Encoding.UTF8.GetBytes(saltString);
            byte[] iv = new byte[16]; // Initialization Vector
            byte[] array;

            using (var aes = Aes.Create())
            {
                aes.KeySize = KEY_SIZE;
                aes.BlockSize = BLOCK_SIZE;

                // Use PBKDF2 to derive a secure key from the secret and the user's salt
                var key = new Rfc2898DeriveBytes(SECRET_KEY, salt, ITERATIONS, HashAlgorithmName.SHA256);
                aes.Key = key.GetBytes(KEY_SIZE / 8);

                // Use a random IV for each encryption for added security
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(iv);
                }
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var memoryStream = new MemoryStream())
                {
                    // Prepend the IV to the stream, so we can retrieve it for decryption
                    memoryStream.Write(iv, 0, iv.Length);
                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (var streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }
                    }
                    array = memoryStream.ToArray();
                }
            }
            return Convert.ToBase64String(array);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SecureStorage] Encryption failed: {e.Message}");
            return null;
        }
    }

   // <summary>
    /// Decrypts a given Base64 encoded string using a password and a salt.
    /// </summary>
    /// <param name="cipherText">The Base64 encoded string to decrypt.</param>
    /// <param name="saltString">The dynamic salt used during encryption.</param>
    /// <param name="suppressWarning">If true, will not log a warning on failure. Defaults to false.</param> // YENİ PARAMETRE
    /// <returns>The original plaintext string, or null if decryption fails.</returns>
    public static string Decrypt(string cipherText, string saltString, bool suppressWarning = false) // YENİ PARAMETRE EKLENDİ
    {
        if (string.IsNullOrEmpty(cipherText) || cipherText.Length < 44)
        {
            return null;
        }

        try
        {
            byte[] fullCipher = Convert.FromBase64String(cipherText);
            byte[] salt = Encoding.UTF8.GetBytes(saltString);
            byte[] iv = new byte[16];

            Array.Copy(fullCipher, 0, iv, 0, iv.Length);

            using (var aes = Aes.Create())
            {
                aes.KeySize = KEY_SIZE;
                aes.BlockSize = BLOCK_SIZE;

                var key = new Rfc2898DeriveBytes(SECRET_KEY, salt, ITERATIONS, HashAlgorithmName.SHA256);
                aes.Key = key.GetBytes(KEY_SIZE / 8);
                aes.IV = iv;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (var memoryStream = new MemoryStream())
                {
                    memoryStream.Write(fullCipher, iv.Length, fullCipher.Length - iv.Length);
                    memoryStream.Position = 0;

                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (var streamReader = new StreamReader(cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            if (!suppressWarning)
            {
                Debug.LogWarning($"[SecureStorage] Decryption failed. This is expected if data belongs to another user or is tampered. Message: {e.Message}");
            }
            return null;
        }
    }
}