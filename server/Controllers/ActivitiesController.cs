using Microsoft.AspNetCore.Mvc;

public class ActivitiesController : Controller
{
    [HttpGet]
    [Route("/activities")]
    public IActionResult GetFriendsOfUser([FromHeader(Name = "AuthToken")] string tokenString)
    {
        if (Authenticate.AuthenticateToken(tokenString) != "VALID")
        {
            return Ok("invalidtoken");
        }

        List<ActivitiesModel> NotificationsOfUser = new List<ActivitiesModel>();
        int CurrentUserId = Int32.Parse(Authenticate.GetOwnerIdFromToken(tokenString));

        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                //Getting multiple data from different tables
                command.CommandText = $@"SELECT Users.FirstName, Users.LastName, UserProfiles.ImageSrc, Activities.Action, Activities.Timestamp, 
                Activities.Link, Activities.State FROM Activities JOIN Users ON Activities.OwnerId = Users.Id JOIN UserProfiles 
                ON Activities.OwnerId = UserProfiles.OwnerId WHERE Activities.TargetId=@TargetId
                ORDER BY Activities.Timestamp DESC";
                command.Parameters.AddWithValue("@TargetId", CurrentUserId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    ActivitiesModel Friend = new ActivitiesModel();
                    Friend.FirstName = reader.GetString(0);
                    Friend.LastName = reader.GetString(1);
                    Friend.ImageSrc = reader.GetString(2);
                    Friend.Action = reader.GetString(3);
                    Friend.Timestamp = reader.GetInt64(4);
                    Friend.Link = reader.GetString(5);
                    Friend.State = reader.GetString(6);
                    Friend.Description = getDescription(Friend.Action, Friend.FirstName, Friend.LastName);
                    Friend.Date = getReadableDate(Friend.Timestamp);

                    NotificationsOfUser.Add(Friend);
                }
            }
        }
        return Ok(NotificationsOfUser);
    }

    [HttpGet]
    [Route("/newactivity")]
    public IActionResult GetNewNotification([FromHeader(Name = "AuthToken")] string tokenString)
    {

        int CurrentUserId = Int32.Parse(Authenticate.GetOwnerIdFromToken(tokenString));
        var NotificationState = "";

        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                //Getting multiple data from different tables
                command.CommandText = $@"SELECT State FROM Activities WHERE State='Unread' AND TargetId=@TargetId";
                command.Parameters.AddWithValue("@TargetId", CurrentUserId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    NotificationState = reader.GetString(0);
                }
            }
        }
        return Ok(NotificationState);
    }

    [HttpPatch]
    [Route("/activities/read")]
    public IActionResult UpdateReadStatus([FromHeader(Name = "AuthToken")] string tokenString)
    {
        if (Authenticate.AuthenticateToken(tokenString) != "VALID")
        {
            return Ok("invalidtoken");
        }

        var targetUserId = Int32.Parse(Authenticate.GetOwnerIdFromToken(tokenString));
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                // Username here is the OwnerId in FriendRequests Table
                command.CommandText = "UPDATE Activities SET State='Read' WHERE TargetId=@TargetUserId;";
                command.Parameters.AddWithValue("@TargetUserId", targetUserId);
                command.ExecuteNonQuery();
            }
        }
        return Ok();
    }

    private string getDescription(string action, string firstName, string lastName){
        var text = "";
        if(action.Equals("FriendRequest")){
            text = firstName + " " + lastName + " has sent you a friend request.";
        }
        else if(action.Equals("AcceptedFriendRequest")){
            text = firstName + " " + lastName + " has accepted your friend request.";
        }
        else if(action.Equals("Like Post")){
            text = firstName + " " + lastName + " liked your post."; 
        }
        else if(action.Equals("Like Photo")){
            text = firstName + " " + lastName + " liked your photo.";
        }
        else if(action.Equals("Post")){
            text = firstName + " " + lastName + " has posted on your timeline.";
        }
        else if(action.Equals("Comment")){
            text = firstName + " " + lastName + " commented on your post.";
        }
        return text;
    }

    private string getReadableDate(long timeStamp){
        return TimeUtility.getRelativeTime(LongToDateTime(timeStamp));
    }

    private static DateTime LongToDateTime(long unixTimeStamp) //converts long to DateTime
    {
        // Unix timestamp is seconds past epoch
        System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        dtDateTime = dtDateTime.AddMilliseconds(Convert.ToDouble(unixTimeStamp)).ToLocalTime();
        return dtDateTime;
    }
}