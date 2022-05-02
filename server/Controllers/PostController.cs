using System.Dynamic;
using Microsoft.AspNetCore.Mvc;

public class PostController : Controller
{
    [HttpPost]
    [Route("/createpost/{visitedUsername}")]
    public IActionResult CreatePost([FromHeader(Name = "AuthToken")] string tokenString, [FromBody] PostModel Post, string visitedUsername)
    {
        if (Authenticate.AuthenticateToken(tokenString) != "VALID")
        {
            return Ok("invalidtoken");
        }
        int OwnerId = Int32.Parse(Authenticate.GetOwnerIdFromToken(tokenString));
        long datetimePosted = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
        int visitedProfileId = 0;

        if (visitedUsername != "newsfeed")
        {
            using (var db = Database.OpenDatabase())
            {
                using (var command = db.CreateCommand())
                {
                    command.CommandText = $@"SELECT * FROM UserProfiles WHERE Username=@Username";
                    command.Parameters.AddWithValue("@Username", visitedUsername);
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        visitedProfileId = reader.GetInt32(1);
                        if (visitedProfileId == OwnerId) {
                            visitedProfileId = 0;
                        }
                    }
                }
            }
        }
       
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"INSERT INTO Posts 
                (OwnerId, Timestamp, Timeline, Content, ImageSrc)
                VALUES (@OwnerId, @Timestamp, @Timeline, @Content, @ImageSrc);";
                command.Parameters.AddWithValue("@OwnerId", OwnerId);
                command.Parameters.AddWithValue("@Timestamp", datetimePosted);
                command.Parameters.AddWithValue("@Timeline", visitedProfileId);
                command.Parameters.AddWithValue("@Content", Post.Content);
                command.Parameters.AddWithValue("@ImageSrc", Post.ImageSrc);
                command.ExecuteNonQuery();
            }
        }

        dynamic RecentPost = new ExpandoObject();
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT Posts.Id, Posts.OwnerId, Posts.Timestamp, Posts.Timeline, Posts.Content, Posts.ImageSrc,
                Users.FirstName as FirstName, Users.LastName as LastName, UserProfiles.Username as Username, UserProfiles.ImageSrc as OwnerImage
                From Posts
                INNER JOIN Users ON Posts.OwnerId=Users.Id
                INNER JOIN UserProfiles ON Posts.OwnerId=UserProfiles.OwnerId
                WHERE Posts.OwnerId=@PostOwnerId
                ORDER BY Timestamp DESC
                OFFSET (0) ROWS
                FETCH NEXT 1 ROWS ONLY;";
                command.Parameters.AddWithValue("@PostOwnerId", OwnerId);
                command.Parameters.AddWithValue("@Timeline", visitedProfileId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    // Id of post
                    RecentPost.Id = Convert.ToInt32(reader["Id"]);
                    // Id of post owner
                    RecentPost.OwnerId = Convert.ToInt32(reader["OwnerId"]);
                    RecentPost.Timestamp = Convert.ToInt64(reader["Timestamp"]);
                    RecentPost.Timeline = Convert.ToInt32(reader["Timeline"]);
                    RecentPost.Content = Convert.ToString(reader["Content"]);
                    RecentPost.ImageSrc = Convert.ToString(reader["ImageSrc"]);
                    RecentPost.NumLikes = GetLikes(Convert.ToInt32(reader["Id"]));
                    RecentPost.NumComments = GetComments(Convert.ToInt32(reader["Id"]));
                    RecentPost.FirstName = Convert.ToString(reader["FirstName"]);
                    RecentPost.LastName = Convert.ToString(reader["LastName"]);
                    RecentPost.Username = Convert.ToString(reader["Username"]);
                    RecentPost.OwnerImage = Convert.ToString(reader["OwnerImage"]);
                    RecentPost.TargetFirstName = GetTargerUser(Convert.ToInt32(reader["Timeline"])).FirstName;
                    RecentPost.TargetLastName = GetTargerUser(Convert.ToInt32(reader["Timeline"])).LastName;
                    RecentPost.TargetUsername = GetTargerUser(Convert.ToInt32(reader["Timeline"])).Username;
                }
            }
        }
        return Ok(RecentPost);
    }

    [HttpGet]
    [Route("/newsfeedposts/{page}")]
    public IActionResult NewsFeedPosts([FromHeader(Name = "AuthToken")] string tokenString, int Page)
    {
        if (Authenticate.AuthenticateToken(tokenString) != "VALID")
        {
            return Ok("invalidtoken");
        }
        dynamic NewsFeedPosts = new List<ExpandoObject>();
        int CurrentUserId = Int32.Parse(Authenticate.GetOwnerIdFromToken(tokenString));
        // AddDays(-14) parses posts within the last 2 weeks
        long postsAfterThisDate = new DateTimeOffset(DateTime.Now.AddDays(-14)).ToUnixTimeMilliseconds();
        // firendIdsCSV will return a CSV of friends' ids
        string friendIdsCSV = String.Join(",", GetFriends(tokenString, 0));

        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT Posts.Id, Posts.OwnerId, Posts.Timestamp, Posts.Timeline, Posts.Content, Posts.ImageSrc,
                Users.FirstName as FirstName, Users.LastName as LastName, UserProfiles.Username as Username, UserProfiles.ImageSrc as OwnerImage FROM Posts
                INNER JOIN Users ON Posts.OwnerId=Users.Id
                INNER JOIN UserProfiles ON Posts.OwnerId=UserProfiles.OwnerId
                WHERE Timestamp>@Timestamp AND
                (
                    (Posts.OwnerId IN ({friendIdsCSV}) AND Posts.Timeline=0) OR
                    (Posts.OwnerId IN ({friendIdsCSV}) AND Posts.Timeline=@Timeline) OR
                    (Posts.OwnerId=@PostOwnerId) OR 
                    (Posts.OwnerId=@PostOwnerId AND Posts.Timeline=@Timeline)
                )
                ORDER BY Timestamp DESC
                OFFSET @Offset ROW
                FETCH NEXT 5 ROWS ONLY;";
                command.Parameters.AddWithValue("@Timestamp", postsAfterThisDate);
                command.Parameters.AddWithValue("@Timeline", CurrentUserId);
                command.Parameters.AddWithValue("@PostOwnerId", CurrentUserId);
                command.Parameters.AddWithValue("@Offset", (Page - 1) * 5);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    dynamic Post = new ExpandoObject();
                    // Id of post
                    Post.Id = Convert.ToInt32(reader["Id"]);
                    // Id of post owner
                    Post.OwnerId = Convert.ToInt32(reader["OwnerId"]);
                    Post.Timestamp = Convert.ToInt64(reader["Timestamp"]);
                    Post.Timeline = Convert.ToInt32(reader["Timeline"]);
                    Post.Content = Convert.ToString(reader["Content"]);
                    Post.ImageSrc = Convert.ToString(reader["ImageSrc"]);
                    Post.NumLikes = GetLikes(Convert.ToInt32(reader["Id"]));
                    Post.NumComments = GetComments(Convert.ToInt32(reader["Id"]));
                    Post.FirstName = Convert.ToString(reader["FirstName"]);
                    Post.LastName = Convert.ToString(reader["LastName"]);
                    Post.Username = Convert.ToString(reader["Username"]);
                    Post.OwnerImage = Convert.ToString(reader["OwnerImage"]);
                    Post.TargetFirstName = GetTargerUser(Convert.ToInt32(reader["Timeline"])).FirstName;
                    Post.TargetLastName = GetTargerUser(Convert.ToInt32(reader["Timeline"])).LastName;
                    Post.TargetUsername = GetTargerUser(Convert.ToInt32(reader["Timeline"])).Username;
                    NewsFeedPosts.Add(Post);
                }
            }
        }
        return Ok(NewsFeedPosts);
    }

    [HttpGet]
    [Route("/timelineposts/{ProfileOwnerUsername}/{page}")]
    public IActionResult TimelinePosts([FromHeader(Name = "AuthToken")] string tokenString, string ProfileOwnerUsername, int Page)
    {
        if (Authenticate.AuthenticateToken(tokenString) != "VALID")
        {
            return Ok("invalidtoken");
        }
        int ProfileOwnerId = 0;
        dynamic TimelinePosts = new List<ExpandoObject>();
        // int CurrentUserId = Int32.Parse(Authenticate.GetOwnerIdFromToken(tokenString));
        // AddDays(-14) parses posts within the last 2 weeks
        long postsAfterThisDate = new DateTimeOffset(DateTime.Now.AddDays(-14)).ToUnixTimeMilliseconds();
        // firendIdsCSV will return a CSV of friends' ids
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT * FROM UserProfiles WHERE Username=@Username;";
                command.Parameters.AddWithValue("@Username", ProfileOwnerUsername);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    ProfileOwnerId = reader.GetInt32(1);
                }
            }
        }
        string friendIdsCSV = String.Join(",", GetFriends("", ProfileOwnerId));

        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT Posts.Id, Posts.OwnerId, Posts.Timestamp, Posts.Timeline, Posts.Content, Posts.ImageSrc,
                Users.FirstName as FirstName, Users.LastName as LastName, UserProfiles.Username as Username, UserProfiles.ImageSrc as OwnerImage
                FROM Posts
                INNER JOIN Users ON Posts.OwnerId=Users.Id
                INNER JOIN UserProfiles ON Posts.OwnerId=UserProfiles.OwnerId
                WHERE Timestamp>@Timestamp AND
                (
                    (Posts.OwnerId IN ({friendIdsCSV}) AND Posts.Timeline=@Timeline) OR
                    (Posts.OwnerId=@OwnerId) OR 
                    (Posts.OwnerId=@OwnerId AND Posts.Timeline=@Timeline)
                )
                ORDER BY Timestamp DESC
                OFFSET @Offset ROW
                FETCH NEXT 10 ROWS ONLY;";
                command.Parameters.AddWithValue("@Timestamp", postsAfterThisDate);
                command.Parameters.AddWithValue("@Timeline", ProfileOwnerId);
                command.Parameters.AddWithValue("@OwnerId", ProfileOwnerId);
                command.Parameters.AddWithValue("@Offset", (Page - 1) * 10);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    dynamic Post = new ExpandoObject();
                    // Id of post
                    Post.Id = reader.GetInt32(0);
                    // Id of post owner
                    Post.OwnerId = Convert.ToInt32(reader["OwnerId"]);
                    Post.Timestamp = Convert.ToInt64(reader["Timestamp"]);
                    Post.Timeline = Convert.ToInt32(reader["Timeline"]);
                    Post.Content = Convert.ToString(reader["Content"]);
                    Post.ImageSrc = Convert.ToString(reader["ImageSrc"]);
                    Post.NumLikes = GetLikes(Convert.ToInt32(reader["Id"]));
                    Post.NumComments = GetComments(Convert.ToInt32(reader["Id"]));
                    Post.FirstName = Convert.ToString(reader["FirstName"]);
                    Post.LastName = Convert.ToString(reader["LastName"]);
                    Post.Username = Convert.ToString(reader["Username"]);
                    Post.OwnerImage = Convert.ToString(reader["OwnerImage"]);
                    Post.TargetFirstName = GetTargerUser(Convert.ToInt32(reader["Timeline"])).FirstName;
                    Post.TargetLastName = GetTargerUser(Convert.ToInt32(reader["Timeline"])).LastName;
                    Post.TargetUsername = GetTargerUser(Convert.ToInt32(reader["Timeline"])).Username;
                    TimelinePosts.Add(Post);
                }
            }
        }
        return Ok(TimelinePosts);
    }

    [HttpGet]
    [Route("/autoupdate/{Timestamp}")]
    public IActionResult AutoUpdate([FromHeader(Name = "AuthToken")] string tokenString, long Timestamp)
    {
        if (Authenticate.AuthenticateToken(tokenString) != "VALID")
        {
            return Ok("invalidtoken");
        }
        dynamic NewsFeedPosts = new List<ExpandoObject>();
        int CurrentUserId = Int32.Parse(Authenticate.GetOwnerIdFromToken(tokenString));
        // firendIdsCSV will return a CSV of friends' ids
        string friendIdsCSV = String.Join(",", GetFriends(tokenString, 0));

        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT Posts.Id, Posts.OwnerId, Posts.Timestamp, Posts.Timeline, Posts.Content, Posts.ImageSrc,
                Users.FirstName as FirstName, Users.LastName as LastName, UserProfiles.Username as Username, UserProfiles.ImageSrc as OwnerImage FROM Posts
                INNER JOIN Users ON Posts.OwnerId=Users.Id
                INNER JOIN UserProfiles ON Posts.OwnerId=UserProfiles.OwnerId
                WHERE Timestamp>@Timestamp AND
                (
                    (Posts.OwnerId IN ({friendIdsCSV}) AND Posts.Timeline=0) OR
                    (Posts.OwnerId IN ({friendIdsCSV}) AND Posts.Timeline=@Timeline) OR
                    (Posts.OwnerId=@PostOwnerId) OR 
                    (Posts.OwnerId=@PostOwnerId AND Posts.Timeline=@Timeline)
                )
                ORDER BY Timestamp DESC";
                command.Parameters.AddWithValue("@Timestamp", Timestamp);
                command.Parameters.AddWithValue("@Timeline", CurrentUserId);
                command.Parameters.AddWithValue("@PostOwnerId", CurrentUserId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                   dynamic Post = new ExpandoObject();
                    // Id of post
                    Post.Id = Convert.ToInt32(reader["Id"]);
                    // Id of post owner
                    Post.OwnerId = Convert.ToInt32(reader["OwnerId"]);
                    Post.Timestamp = Convert.ToInt64(reader["Timestamp"]);
                    Post.Timeline = Convert.ToInt32(reader["Timeline"]);
                    Post.Content = Convert.ToString(reader["Content"]);
                    Post.ImageSrc = Convert.ToString(reader["ImageSrc"]);
                    Post.NumLikes = GetLikes(Convert.ToInt32(reader["Id"]));
                    Post.NumComments = GetComments(Convert.ToInt32(reader["Id"]));
                    Post.FirstName = Convert.ToString(reader["FirstName"]);
                    Post.LastName = Convert.ToString(reader["LastName"]);
                    Post.Username = Convert.ToString(reader["Username"]);
                    Post.OwnerImage = Convert.ToString(reader["OwnerImage"]);
                    Post.TargetFirstName = GetTargerUser(Convert.ToInt32(reader["Timeline"])).FirstName;
                    Post.TargetLastName = GetTargerUser(Convert.ToInt32(reader["Timeline"])).LastName;
                    Post.TargetUsername = GetTargerUser(Convert.ToInt32(reader["Timeline"])).Username;
                    NewsFeedPosts.Add(Post);
                }
            }
        }
        return Ok(NewsFeedPosts);
    }

    private int GetLikes(int PostId)
    {
        int NumLikes = 0;
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT COUNT(Id) FROM Likes WHERE Target='Post' AND TargetId=@TargetId";
                command.Parameters.AddWithValue("@TargetId", PostId);
                NumLikes = (Int32)command.ExecuteScalar();
            }
        }
        return NumLikes;
    }

    private int GetComments(int PostId)
    {
        int NumComments = 0;
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT COUNT(Id) FROM Comments WHERE PostId=@PostId";
                command.Parameters.AddWithValue("@PostId", PostId);
                NumComments = (Int32)command.ExecuteScalar();
            }
        }
        return NumComments;
    }

    public TargetModel GetTargerUser(int TargetUserId)
    {
        TargetModel NameAndUsername = new TargetModel();
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT * FROM Users WHERE Id=@Id;";
                command.Parameters.AddWithValue("@Id", TargetUserId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    NameAndUsername.FirstName = reader.GetString(2);
                    NameAndUsername.LastName = reader.GetString(3);
                }
            }
        }
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT * FROM UserProfiles WHERE OwnerId=@OwnerId;";
                command.Parameters.AddWithValue("@OwnerId", TargetUserId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    NameAndUsername.Username = reader.GetString(2);
                }
            }
        }
        return NameAndUsername;
    }

    [HttpGet]
    [Route("/posts/{PostId}")]
    public IActionResult GetSinglePost(int PostId)
    {
        PostModel Post = new PostModel();
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT * FROM Posts WHERE Id=@Id";
                command.Parameters.AddWithValue("@Id", PostId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Post.Id = reader.GetInt32(0);
                    Post.OwnerId = reader.GetInt32(1);
                    Post.Timestamp = reader.GetInt64(2);
                    Post.Timeline = reader.GetInt32(3);
                    Post.Content = reader.GetString(4);
                    Post.ImageSrc = reader.GetString(5);
                    Post.NumLikes = GetLikes(reader.GetInt32(0));
                    Post.NumComments = GetComments(reader.GetInt32(0));
                }
            }
        }
        if (Post.Id == 0)
        {
            return Ok("doesnotexist");
        }
        return Ok(Post);
    }

    [HttpDelete]
    [Route("/posts/{PostId}")]
    public IActionResult DeletePost([FromHeader(Name = "AuthToken")] string tokenString, int PostId)
    {
        if (Authenticate.AuthenticateToken(tokenString) != "VALID")
        {
            return Ok("invalidtoken");
        }
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"DELETE FROM Posts WHERE Id=@Id;";
                command.Parameters.AddWithValue("@Id", PostId);
                command.ExecuteNonQuery();
            }
        }

        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"DELETE FROM Comments WHERE PostId=@PostId;";
                command.Parameters.AddWithValue("@PostId", PostId);
                command.ExecuteNonQuery();
            }
        }
        return Ok("deleted");
    }

    [HttpPatch]
    [Route("/posts/{PostId}")]
    public IActionResult PatchPost([FromHeader(Name = "AuthToken")] string tokenString, [FromBody] PostModel Post, int PostId)
    {
        if (Authenticate.AuthenticateToken(tokenString) != "VALID")
        {
            return Ok("invalidtoken");
        }
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"UPDATE Posts SET Content=@Content WHERE Id=@Id;";
                command.Parameters.AddWithValue("@Content", Post.Content);
                command.Parameters.AddWithValue("@Id", PostId);
                command.ExecuteNonQuery();
            }
            if (Post.ImageSrc != "")
            {
                using (var command = db.CreateCommand())
                {
                    command.CommandText = $@"UPDATE Posts SET ImageSrc=@ImageSrc WHERE Id=@Id;";
                    command.Parameters.AddWithValue("@ImageSrc", Post.ImageSrc);
                    command.Parameters.AddWithValue("@Id", PostId);
                    command.ExecuteNonQuery();
                }
            }
        }
        return Ok("updated");
    }

    public static List<int> GetFriends(string tokenString, int ProfileUserId)
    {
        int CurrentUserId = 0;
        if (tokenString != "")
        {
            CurrentUserId = Int32.Parse(Authenticate.GetOwnerIdFromToken(tokenString));
        }
        if (ProfileUserId != 0)
        {
            CurrentUserId = ProfileUserId;
        }

        List<int> FriendIds = new List<int>();
        FriendIds.Add(0);
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT * FROM Friends WHERE OwnerId=@OwnerId;";
                command.Parameters.AddWithValue("@OwnerId", CurrentUserId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    FriendIds.Add(reader.GetInt32(2));
                }
            }
        }
        return FriendIds;
    }
}