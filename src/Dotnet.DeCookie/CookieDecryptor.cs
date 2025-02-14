using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnet.DeCookie;

public class CookieDecryptor
{
    private readonly string _applicationName;

    public CookieDecryptor(string applicationName)
    {
        if (applicationName == null)
            throw new ArgumentNullException(nameof(applicationName));
        
        if (string.IsNullOrWhiteSpace(applicationName))
            throw new ArgumentException("Application name cannot be empty or whitespace.", nameof(applicationName));

        _applicationName = applicationName;
    }

    public record DecryptionResult(bool Success, string Message, string? DecryptedValue = null);

    public DecryptionResult DecodeOnly(string cookie)
    {
        try
        {
            if (string.IsNullOrEmpty(cookie))
            {
                return new DecryptionResult(false, "Cookie value is required.");
            }

            // URL decode and Base64 decode the cookie
            var decodedCookie = Uri.UnescapeDataString(cookie);

            // Validate that the decoded value is a valid base64 string
            if (!IsValidBase64String(decodedCookie))
            {
                return new DecryptionResult(false, "Error decoding cookie: Invalid base64 string.");
            }

            return new DecryptionResult(true, "Cookie decoded successfully (but still encrypted).", decodedCookie);
        }
        catch (Exception ex)
        {
            return new DecryptionResult(false, $"Error decoding cookie: {ex.Message}");
        }
    }

    public DecryptionResult Decrypt(string cookie, string keyPath)
    {
        try
        {
            if (string.IsNullOrEmpty(cookie))
            {
                return new DecryptionResult(false, "Cookie value is required.");
            }

            if (string.IsNullOrEmpty(keyPath))
            {
                return new DecryptionResult(false, "Key path is required.");
            }

            var keyDirectory = Path.GetDirectoryName(keyPath);
            if (string.IsNullOrEmpty(keyDirectory))
            {
                return new DecryptionResult(false, "Invalid key file path.");
            }

            // URL decode and Base64 decode the cookie
            var decodedCookie = Uri.UnescapeDataString(cookie);
            if (!IsValidBase64String(decodedCookie))
            {
                return new DecryptionResult(false, "Error decoding cookie: Invalid base64 string.");
            }

            var protectedPayload = Convert.FromBase64String(decodedCookie);

            // Create data protection provider using the specified key
            var services = new ServiceCollection()
                .AddDataProtection()
                .SetApplicationName(_applicationName)
                .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory))
                .Services
                .BuildServiceProvider();

            var dataProtection = services.GetRequiredService<IDataProtectionProvider>();
            var protector = dataProtection.CreateProtector(
                "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware",
                "Cookies",
                "v2");

            // Unprotect the data
            var unprotectedBytes = protector.Unprotect(protectedPayload);
            var unprotectedText = Encoding.UTF8.GetString(unprotectedBytes);

            return new DecryptionResult(true, "Cookie decrypted successfully.", unprotectedText);
        }
        catch (Exception ex)
        {
            return new DecryptionResult(false, $"Error decrypting cookie: {ex.Message}");
        }
    }

    private bool IsValidBase64String(string value)
    {
        try
        {
            Span<byte> buffer = new byte[value.Length];
            return Convert.TryFromBase64String(value, buffer, out _);
        }
        catch
        {
            return false;
        }
    }
} 