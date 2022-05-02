using Microsoft.AspNetCore.Mvc;

public class AlbumController : Controller
{
    [HttpPost]
    [Route("/create-album")]
    public IActionResult CreateAlbum([FromHeader(Name = "AuthToken")] string token, [FromBody] AlbumModel model)
    {
        // ownerId here is the current user
        var ownerId = Int32.Parse(Authenticate.GetOwnerIdFromToken(token));
        var timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
        int latestAlbumId = 0;
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = 
                    @"INSERT INTO Albums (OwnerId, AlbumName, Timestamp)
                    VALUES (@OwnerId, @AlbumName, @Timestamp);";
                command.Parameters.AddWithValue("@OwnerId", ownerId);
                command.Parameters.AddWithValue("@AlbumName", model.AlbumName);
                command.Parameters.AddWithValue("@Timestamp", timestamp);
                command.ExecuteNonQuery();
            }
        }
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = "SELECT TOP 1 * FROM Albums ORDER BY Id DESC;";
                var reader = command.ExecuteReader();
                while(reader.Read())
                {
                    latestAlbumId = reader.GetInt32(0);
                }
            }
        }
        return Ok(latestAlbumId);
    }

    [HttpGet]
    [Route("/albums")]
    public IActionResult GetAllAlbums([FromHeader(Name = "OwnerId")] int ownerId)
    {
        var albums = new List<AlbumModel>();
        var thumbnail = "";
        var count = 0;
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = "SELECT * FROM Albums WHERE OwnerId=@OwnerId";
                command.Parameters.AddWithValue("@OwnerId", ownerId);
                var reader = command.ExecuteReader();
                while(reader.Read())
                {
                    thumbnail = GetThumbnail(reader.GetInt32(0));
                    count = GetNumberOfPhotos(reader.GetInt32(0));
                    albums.Add(new AlbumModel {
                        Id = reader.GetInt32(0), 
                        OwnerId = reader.GetInt32(1),
                        AlbumName = reader.GetString(2),
                        Timestamp = reader.GetInt64(3),
                        Thumbnail = thumbnail,
                        NumPhotos = count
                    });
                }
            }
        }
        return Json(albums);
    }
    
    [HttpGet]
    [Route("/albums/{albumId}")]
    public IActionResult GetAlbum(int albumId)
    {
        AlbumModel model = new AlbumModel();
        int count = 0;
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = "SELECT * FROM Albums WHERE Id=@Id;";
                command.Parameters.AddWithValue("@Id", albumId);
                var reader = command.ExecuteReader();
                while(reader.Read())
                {
                    count = GetNumberOfPhotos(reader.GetInt32(0));
                    model.Id = reader.GetInt32(0);
                    model.AlbumName = reader.GetString(2);
                    model.NumPhotos = count;
                }
            }
        }
        return Json(model);
    }

    [HttpPatch]
    [Route("/albums/{albumId}")]
    public IActionResult EditAlbumName(int albumId, [FromBody] AlbumModel model)
    {
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = "UPDATE Albums SET AlbumName=@AlbumName WHERE Id=@Id;";
                command.Parameters.AddWithValue("@Id", albumId);
                command.Parameters.AddWithValue("@AlbumName", model.AlbumName);
                command.ExecuteNonQuery();
            }
        }
        return Ok();
    }

    [HttpDelete]
    [Route("/albums/{albumId}")]
    public IActionResult DeleteAlbum(int albumId)
    {
        AlbumModel album = new AlbumModel();
        PhotoModel photo = new PhotoModel();
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = "DELETE FROM Albums WHERE Id=@Id;";
                command.Parameters.AddWithValue("@Id", albumId);
                var reader = command.ExecuteReader();
                while(reader.Read())
                {
                    album.OwnerId = reader.GetInt32(1);
                    album.AlbumName = reader.GetString(2);
                    album.Timestamp = reader.GetInt64(3);
                }
            }
        }
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = "DELETE FROM Photos WHERE AlbumId=@AlbumId;";
                command.Parameters.AddWithValue("@AlbumId", albumId);
                var reader = command.ExecuteReader();
                while(reader.Read())
                {
                    photo.AlbumId =  reader.GetInt32(1);
                    photo.Timestamp = reader.GetInt64(2);
                    photo.ImageSrc = reader.GetString(3);
                    photo.NumLikes = reader.GetInt32(4);
                }
            }
        }
        return Ok();
    }

    public static string GetThumbnail(int albumId)
    {
        var thumbnail = "";
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = "SELECT TOP 1 * FROM Photos WHERE AlbumId=@AlbumId ORDER BY Id ASC;";
                command.Parameters.AddWithValue("@AlbumId", albumId);
                var reader = command.ExecuteReader();
                while(reader.Read())
                {
                    thumbnail = reader.GetString(3);
                }
            }
        }
        return thumbnail;
    }
    
    public static int GetNumberOfPhotos(int albumId)
    {
        var count = 0;
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = "SELECT COUNT (Id) FROM Photos WHERE AlbumId=@AlbumId;";
                command.Parameters.AddWithValue("@AlbumId", albumId);
                count = (Int32)command.ExecuteScalar();
            }
        }
        return count;
    }
}