using Microsoft.AspNetCore.Mvc;

public class FriendRequestController : Controller 
{
    [HttpPost]
    [Route("/request/{Username}")]
    public IActionResult AddFriendRequest([FromHeader(Name = "AuthToken")] string token, string Username, FriendRequestModel model)
    {
        // OwnerId here is the current user
        model.OwnerId = Int32.Parse(Authenticate.GetOwnerIdFromToken(token));
        // Default status is pending
        model.Status = "Pending";
        model.Timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
        model.TargetUserId = GetOwnerId(Username);
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText =
                    @"INSERT INTO FriendRequests (OwnerId, TargetUserId, Timestamp, Status)
                    VALUES (@OwnerId, @TargetUserId, @Timestamp, @Status);";
                command.Parameters.AddWithValue("@OwnerId", model.OwnerId);
                command.Parameters.AddWithValue("@TargetUserId", model.TargetUserId);
                command.Parameters.AddWithValue("@Timestamp", model.Timestamp);
                command.Parameters.AddWithValue("@Status", model.Status);
                command.ExecuteNonQuery();
            }
        }

        var username = "";

         using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                //Getting multiple data from different tables
                command.CommandText = $@"SELECT Username FROM UserProfiles WHERE OwnerId=@OwnerId";
                command.Parameters.AddWithValue("@OwnerId", model.OwnerId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    username = reader.GetString(0);
                }
            }
        }

        // Friend request action will be added to Activites table
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = 
                    @"INSERT INTO Activities (OwnerId, Action, TargetId, Timestamp, State, Link)
                    VALUES (@OwnerId, @Action, @TargetId, @Timestamp, @State, @Link);";
                command.Parameters.AddWithValue("@OwnerId", model.OwnerId);
                command.Parameters.AddWithValue("@Action", "FriendRequest");
                command.Parameters.AddWithValue("@TargetId", model.TargetUserId);
                command.Parameters.AddWithValue("@Timestamp", model.Timestamp);
                command.Parameters.AddWithValue("@State", "Unread");
                command.Parameters.AddWithValue("@Link", "/" + username);
                command.ExecuteNonQuery();
            }
        }
        return Ok();
    }
    
    [HttpPost]
    [Route("/accept/{Username}")]
    public IActionResult AcceptFriendRequest([FromHeader(Name = "AuthToken")] string token, string Username, FriendsModel model)
    {
        // OwnerId here is the current user
        model.OwnerId = Int32.Parse(Authenticate.GetOwnerIdFromToken(token)); 
        model.FriendId = GetOwnerId(Username);
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                // Two rows will be added to Friends Table - AB and BA
                command.CommandText = 
                    @"INSERT INTO Friends (OwnerId, FriendId)
                    VALUES (@OwnerId, @FriendId), (@FriendId, @OwnerId);";
                command.Parameters.AddWithValue("@OwnerId", model.OwnerId);
                command.Parameters.AddWithValue("@FriendId", model.FriendId);
                command.ExecuteNonQuery();   
            }
        }

        var username = "";

         using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                //Getting multiple data from different tables
                command.CommandText = $@"SELECT Username FROM UserProfiles WHERE OwnerId=@OwnerId";
                command.Parameters.AddWithValue("@OwnerId", model.OwnerId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    username = reader.GetString(0);
                }
            }
        }
        
        // Accepting friend request will be added to Activities table
        using (var db = Database.OpenDatabase())
        {
            var timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
            using (var command = db.CreateCommand())
            {
                command.CommandText = 
                    @"INSERT INTO Activities (OwnerId, Action, TargetId, Timestamp, State, Link)
                    VALUES (@OwnerId, @Action, @TargetId, @Timestamp, @State, @Link);";
                command.Parameters.AddWithValue("@OwnerId", model.OwnerId);
                command.Parameters.AddWithValue("@Action", "AcceptedFriendRequest");
                command.Parameters.AddWithValue("@TargetId", model.FriendId);
                command.Parameters.AddWithValue("@Timestamp", timestamp);
                command.Parameters.AddWithValue("@State", "Unread");
                command.Parameters.AddWithValue("@Link", "/"+ username);
                command.ExecuteNonQuery();
            }
        }
        return Ok();
    }

    [HttpPatch]
    [Route("/accept/{Username}")]
    public IActionResult ChangePendingStatus([FromHeader(Name = "AuthToken")] string token, string Username)
    {
        // ownerId here is the visited profile
        var targetUserId = Int32.Parse(Authenticate.GetOwnerIdFromToken(token));
        var ownerId = GetOwnerId(Username);
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                // Username here is the OwnerId in FriendRequests Table
                command.CommandText = "UPDATE FriendRequests SET Status=@Status WHERE OwnerId=@OwnerId AND TargetUserId=@TargetUserId;";
                command.Parameters.AddWithValue("@Status", "Accepted");
                command.Parameters.AddWithValue("@TargetUserId", targetUserId);
                command.Parameters.AddWithValue("@OwnerId", ownerId);
                command.ExecuteNonQuery();
            }
        }
        return Ok();
    }

    [HttpDelete]
    [Route("/decline/{Username}")]
    public IActionResult DeleteFriendRequest([FromHeader(Name = "AuthToken")] string tokenString, string Username)
    {
        if (Authenticate.AuthenticateToken(tokenString) != "VALID")
        {
            return Ok("invalidtoken");
        }

        int targetUserId = Int32.Parse(Authenticate.GetOwnerIdFromToken(tokenString));
        var ownerId = GetOwnerId(Username);

        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"DELETE FROM FriendRequests WHERE OwnerId=@OwnerId AND TargetUserId=@TargetUserId";
                command.Parameters.AddWithValue("@OwnerId", ownerId);
                command.Parameters.AddWithValue("@TargetUserId", targetUserId);
                command.ExecuteNonQuery();
            }
        }
        return Ok();
    }

    // OwnerId here is the visited profile
    private static int GetOwnerId(string Username)
    {
        var id = 0;
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = @"SELECT * FROM UserProfiles WHERE Username=@Username;";
                command.Parameters.AddWithValue("@Username", Username);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    id = reader.GetInt32(1);
                }
            }
        }
        return id;
    }
}