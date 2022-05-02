using Microsoft.AspNetCore.Mvc;

public class FriendsController : Controller
{
    [HttpGet]
    [Route("/friends")]
    public IActionResult GetFriendsOfUser([FromHeader(Name = "AuthToken")] string tokenString)
    {
        if (Authenticate.AuthenticateToken(tokenString) != "VALID")
        {
            return Ok("invalidtoken");
        }

        List<UserProfileModel> FriendsOfUser = new List<UserProfileModel>();
        int CurrentUserId = Int32.Parse(Authenticate.GetOwnerIdFromToken(tokenString));

        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                //Getting multiple data from different tables
                command.CommandText = $@"SELECT Users.FirstName, Users.LastName, UserProfiles.Username, 
                UserProfiles.ImageSrc FROM Friends JOIN Users ON 
                Friends.FriendId = Users.Id JOIN UserProfiles ON Friends.FriendId = UserProfiles.OwnerId 
                WHERE Friends.OwnerId=@OwnerId";
                command.Parameters.AddWithValue("@OwnerId", CurrentUserId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    UserProfileModel Friend = new UserProfileModel();
                    Friend.FirstName = reader.GetString(0);
                    Friend.LastName = reader.GetString(1);
                    Friend.Username = reader.GetString(2);
                    Friend.ImageSrc = reader.GetString(3);

                    FriendsOfUser.Add(Friend);
                }
            }
        }
        return Ok(FriendsOfUser);
    }

    [HttpGet]
    [Route("/friendrequest")]
    public IActionResult GetFriendRequestsOfUser([FromHeader(Name = "AuthToken")] string tokenString)
    {
        if (Authenticate.AuthenticateToken(tokenString) != "VALID")
        {
            return Ok("invalidtoken");
        }

        List<UserProfileModel> FriendsOfUser = new List<UserProfileModel>();
        int CurrentUserId = Int32.Parse(Authenticate.GetOwnerIdFromToken(tokenString));

        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                //Getting multiple data from different tables
                command.CommandText = $@"SELECT Users.FirstName, Users.LastName, UserProfiles.Username, UserProfiles.ImageSrc 
                FROM FriendRequests JOIN Users ON FriendRequests.OwnerId = Users.Id 
                JOIN UserProfiles ON FriendRequests.OwnerId = UserProfiles.OwnerId 
                WHERE FriendRequests.Status='Pending' AND FriendRequests.TargetUserId=@TargetUserId";
                command.Parameters.AddWithValue("@TargetUserId", CurrentUserId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    UserProfileModel Friend = new UserProfileModel();
                    Friend.FirstName = reader.GetString(0);
                    Friend.LastName = reader.GetString(1);
                    Friend.Username = reader.GetString(2);
                    Friend.ImageSrc = reader.GetString(3);

                    FriendsOfUser.Add(Friend);
                }
            }
        }
        return Ok(FriendsOfUser);
    }
}