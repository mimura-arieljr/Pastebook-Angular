using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

// Requires a JWT package of which the installation CLI is shown below
// `dotnet add package System.IdentityModel.Tokens.Jwt --version 6.15.0`

public class Authenticate
{   
    // Key should be set in the environment
    private static string key = "mysecretspecialsignature1234";
    public static string GenerateToken(string OwnerId)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var header = new JwtHeader(credentials);
        var payload = new JwtPayload
           {
                { "OwnerId", OwnerId },
                // Adjust expiration time by changing 'AddMinutes(2)' to 'AddHours(x)', 'AddDays(x)', etc.
                { "Exp", new DateTimeOffset(DateTime.Now.AddDays(3)).ToUnixTimeMilliseconds().ToString()}
           };

        var secToken = new JwtSecurityToken(header, payload);
        var handler = new JwtSecurityTokenHandler();
        var tokenString = handler.WriteToken(secToken);

        return tokenString;
    }

    public static string AuthenticateToken(string tokenString)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        HMACSHA256 hmac = new HMACSHA256(keyBytes);

        byte[] signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(tokenString.Split(".")[0] + "." + tokenString.Split(".")[1]));
        string signature = Base64UrlEncode(signatureBytes);

        // Check if posted signature is valid
        bool isValidSignature = tokenString.Split(".")[2].Equals(signature);
        if (!isValidSignature)
        {
            return "INVALID";
        }

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);
        long currentTime = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();

        // Checks if token is expired by comparing the difference of current timestamp
        long isExpired = long.Parse(token.Claims.First(c => c.Type == "Exp").Value) - currentTime;
        if (isExpired <= 0)
        {
            return "EXPIRED";
        }
        else
        {
            return "VALID";
        }   
    }

    public static string GetOwnerIdFromToken(string tokenString)
    {
        string OwnerId = "";

        var result = AuthenticateToken(tokenString);
        if("VALID".Equals(result)){

            //Get OwnerId from Token
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            OwnerId = token.Claims.First(c => c.Type == "OwnerId").Value;
        }

        return OwnerId;
    }

    // JWT Tokens are Base64Url Encoded not Base64 therefore this simple converter is used
    private static string Base64UrlEncode(byte[] input)
    {
        var output = Convert.ToBase64String(input);
        output = output.Split('=')[0]; // Remove any trailing '='s
        output = output.Replace('+', '-'); // 62nd char of encoding
        output = output.Replace('/', '_'); // 63rd char of encoding
        return output;
    }
}