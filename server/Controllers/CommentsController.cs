using Microsoft.AspNetCore.Mvc;

public class CommentsController : Controller
{
    [HttpPost]
    [Route("/comments")]
    public IActionResult PostComment([FromHeader(Name = "AuthToken")] string tokenString, [FromBody] CommentModel Comment)
    {
        if (Authenticate.AuthenticateToken(tokenString) != "VALID")
        {
            return Ok("invalidtoken");
        }
        long currentTime = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"INSERT INTO Comments (OwnerId, PostId, Timestamp, Content) VALUES (@OwnerId, @PostId, @Timestamp, @Content);";
                command.Parameters.AddWithValue("@OwnerId", Comment.OwnerId);
                command.Parameters.AddWithValue("@PostId", Comment.PostId);
                command.Parameters.AddWithValue("@Timestamp", currentTime);
                command.Parameters.AddWithValue("@Content", Comment.Content);
                command.ExecuteNonQuery();
            }
        }

        if (Comment.OwnerId != Comment.TargetUserId)
        {
            // Comment action will be added to Activites table
            using (var db = Database.OpenDatabase())
            {
                using (var command = db.CreateCommand())
                {
                    command.CommandText =
                        @"INSERT INTO Activities (OwnerId, Action, TargetId, Timestamp, State, Link)
                    VALUES (@OwnerId, @Action, @TargetId, @Timestamp, @State, @Link);";
                    command.Parameters.AddWithValue("@OwnerId", Comment.OwnerId);
                    command.Parameters.AddWithValue("@Action", "Comment");
                    command.Parameters.AddWithValue("@TargetId", Comment.TargetUserId);
                    command.Parameters.AddWithValue("@Timestamp", currentTime);
                    command.Parameters.AddWithValue("@State", "Unread");
                    command.Parameters.AddWithValue("@Link", "/posts/" + Comment.PostId);
                    command.ExecuteNonQuery();
                }
            }
        }
        CommentModel CommentDetail = new CommentModel();
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {

                command.CommandText = $@"SELECT TOP 1 Comments.OwnerId, Comments.PostId, Comments.Timestamp, Comments.Content,
                Users.FirstName as FirstName, Users.LastName as LastName, UserProfiles.Username as Username, UserProfiles.ImageSrc as ImageSrc
                FROM Comments
                INNER JOIN Users ON Comments.OwnerId=Users.Id
                INNER JOIN UserProfiles ON Comments.OwnerId=UserProfiles.OwnerId
				ORDER BY Comments.Id DESC;";
                var reader = command.ExecuteReader();
                while (reader.Read())
                {

                    CommentDetail.OwnerId = Convert.ToInt32(reader["OwnerId"]);
                    CommentDetail.PostId = Convert.ToInt32(reader["PostId"]);
                    CommentDetail.Timestamp = Convert.ToInt64(reader["Timestamp"]);
                    CommentDetail.Content = Convert.ToString(reader["Content"]);
                    CommentDetail.FirstName = Convert.ToString(reader["FirstName"]);
                    CommentDetail.LastName = Convert.ToString(reader["LastName"]);
                    CommentDetail.Username = Convert.ToString(reader["Username"]);
                    CommentDetail.ImageSrc = Convert.ToString(reader["ImageSrc"]);
                }
            }
        }
        return Ok(CommentDetail);
    }

    [HttpGet]
    [Route("/comments")]
    public IActionResult GetComments([FromHeader(Name="PostId")] int PostId)
    {
        List<CommentModel> CommentDetails = new List<CommentModel>();
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {

                command.CommandText = $@"SELECT Comments.OwnerId, Comments.PostId, Comments.Timestamp, Comments.Content,
                Users.FirstName as FirstName, Users.LastName as LastName, UserProfiles.Username as Username, UserProfiles.ImageSrc as ImageSrc
                FROM Comments
                INNER JOIN Users ON Comments.OwnerId=Users.Id
                INNER JOIN UserProfiles ON Comments.OwnerId=UserProfiles.OwnerId
				WHERE Comments.PostId=@PostId";
                command.Parameters.AddWithValue("@PostId", PostId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    CommentModel CommentDetail = new CommentModel();
                    CommentDetail.OwnerId = Convert.ToInt32(reader["OwnerId"]);
                    CommentDetail.PostId = Convert.ToInt32(reader["PostId"]);
                    CommentDetail.Timestamp = Convert.ToInt64(reader["Timestamp"]);
                    CommentDetail.Content = Convert.ToString(reader["Content"]);
                    CommentDetail.FirstName = Convert.ToString(reader["FirstName"]);
                    CommentDetail.LastName = Convert.ToString(reader["LastName"]);
                    CommentDetail.Username = Convert.ToString(reader["Username"]);
                    CommentDetail.ImageSrc = Convert.ToString(reader["ImageSrc"]);
                    CommentDetails.Add(CommentDetail);
                }
            }
        }
        return Ok(CommentDetails);
    }
}