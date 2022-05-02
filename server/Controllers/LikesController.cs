using Microsoft.AspNetCore.Mvc;

public class LikesController : Controller
{
    [HttpPost]
    [Route("/like")]
    public IActionResult LikePhoto([FromHeader(Name = "AuthToken")] string token, [FromBody] LikeModel model)
    {
        // Owner Id here is the one who did the like action
        var ownerId = Int32.Parse(Authenticate.GetOwnerIdFromToken(token));
        var timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = 
                    @"INSERT INTO Likes (OwnerId, Target, TargetId)
                    VALUES (@OwnerId, @Target, @TargetId);";
                command.Parameters.AddWithValue("@OwnerId", ownerId);
                command.Parameters.AddWithValue("@Target", model.Target);
                command.Parameters.AddWithValue("@TargetId", model.TargetId);
                command.ExecuteNonQuery();
            }
        }

        if(ownerId != model.TargetUserId)
        {
            // Like action will be added to Activities table
            using (var db = Database.OpenDatabase())
            {
                using (var command = db.CreateCommand())
                {
                    command.CommandText = 
                        @"INSERT INTO Activities (OwnerId, Action, TargetId, Timestamp, State, Link)
                        VALUES (@OwnerId, @Action, @TargetId, @Timestamp, @State, @Link);";
                    // who did the action
                    command.Parameters.AddWithValue("@OwnerId", ownerId); 
                    command.Parameters.AddWithValue("@Action", "Like " + model.Target);
                    // who will receive the notif
                    command.Parameters.AddWithValue("@TargetId", model.TargetUserId); 
                    command.Parameters.AddWithValue("@Timestamp", timestamp); // when the action is done
                    command.Parameters.AddWithValue("@State", "Unread"); // default
                    if(model.Target=="Photo")
                    {
                        command.Parameters.AddWithValue("@Link", "/"+model.Username+"/albums/"+model.AlbumId+"/"+model.TargetId);
                    }
                    else if(model.Target=="Post")
                    {
                        command.Parameters.AddWithValue("@Link", "/posts/"+model.TargetId);
                    }
                    command.ExecuteNonQuery();
                }
            }
        }
        return Ok();
    }

    [HttpGet]
    // targetId here is either photoId or postId
    [Route("/likes/{targetId}")]
    public IActionResult CheckLikeStatus(int targetId, [FromHeader(Name = "Target")] string target, [FromHeader(Name = "AuthToken")] string token)
    {
        LikeModel model = new LikeModel();
        var currentUser = Int32.Parse(Authenticate.GetOwnerIdFromToken(token));
        var ownerId = 0;
        var isLiked = false;
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = "SELECT * FROM Likes WHERE TargetId=@TargetId AND Target=@Target;";
                command.Parameters.AddWithValue("@TargetId", targetId);
                command.Parameters.AddWithValue("@Target", target);
                var reader = command.ExecuteReader();
                while(reader.Read())
                {
                    ownerId = reader.GetInt32(1);
                    if(ownerId == currentUser)
                    {
                        isLiked = true;
                    }
                }
            }
        }
        return Ok(isLiked);
    }

    [HttpDelete]
    [Route("/unlike/{targetId}")]
    public IActionResult Unlike(int targetId, [FromHeader(Name = "Action")] string action, [FromHeader(Name = "AuthToken")] string token)
    {
        var ownerId = Int32.Parse(Authenticate.GetOwnerIdFromToken(token));
        LikeModel model = new LikeModel();
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = "DELETE FROM Likes WHERE TargetId=@TargetId AND OwnerId=@OwnerId;";
                command.Parameters.AddWithValue(@"TargetId", targetId);
                command.Parameters.AddWithValue("@OwnerId", ownerId);
                var reader = command.ExecuteReader();
                while(reader.Read())
                {
                    model.Id = reader.GetInt32(0);
                    model.OwnerId = reader.GetInt32(1);
                    model.Target = reader.GetString(2);
                }
            }
        }
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = "DELETE FROM Activities WHERE OwnerId=@OwnerId and Action=@Action;";
                command.Parameters.AddWithValue("@OwnerId", ownerId);
                command.Parameters.AddWithValue("@Action", action);
                command.ExecuteNonQuery();
            }
        }
        return Ok();
    }

    [HttpGet]
    [Route("/likes/list/{targetId}")]
    public IActionResult GetListOfLikes(int targetId, [FromHeader(Name = "Target")] string target)
    {
        
        var list = new List<ListOfLikesModel>();
        var likedBy = new List<int>();
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = "SELECT * FROM Likes WHERE TargetId=@TargetId AND Target=@Target;";
                command.Parameters.AddWithValue("@TargetId", targetId);
                command.Parameters.AddWithValue("@Target", target);
                var reader = command.ExecuteReader();
                while(reader.Read())
                {
                   likedBy.Add(reader.GetInt32(1));
                }
            }
        }
        
        for(int x = 0; x < likedBy.Count; x++)
        {
            using (var db = Database.OpenDatabase())
            {
                using (var command = db.CreateCommand())
                {
                    command.CommandText =
                        @"SELECT Likes.OwnerId, Users.FirstName, Users.LastName, UserProfiles.Username, UserProfiles.ImageSrc
                        FROM Likes
                        JOIN Users
                        ON Likes.OwnerId = Users.Id
                        JOIN UserProfiles
                        ON Users.Id = UserProfiles.OwnerId
                        WHERE Likes.OwnerId=@OwnerId AND Likes.Target=@Target AND Likes.TargetId=@TargetId;";
                    command.Parameters.AddWithValue("@OwnerId", likedBy[x]);
                    command.Parameters.AddWithValue("@Target", target);
                    command.Parameters.AddWithValue("@TargetId", targetId);
                    var reader = command.ExecuteReader();
                    while(reader.Read())
                    {
                        list.Add(new ListOfLikesModel {
                            OwnerId = reader.GetInt32(0),
                            FirstName = reader.GetString(1),
                            LastName = reader.GetString(2),
                            Username = reader.GetString(3),
                            ProfilePic = reader.GetString(4)
                        });
                    }
                }
            }
        }
        return Json(list);
    }
}