using Microsoft.AspNetCore.Mvc;

public class SearchController : Controller
{

    [HttpGet]
    [Route("/search")]
    public IActionResult GetAllUsersName()
    {
        var SearchNames = new List<SearchModel>();
        using (var db = Database.OpenDatabase())
        {
            using ( var command = db.CreateCommand()) {
                command.CommandText = $@"SELECT Users.FirstName, Users.LastName, UserProfiles.Username, UserProfiles.ImageSrc 
                    FROM Users JOIN UserProfiles 
                    ON Users.Id = UserProfiles.OwnerId;";
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    SearchNames.Add(new SearchModel {
                        FirstName = reader.GetString(0),
                        LastName = reader.GetString(1),
                        Username = reader.GetString(2),
                        ImageSrc = reader.GetString(3)
                    });
                    
                }
            }
        }
        return Ok(SearchNames);
    }
}