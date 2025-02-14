# Cookie Sample Project

This sample project demonstrates how to create and protect cookies that can be decrypted using the Dotnet.DeCookie tool.

## Running the Sample

1. Start the project:
```bash
dotnet run
```

2. Create a protected cookie:
```bash
curl http://localhost:5285/Cookie/create
```
This will return:
- The protected cookie value
- The location of the data protection key

3. Get the raw cookie value:
```bash
curl http://localhost:5285/Cookie/raw
```

4. View the decrypted cookie (using the sample app):
```bash
curl http://localhost:5285/Cookie/decrypt
```

## Using with Dotnet.DeCookie

1. Get the cookie value from the `/Cookie/raw` endpoint
2. Note the key location from the `/Cookie/create` endpoint
3. Use the Dotnet.DeCookie tool:

```bash
# Just decode the cookie (without decryption)
dotnet-decookie --cookie "your_cookie_value" --app-name "Dotnet.DeCookie"

# Decrypt the cookie using the key
dotnet-decookie --cookie "your_cookie_value" --key "/path/to/key.xml" --app-name "Dotnet.DeCookie"
```

### Application Name Configuration

The application name is required for data protection and must match between your application and the decryption tool. By default, this sample uses "Dotnet.DeCookie".

1. In `appsettings.json`:
```json
{
  "DataProtection": {
    "ApplicationName": "Dotnet.DeCookie"  // Must match the --app-name parameter
  }
}
```

2. When using the Dotnet.DeCookie tool, always specify the matching application name:
```bash
dotnet-decookie --cookie "your_cookie_value" --key "/path/to/key.xml" --app-name "Dotnet.DeCookie"
```

**Note:** The application name must match exactly between your app and the decryption tool, or decryption will fail.

## Cookie Format

The sample creates a cookie with this structure:
```json
{
    "name": "test-user",
    "role": "admin",
    "exp": 1234567890
}
```

The cookie is:
1. Serialized to JSON
2. Protected using ASP.NET Core's Data Protection
3. Base64 encoded
4. Stored as a cookie named "TestCookie" 