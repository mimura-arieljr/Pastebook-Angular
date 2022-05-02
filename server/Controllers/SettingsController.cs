using Microsoft.AspNetCore.Mvc;

public class SettingsController : Controller
{

    [HttpPut]
    [Route("/updateuserinfo/{Id}")]
    public IActionResult UpdateUserInfo(int Id, [FromHeader(Name = "AuthToken")] string tokenToBeVerified, [FromBody] UserModel User)
    {
        var result = Authenticate.AuthenticateToken(tokenToBeVerified);
        if ("VALID".Equals(result))
        {
            using (var db = Database.OpenDatabase())
            {
                using (var command = db.CreateCommand())
                {
                    command.CommandText = $@"UPDATE Users SET FirstName=@FirstName, LastName=@LastName, Birthday=@Birthday, Gender=@Gender, MobileNumber=@MobileNumber WHERE Id={Id};";
                    command.Parameters.AddWithValue("@FirstName", User.FirstName);
                    command.Parameters.AddWithValue("@LastName", User.LastName);
                    command.Parameters.AddWithValue("@Birthday", User.Birthday);
                    command.Parameters.AddWithValue("@Gender", User.Gender);
                    command.Parameters.AddWithValue("@MobileNumber", User.MobileNumber);
                    command.ExecuteNonQuery();
                }
            }
            return Ok("User info has been updated.");
        }
        return Forbid("User token invalid or expired.");
    }

    [HttpPut]
    [Route("/updateusersecurity")]
    public IActionResult UpdateUserSecurity([FromHeader(Name = "AuthToken")] string tokenToBeVerified, [FromBody] UserModel User)
    {
        var result = Authenticate.AuthenticateToken(tokenToBeVerified);
        if ("VALID".Equals(result))
        {
            var OwnerId = Authenticate.GetOwnerIdFromToken(tokenToBeVerified);
            bool correctPassword = false;
            using (var db = Database.OpenDatabase())
            {
                Console.WriteLine("Authenticating user with Id: " + OwnerId);
                var currentPassword = User.CurrentPassword;
                using (var command = db.CreateCommand())
                {
                    command.CommandText = $@"SELECT Id,Password FROM Users WHERE Id=@Id;";
                    command.Parameters.AddWithValue("@Id", OwnerId);
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        correctPassword = BCrypt.Net.BCrypt.Verify(currentPassword, reader.GetString(1));
                    }
                }
            }

            if (correctPassword)
            {
                if (RegistrationController.checkDuplicateEmail(User.EmailAddress))
                {
                    return Ok("Duplicate");
                }

                using (var db = Database.OpenDatabase())
                {
                    using (var command = db.CreateCommand())
                    {
                        command.CommandText = $@"UPDATE Users SET Password=@Password, EmailAddress=@EmailAddress WHERE Id=@Id;";
                        command.Parameters.AddWithValue("@Id", OwnerId);
                        var hashedpassword = BCrypt.Net.BCrypt.HashPassword(User.Password);
                        command.Parameters.AddWithValue("@Password", hashedpassword);
                        command.Parameters.AddWithValue("@EmailAddress", User.EmailAddress);
                        command.ExecuteNonQuery();

                        string updateEmailBody = "<p>You have successfully updated your Email Address/Password</p>";
                        RegistrationController.SendEmailtoUser(User.EmailAddress, updateEmailBody);          
                    }
                }
                return Ok("User security has been updated.");
            }
            else
            {
                return Ok("Incorrect Password");
            }
        }
        return Ok("User token invalid or expired.");
    }
}