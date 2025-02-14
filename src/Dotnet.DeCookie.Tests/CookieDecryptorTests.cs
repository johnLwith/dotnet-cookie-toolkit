using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dotnet.DeCookie.Tests;

public class CookieDecryptorTests
{
    private readonly CookieDecryptor _decryptor;
    private readonly string _validEncodedCookie;
    private readonly string _validProtectedPayload;
    private const string ApplicationName = "Dotnet.DeCookie";

    public CookieDecryptorTests()
    {
        _decryptor = new CookieDecryptor(ApplicationName);

        // Create a valid protected payload that mimics an ASP.NET Core authentication cookie
        var services = new ServiceCollection()
            .AddDataProtection()
            .SetApplicationName(ApplicationName)
            .Services
            .BuildServiceProvider();

        var protector = services
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector(
                "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware",
                "Cookies",
                "v2");

        var cookieData = "{\"name\":\"test-user\",\"role\":\"admin\",\"exp\":1735689600}";
        var protectedBytes = protector.Protect(Encoding.UTF8.GetBytes(cookieData));
        _validProtectedPayload = Convert.ToBase64String(protectedBytes);
        _validEncodedCookie = Uri.EscapeDataString(_validProtectedPayload);
    }

    [Fact]
    public void Constructor_WithNullApplicationName_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new CookieDecryptor(null!));
        Assert.Equal("applicationName", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhitespaceApplicationName_ThrowsArgumentException(string appName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new CookieDecryptor(appName));
        Assert.Equal("applicationName", exception.ParamName);
        Assert.Contains("cannot be empty or whitespace", exception.Message);
    }

    [Theory]
    [InlineData("MyApp")]
    [InlineData("CustomAppName")]
    [InlineData("App.With.Dots")]
    public void Constructor_WithValidApplicationName_CreatesInstance(string appName)
    {
        // Act
        var decryptor = new CookieDecryptor(appName);

        // Assert
        Assert.NotNull(decryptor);
    }

    [Fact]
    public void Decrypt_WithDifferentApplicationName_ReturnsError()
    {
        // Arrange
        var differentAppDecryptor = new CookieDecryptor("DifferentApp");
        var tempKeyDir = Path.Combine(Path.GetTempPath(), "decookie-test-keys");
        var keyPath = Path.Combine(tempKeyDir, "key.xml");
        Directory.CreateDirectory(tempKeyDir);
        try
        {
            // Create a service provider and persist the key
            var services = new ServiceCollection()
                .AddDataProtection()
                .SetApplicationName(ApplicationName) // Use original app name to create cookie
                .PersistKeysToFileSystem(new DirectoryInfo(tempKeyDir))
                .Services
                .BuildServiceProvider();

            var protector = services
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector(
                    "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware",
                    "Cookies",
                    "v2");

            // Create a protected cookie
            var cookieData = "{\"name\":\"test-user\",\"role\":\"admin\",\"exp\":1735689600}";
            var protectedBytes = protector.Protect(Encoding.UTF8.GetBytes(cookieData));
            var protectedBase64 = Convert.ToBase64String(protectedBytes);
            var encodedCookie = Uri.EscapeDataString(protectedBase64);

            // Act
            var result = differentAppDecryptor.Decrypt(encodedCookie, keyPath);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Error decrypting cookie:", result.Message);
            Assert.Null(result.DecryptedValue);
        }
        finally
        {
            // Cleanup
            try { Directory.Delete(tempKeyDir, true); } catch { }
        }
    }

    [Fact]
    public void DecodeOnly_WithEmptyCookie_ReturnsError()
    {
        // Arrange
        var cookie = string.Empty;

        // Act
        var result = _decryptor.DecodeOnly(cookie);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Cookie value is required.", result.Message);
        Assert.Null(result.DecryptedValue);
    }

    [Fact]
    public void DecodeOnly_WithInvalidBase64_ReturnsError()
    {
        // Arrange
        var cookie = "not-a-base64-string";

        // Act
        var result = _decryptor.DecodeOnly(cookie);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Error decoding cookie:", result.Message);
        Assert.Null(result.DecryptedValue);
    }

    [Fact]
    public void DecodeOnly_WithValidEncodedCookie_ReturnsDecodedValue()
    {
        // Arrange
        var cookie = _validEncodedCookie;

        // Act
        var result = _decryptor.DecodeOnly(cookie);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Cookie decoded successfully (but still encrypted).", result.Message);
        Assert.NotNull(result.DecryptedValue);
        Assert.Equal(_validProtectedPayload, result.DecryptedValue);
    }

    [Theory]
    [InlineData("CfDJ8AAAAAAAAAAAAAAAAAAAAAAAAAA%3D")]  // Valid base64, but invalid protected payload
    [InlineData("CfDJ8MT3yVVyF9yFrqvM1D1CgEsKo0w%2BVUwoRFf8MUuTnZUfpM7MXA%3D%3D")] // URL-encoded but invalid
    public void DecodeOnly_WithValidBase64ButInvalidProtectedPayload_DecodesSuccessfully(string cookie)
    {
        // Act
        var result = _decryptor.DecodeOnly(cookie);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Cookie decoded successfully (but still encrypted).", result.Message);
        Assert.NotNull(result.DecryptedValue);
    }

    [Fact]
    public void Decrypt_WithEmptyCookie_ReturnsError()
    {
        // Arrange
        var cookie = string.Empty;
        var keyPath = Path.Combine(Path.GetTempPath(), "key.xml");

        // Act
        var result = _decryptor.Decrypt(cookie, keyPath);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Cookie value is required.", result.Message);
        Assert.Null(result.DecryptedValue);
    }

    [Fact]
    public void Decrypt_WithEmptyKeyPath_ReturnsError()
    {
        // Arrange
        var cookie = "some-cookie-value";
        var keyPath = string.Empty;

        // Act
        var result = _decryptor.Decrypt(cookie, keyPath);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Key path is required.", result.Message);
        Assert.Null(result.DecryptedValue);
    }

    [Fact]
    public void Decrypt_WithInvalidKeyPath_ReturnsError()
    {
        // Arrange
        var cookie = "some-cookie-value";
        var keyPath = "invalid-path";

        // Act
        var result = _decryptor.Decrypt(cookie, keyPath);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid key file path.", result.Message);
        Assert.Null(result.DecryptedValue);
    }

    [Fact]
    public void Decrypt_WithValidCookieButWrongKey_ReturnsError()
    {
        // Arrange
        var tempKeyDir = Path.Combine(Path.GetTempPath(), "decookie-test-keys");
        var keyPath = Path.Combine(tempKeyDir, "key.xml");
        Directory.CreateDirectory(tempKeyDir);
        try
        {
            // Create a different key than the one used to protect the cookie
            var services = new ServiceCollection()
                .AddDataProtection()
                .SetApplicationName(ApplicationName)
                .PersistKeysToFileSystem(new DirectoryInfo(tempKeyDir))
                .Services
                .BuildServiceProvider();

            // Act
            var result = _decryptor.Decrypt(_validEncodedCookie, keyPath);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Error decrypting cookie:", result.Message);
            Assert.Null(result.DecryptedValue);
        }
        finally
        {
            // Cleanup
            try { Directory.Delete(tempKeyDir, true); } catch { }
        }
    }

    [Fact]
    public void Decrypt_WithValidCookieAndMatchingKey_ReturnsDecryptedValue()
    {
        // Arrange
        var tempKeyDir = Path.Combine(Path.GetTempPath(), "decookie-test-keys");
        var keyPath = Path.Combine(tempKeyDir, "key.xml");
        Directory.CreateDirectory(tempKeyDir);
        try
        {
            // Create a service provider and persist the key
            var services = new ServiceCollection()
                .AddDataProtection()
                .SetApplicationName(ApplicationName)
                .PersistKeysToFileSystem(new DirectoryInfo(tempKeyDir))
                .Services
                .BuildServiceProvider();

            var protector = services
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector(
                    "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware",
                    "Cookies",
                    "v2");

            // Create a protected cookie with the same key that will be used for decryption
            var cookieData = "{\"name\":\"test-user\",\"role\":\"admin\",\"exp\":1735689600}";
            var protectedBytes = protector.Protect(Encoding.UTF8.GetBytes(cookieData));
            var protectedBase64 = Convert.ToBase64String(protectedBytes);
            var encodedCookie = Uri.EscapeDataString(protectedBase64);

            // Act
            var result = _decryptor.Decrypt(encodedCookie, keyPath);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Cookie decrypted successfully.", result.Message);
            Assert.Equal(cookieData, result.DecryptedValue);
        }
        finally
        {
            // Cleanup
            try { Directory.Delete(tempKeyDir, true); } catch { }
        }
    }
} 