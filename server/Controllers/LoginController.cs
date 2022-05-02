using Microsoft.AspNetCore.Mvc;

public class LoginController : Controller
{

    [HttpPost]
    [Route("/login")]
    public IActionResult Login([FromBody] LoginModel LoginCredentials)
    {
        string OwnerId = "";
        bool correctPassword = false;
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT * FROM Users WHERE EmailAddress=@EmailAddress;";
                command.Parameters.AddWithValue("@EmailAddress", LoginCredentials.Email);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    OwnerId = reader.GetInt32(0).ToString();
                    correctPassword = BCrypt.Net.BCrypt.Verify(LoginCredentials.Password, reader.GetString(1));
                }
            }
        }

        if (correctPassword)
        {
            var newToken = Authenticate.GenerateToken(OwnerId);
            return Ok(newToken);
        }
        else
        {
            return Ok("invalid");
        }
    }

    [HttpPost]
    [Route("/verifylocalstoragetoken")]
    public IActionResult VerifyLocalStorageToken([FromHeader(Name = "AuthToken")] string tokenToBeVerified)
    {
        var IsValidToken = Authenticate.AuthenticateToken(tokenToBeVerified);
        return Ok(IsValidToken);
    }

    [HttpPost]
    [Route("/getcurrent")]
    public IActionResult GetCurrentUser([FromHeader(Name = "AuthToken")] string tokenString)
    {
        Dictionary<string, dynamic> CurrentUser = new Dictionary<string, dynamic>();
        int userId = Int32.Parse(Authenticate.GetOwnerIdFromToken(tokenString));
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT * FROM UserProfiles WHERE OwnerId=@OwnerId;";
                command.Parameters.AddWithValue("@OwnerId", userId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    CurrentUser.Add("Username", reader.GetString(2));
                    CurrentUser.Add("ImageSrc", reader.GetString(4));
                }
            }
        }

        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT * FROM Users WHERE Id=@Id;";
                command.Parameters.AddWithValue("@Id", userId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    CurrentUser.Add("FirstName", reader.GetString(2));
                    CurrentUser.Add("LastName", reader.GetString(3));
                }
            }
        }

        return Ok(CurrentUser);
    }

}