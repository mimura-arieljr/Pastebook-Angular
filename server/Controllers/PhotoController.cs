using Microsoft.AspNetCore.Mvc;
public class PhotoController : Controller
{
    [HttpPost]
    [Route("/add-photo")]
    public IActionResult AddPhoto([FromBody] PhotoModel model)
    {
        var timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = 
                    @"INSERT INTO Photos (AlbumId, Timestamp, ImageSrc)
                    VALUES (@AlbumId, @Timestamp, @ImageSrc);";
                command.Parameters.AddWithValue("@AlbumId", model.AlbumId);
                command.Parameters.AddWithValue("@Timestamp", timestamp);
                command.Parameters.AddWithValue("@ImageSrc", model.ImageSrc);
                command.ExecuteNonQuery();
            }
        }
        PhotoModel photo = new PhotoModel();
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = "SELECT TOP 1 * FROM Photos ORDER BY Id DESC;";
                var reader = command.ExecuteReader();
                while(reader.Read())
                {
                    photo.Id = reader.GetInt32(0);
                    photo.AlbumId =  reader.GetInt32(1);
                    photo.Timestamp = reader.GetInt64(2);
                    photo.ImageSrc = reader.GetString(3);
                }
            }
        }
        return Json(photo);
    }

    [HttpGet]
    [Route("/photos")]
    public IActionResult GetAllPhotos([FromHeader(Name = "AlbumId")] int albumId)
    {
        var photos = new List<PhotoModel>();
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = "SELECT Id, ImageSrc FROM Photos WHERE AlbumId=@AlbumId;";
                command.Parameters.AddWithValue("@AlbumId", albumId);
                var reader = command.ExecuteReader();
                while(reader.Read())
                {
                    photos.Add(new PhotoModel {
                        Id = reader.GetInt32(0),
                        ImageSrc = reader.GetString(1)
                    });
                }
            }
        }
        return Json(photos);
    }
    
    [HttpGet]
    [Route("/photos/{photoId}")]
    public IActionResult GetPhoto(int photoId)
    {
        PhotoModel photo = new PhotoModel();
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = "SELECT * FROM Photos WHERE Id=@Id;";
                command.Parameters.AddWithValue("@Id", photoId);
                var reader = command.ExecuteReader();
                while(reader.Read())
                {
                    photo.Id = reader.GetInt32(0);
                    photo.AlbumId =  reader.GetInt32(1);
                    photo.Timestamp = reader.GetInt64(2);
                    photo.ImageSrc = reader.GetString(3);
                    photo.NumLikes = GetNumLikes(reader.GetInt32(0));
                }
            }
        };
        return Json(photo);
    }

    public static int GetNumLikes(int id)
    {
        int count = 0;
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = "SELECT COUNT (Id) From Likes WHERE TargetId=@TargetId AND Target='Photo';";
                command.Parameters.AddWithValue("@TargetId", id);
                count = (Int32)command.ExecuteScalar();
            }
        }
        return count;
    }

    [HttpDelete]
    [Route("/photos/{photoId}")]
    public IActionResult DeletePhoto(int photoId)
    {
        PhotoModel photo = new PhotoModel();
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = "DELETE FROM Photos WHERE Id=@Id;";
                command.Parameters.AddWithValue("@Id", photoId);
                var reader = command.ExecuteReader();
                while(reader.Read())
                {
                    photo.AlbumId =  reader.GetInt32(1);
                    photo.Timestamp = reader.GetInt64(2);
                    photo.ImageSrc = reader.GetString(3);
                }
            }
        }
        return Ok();
    }
}