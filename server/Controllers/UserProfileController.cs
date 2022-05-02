using Microsoft.AspNetCore.Mvc;
using System.Text;

public class UserProfileController : Controller
{
    [HttpGet]
    [Route("/{Username}")]
    public IActionResult GetUserProfile([FromHeader(Name = "AuthToken")] string tokenToBeVerified, string Username)
    {
        // Check if the token is legit (whether whoever is logged in)
        if (Authenticate.AuthenticateToken(tokenToBeVerified) != "VALID")
        {
            return Ok("invalidtoken");
        }
        // Check if Username from url exists
        if (!verifyUsername(Username))
        {
            return Ok("doesnotexist");
        }

        UserProfileModel Profile = new UserProfileModel();
        string status = "";
        // Check if OwnerId from token is the owner of the username in the url
        bool checkIsOwner = isOwner(tokenToBeVerified, Username);
        // Check if OwnerId and username in the url are friends
        bool checkAreFriends = areFriends(tokenToBeVerified, Username);
        // Check if OwnerId and username has pending friendship
        string checkHasPendingRequest = hasPendingRequest(tokenToBeVerified, Username);

        if (checkIsOwner)
        {
            status = "Owner";
        }
        else
        {
            if (checkAreFriends)
            {
                status = "Friends";
            }
            else
            {
                if (checkHasPendingRequest == "PendingRequest")
                {
                    status = "PendingRequest";
                }
                else if (checkHasPendingRequest == "PendingAccept")
                {
                    status = "PendingAccept";
                }
                else
                {
                    status = "NotFriends";
                }
            }
        }
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT * FROM UserProfiles WHERE Username=@Username;";
                command.Parameters.AddWithValue("@Username", Username);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Profile.Status = status;
                    Profile.OwnerId = reader.GetInt32(1);
                    Profile.Username = reader.GetString(2);
                    Profile.Bio = reader.GetString(3);
                    Profile.ImageSrc = reader.GetString(4);
                }
            }
        }
        return Ok(Profile);
    }

    [HttpPatch]
    [Route("/{Username}/bio")]
    public IActionResult ModifyUserProfileBio([FromHeader(Name = "AuthToken")] string tokenToBeVerified, [FromBody] UserProfileModel Profile)
    {
        var ownerId = Authenticate.GetOwnerIdFromToken(tokenToBeVerified);
        using (var db = Database.OpenDatabase())
        {
            if (Profile.Bio != null)
            {
                using (var command = db.CreateCommand())
                {
                    command.CommandText = $@"UPDATE UserProfiles SET Bio=@Bio WHERE OwnerId=@OwnerId;";
                    command.Parameters.AddWithValue("@Bio", Profile.Bio);
                    command.Parameters.AddWithValue("@OwnerId", ownerId);
                    command.ExecuteNonQuery();
                }
            }
        }
        return Ok("User Profile Bio has been updated.");
    }

    [HttpPatch]
    [Route("/{Username}/pic")]
    public IActionResult ModifyUserProfilePic([FromHeader(Name = "AuthToken")] string tokenToBeVerified, [FromBody] UserProfileModel Profile)
    {
        var ownerId = Authenticate.GetOwnerIdFromToken(tokenToBeVerified);
        using (var db = Database.OpenDatabase())
        {
            if (Profile.ImageSrc != null)
            {
                using (var command = db.CreateCommand())
                {
                    command.CommandText = $@"UPDATE UserProfiles SET ImageSrc=@ImageSrc WHERE OwnerId=@OwnerId;";
                    command.Parameters.AddWithValue("@ImageSrc", Profile.ImageSrc);
                    command.Parameters.AddWithValue("@OwnerId", ownerId);
                    command.ExecuteNonQuery();
                }
            }
        }
        return Ok("User Profile Picture has been updated.");
    }

    [HttpGet]
    [Route("/info/{ownerId}")]
    // OwnerId here is visited profile
    public IActionResult GetUserInfo([FromHeader(Name = "Status")] string Status, int OwnerId)
    {
        // Token and username already verified on GetUserProfile()
        // This function will return the modified details (UserController) of the user based on the url
        UserInfoModel User = new UserInfoModel();
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT * FROM Users WHERE Id=@Id;";
                command.Parameters.AddWithValue("@Id", OwnerId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    User.FirstName = reader.GetString(2);
                    User.LastName = reader.GetString(3);
                    User.Birthday = reader.GetInt64(5);
                    User.Gender = reader.GetString(6);
                    // Will return email address and mobile number only if the visited profile is the current user's profile or a friend
                    if (Status.Equals("Owner") || Status.Equals("Friends"))
                    {
                        User.EmailAddress = reader.GetString(4);
                        User.MobileNumber = reader.GetString(7);
                    }
                }
            }
        }
        return Ok(User);
    }

    [HttpGet]
    [Route("/getid")]
    public IActionResult GetId([FromHeader(Name = "AuthToken")] string tokenString)
    {
        if (Authenticate.AuthenticateToken(tokenString) != "VALID")
        {
            return Ok("invalidtoken");
        }
        return Ok(Authenticate.GetOwnerIdFromToken(tokenString));
    }

    public static bool verifyUsername(string Username)
    {
        bool isValidUsername = false;
        string usernamePlaceholder = "";
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT * FROM UserProfiles WHERE Username=@Username;";
                command.Parameters.AddWithValue("@Username", Username);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    usernamePlaceholder = reader.GetString(2);
                    if (usernamePlaceholder != "")
                    {
                        isValidUsername = true;
                    }
                }
            }
        }
        return isValidUsername;
    }

    public static bool isOwner(string tokenToBeVerified, string Username)
    {
        bool isOwner = false;
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT * FROM UserProfiles WHERE OwnerId=@OwnerId;";
                command.Parameters.AddWithValue("@OwnerId", Authenticate.GetOwnerIdFromToken(tokenToBeVerified));
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (Username.Equals(reader.GetString(2)))
                    {
                        isOwner = true;
                    }
                }
            }
        }
        return isOwner;
    }

    public static bool areFriends(string tokenToBeVerified, string Username)
    {
        bool areFriends = false;
        int friendId = 0;
        int friendshipId = 0;
        // Get id of username
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT * FROM UserProfiles WHERE Username=@Username;";
                command.Parameters.AddWithValue("@Username", Username);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    friendId = reader.GetInt32(1);
                }
            }
        }
        // Check if there is friendship (Friends table is two-way, AB and BA)
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT * FROM Friends WHERE OwnerId=@OwnerId AND FriendId=@FriendId;";
                command.Parameters.AddWithValue("@OwnerId", Authenticate.GetOwnerIdFromToken(tokenToBeVerified));
                command.Parameters.AddWithValue("@FriendId", friendId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    friendshipId = reader.GetInt32(0);
                }
            }
        }

        if (friendshipId != 0)
        {
            areFriends = true;
        }
        return areFriends;
    }

    public static string hasPendingRequest(string tokenToBeVerified, string Username)
    {
        var hasPendingRequest = "";
        int targetUserId = 0;
        int ownerId = Int32.Parse(Authenticate.GetOwnerIdFromToken(tokenToBeVerified));
        int requestId = 0;
        var status = "";
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = @"SELECT * FROM UserProfiles WHERE Username=@Username;";
                command.Parameters.AddWithValue("@Username", Username);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    targetUserId = reader.GetInt32(1);
                }
            }
        }
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = @"SELECT * FROM FriendRequests WHERE OwnerId=@OwnerId AND TargetUserId=@TargetUserId;";
                command.Parameters.AddWithValue("@OwnerId", ownerId);
                command.Parameters.AddWithValue("@TargetUserId", targetUserId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    requestId = reader.GetInt32(0);
                    status = reader.GetString(4);
                }
            }
            if (requestId != 0 && status == "Pending")
            {
                hasPendingRequest = "PendingRequest";

            }
        }
        using (var db = Database.OpenDatabase())
        {
            requestId = 0;
            status = "";
            using (var command = db.CreateCommand())
            {
                command.CommandText = @"SELECT * FROM FriendRequests WHERE OwnerId=@OwnerId AND TargetUserId=@TargetUserId;";
                command.Parameters.AddWithValue("@OwnerId", targetUserId);
                command.Parameters.AddWithValue("@TargetUserId", ownerId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    requestId = reader.GetInt32(0);
                    status = reader.GetString(4);
                }
            }
            if (requestId != 0 && status == "Pending")
            {
                hasPendingRequest = "PendingAccept";
            }
        }
        return hasPendingRequest;
    }

    [HttpPut]
    [Route("/info")]
    public IActionResult ModifyUserInfo([FromHeader(Name = "AuthToken")] string tokenToBeVerified, [FromBody] UserModel User)
    {
        var ownerId = Authenticate.GetOwnerIdFromToken(tokenToBeVerified);
        using (var db = Database.OpenDatabase())
        {
            if (User.FirstName != null || User.LastName != null)
            {
                using (var command = db.CreateCommand())
                {
                    command.CommandText = $@"UPDATE Users SET FirstName=@FirstName, LastName=@LastName WHERE Id=@Id;";
                    command.Parameters.AddWithValue("@FirstName", User.FirstName);
                    command.Parameters.AddWithValue("@LastName", User.LastName);
                    command.Parameters.AddWithValue("@Id", ownerId);
                    command.ExecuteNonQuery();

                    StringBuilder newUsername = new StringBuilder();
                    newUsername.Append(User.FirstName.ToLower());
                    newUsername.Append(User.LastName.ToLower());

                    int userId = Convert.ToInt32(ownerId);
                    // Check if username is existing already
                    string usernamePlaceholder = "";
                    command.CommandText = $@"SELECT * FROM UserProfiles WHERE Username=@Username EXCEPT
                    SELECT * FROM UserProfiles WHERE OwnerId=@OwnerId;";
                    command.Parameters.AddWithValue("@Username", newUsername.ToString().Replace(" ", "").Replace("-", "").Replace(".", ""));
                    command.Parameters.AddWithValue("@OwnerId", ownerId);
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        usernamePlaceholder = reader.GetString(2);
                    }
                    reader.Close();

                    if (usernamePlaceholder != "")
                    {
                        newUsername.Append(userId);
                    }

                    modifyUsername(userId, newUsername.ToString().Replace(" ", "").Replace("-", "").Replace(".", ""));
                }
            }
            if (User.MobileNumber != null)
            {
                using (var command = db.CreateCommand())
                {
                    command.CommandText = $@"UPDATE Users SET MobileNumber=@MobileNumber WHERE Id=@Id;";
                    command.Parameters.AddWithValue("@MobileNumber", User.MobileNumber);
                    command.Parameters.AddWithValue("@Id", ownerId);
                    command.ExecuteNonQuery();
                }
            }
            if (User.Birthday != 0)
            {
                using (var command = db.CreateCommand())
                {
                    command.CommandText = $@"UPDATE Users SET Birthday=@Birthday WHERE Id=@Id;";
                    command.Parameters.AddWithValue("@Birthday", User.Birthday);
                    command.Parameters.AddWithValue("@Id", ownerId);
                    command.ExecuteNonQuery();
                }
            }
        }

        string NewUsername = "";
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT * FROM UserProfiles WHERE OwnerId=@OwnerId;";
                command.Parameters.AddWithValue("@OwnerId", ownerId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    NewUsername = reader.GetString(2);
                }
                reader.Close();
            }
        }
        return Ok(NewUsername);
    }

    public static void modifyUsername(int Id, string Username)
    {
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"UPDATE UserProfiles SET Username=@Username WHERE OwnerId=@OwnerId;";
                command.Parameters.AddWithValue("@Username", Username);
                command.Parameters.AddWithValue("@OwnerId", Id);
                command.ExecuteNonQuery();
            }
        }
    }
}

