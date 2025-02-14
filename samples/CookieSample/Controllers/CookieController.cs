using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;

namespace CookieSample.Controllers;

[ApiController]
[Route("[controller]")]
public class CookieController : ControllerBase
{
    private readonly IDataProtector _protector;

    public CookieController(IDataProtectionProvider protectionProvider)
    {
        _protector = protectionProvider.CreateProtector(
            "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware",
            "Cookies",
            "v2");
    }

    [HttpGet("create")]
    public IActionResult CreateCookie()
    {
        // Create a sample cookie payload
        var cookieData = new
        {
            name = "test-user",
            role = "admin",
            exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        };

        // Convert to JSON
        var jsonData = JsonSerializer.Serialize(cookieData);

        // Protect the cookie data
        var protectedBytes = _protector.Protect(System.Text.Encoding.UTF8.GetBytes(jsonData));
        var protectedBase64 = Convert.ToBase64String(protectedBytes);

        // Set the cookie
        Response.Cookies.Append("TestCookie", protectedBase64);

        // Return both cookie value and key location
        return Ok(new
        {
            cookie = protectedBase64,
            keyLocation = OperatingSystem.IsLinux() 
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".aspnet",
                    "DataProtection-Keys")
                : Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ASP.NET",
                    "DataProtection-Keys")
        });
    }

    [HttpGet("raw")]
    public IActionResult GetRawCookie()
    {
        // Try to get the cookie
        if (!Request.Cookies.TryGetValue("TestCookie", out var cookieValue))
        {
            return NotFound("Cookie not found. Create one first using /Cookie/create");
        }

        return Ok(new { cookie = cookieValue });
    }

    [HttpGet("decrypt")]
    public IActionResult DecryptCookie()
    {
        // Try to get the cookie
        if (!Request.Cookies.TryGetValue("TestCookie", out var cookieValue))
        {
            return NotFound("Cookie not found. Create one first using /Cookie/create");
        }

        try
        {
            Console.WriteLine(cookieValue);

            // Decode and decrypt
            var protectedBytes = Convert.FromBase64String(cookieValue);
            var unprotectedBytes = _protector.Unprotect(protectedBytes);
            var jsonData = System.Text.Encoding.UTF8.GetString(unprotectedBytes);

            // Parse and return
            return Ok(JsonSerializer.Deserialize<object>(jsonData));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to decrypt cookie", details = ex.Message });
        }
    }
} 