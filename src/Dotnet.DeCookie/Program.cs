using System.CommandLine;

namespace Dotnet.DeCookie;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var cookieOption = new Option<string>(
            name: "--cookie",
            description: "The cookie value to decrypt")
        { IsRequired = true };

        var keyOption = new Option<string>(
            name: "--key",
            description: "The data protection key (usually found in %LOCALAPPDATA%\\ASP.NET\\DataProtection-Keys or ~/.aspnet/DataProtection-Keys)");

        var appNameOption = new Option<string>(
            name: "--app-name",
            description: "The application name used for data protection")
        { IsRequired = true };

        var rootCommand = new RootCommand("ASP.NET Core Cookie Decryption Tool");
        rootCommand.AddOption(cookieOption);
        rootCommand.AddOption(keyOption);
        rootCommand.AddOption(appNameOption);

        rootCommand.SetHandler(async (string cookie, string? key, string appName) =>
        {
            var decryptor = new CookieDecryptor(appName);
            var result = !string.IsNullOrEmpty(key) 
                ? decryptor.Decrypt(cookie, key)
                : decryptor.DecodeOnly(cookie);

            if (result.Success)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    Console.WriteLine($"Decrypted cookie value: {result.DecryptedValue}");
                }
                else
                {
                    Console.WriteLine("Warning: No key provided. Showing decoded (but still encrypted) value:");
                    Console.WriteLine(result.DecryptedValue);
                }
            }
            else
            {
                Console.WriteLine(result.Message);
            }
        }, cookieOption, keyOption, appNameOption);

        return await rootCommand.InvokeAsync(args);
    }
}
