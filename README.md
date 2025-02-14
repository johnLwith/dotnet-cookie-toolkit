# Dotnet.DeCookie

A .NET global tool for decrypting ASP.NET Core cookies.

## Features

- Decode URL-encoded and base64-encoded cookies
- Decrypt ASP.NET Core authentication cookies using data protection keys
- Easy-to-use command-line interface
- Support for both decoding and decryption operations

## Installation

To install the latest preview version:

```bash
dotnet tool install --global Dotnet.DeCookie --version 0.1.0-preview.1
```

To install from a local build:

```bash
dotnet tool install --global --add-source ./nupkg Dotnet.DeCookie
```

## Usage

### Decode a cookie (without decryption)
```bash
dotnet-decookie --cookie "your_cookie_value" --app-name "YourAppName"
```

### Decrypt a cookie using a key file
```bash
dotnet-decookie --cookie "your_cookie_value" --key "/path/to/key.xml" --app-name "YourAppName"
```

### Application Name
The application name is required and must match the one used by your ASP.NET Core application for data protection. This ensures that the tool can correctly decrypt cookies created by your application.

```bash
# Example with explicit application name
dotnet-decookie --cookie "your_cookie_value" --key "/path/to/key.xml" --app-name "YourAppName"
```

The key file is typically found in:
- Windows: `%LOCALAPPDATA%\ASP.NET\DataProtection-Keys`
- Linux: `~/.aspnet/DataProtection-Keys`
- macOS: `/Users/your_user/.aspnet/DataProtection-Keys`

## Examples

```bash
# Decode a cookie
dotnet-decookie --cookie "CfDJ8ICLxGAm..."

# Decrypt a cookie with a key
dotnet-decookie --cookie "CfDJ8ICLxGAm..." --key "/path/to/key.xml"
```

## Sample Project

A sample ASP.NET Core project is included in the `samples/CookieSample` directory. This project demonstrates:
- Creating protected cookies using ASP.NET Core's Data Protection
- Retrieving raw cookie values
- Decrypting cookies using both the sample app and the Dotnet.DeCookie tool

To try it out:

1. Run the sample project:
```bash
cd samples/CookieSample
dotnet run
```

2. Create a test cookie:
```bash
curl http://localhost:5000/Cookie/create
```

3. Use the returned cookie value and key location with the Dotnet.DeCookie tool.

See the [sample project's README](samples/CookieSample/README.md) for more details.

## Building from Source

```bash
git clone https://github.com/yourusername/dotnet-cookie-toolkit.git
cd dotnet-cookie-toolkit
dotnet build
dotnet pack
```

## Status

This is a preview version (0.1.0-preview.1). While functional, it may contain bugs or incomplete features. Please report any issues you encounter.

## License

MIT

---
Powered by [Cursor](https://cursor.sh) - The AI-first Code Editor