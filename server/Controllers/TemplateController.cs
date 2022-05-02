using Microsoft.AspNetCore.Mvc;

public class TemplateController : Controller
{
    private string? CorrectAdminAPI = Environment.GetEnvironmentVariable("ADMIN_API_KEY");
    // ADMIN ONLY = requires ADMIN_API_KEY="mysecretapi"

    [HttpGet]
    [Route("/template/selectall")]
    public IActionResult GetTemplate()
    {
        List<TemplateModel> TemplateList = new List<TemplateModel>();
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT * FROM Templates";
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    TemplateModel Template = new TemplateModel();
                    Template.Id = reader.GetInt32(0);
                    Template.Name = reader.GetString(1);
                    TemplateList.Add(Template);
                }
            }
        }
        return Ok(TemplateList);
    }

    [HttpGet]
    [Route("/template/selectcolumns")]
    public IActionResult GetTemplateWithCondition([FromBody] TemplateModel Template)
    {
        List<TemplateModel> ReturnedTemplate = new List<TemplateModel>();
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"SELECT * FROM Templates WHERE Id=@Id AND Name=@Name";
                command.Parameters.AddWithValue("@Id", Template.Id);
                command.Parameters.AddWithValue("@Name", Template.Name);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    TemplateModel TemplateData = new TemplateModel();
                    TemplateData.Id = reader.GetInt32(0);
                    TemplateData.Name = reader.GetString(1);
                    ReturnedTemplate.Add(TemplateData);
                }
            }
        }
        return Ok(ReturnedTemplate);
    }

    [HttpPost]
    [Route("/template/post")]
    public IActionResult PostTemplate([FromBody] TemplateModel Template)
    {
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"INSERT INTO Templates (Id, Name) VALUES (@Id, @Name);";
                command.Parameters.AddWithValue("@Guest", Template.Id);
                command.Parameters.AddWithValue("@Staff", Template.Name);
                command.ExecuteNonQuery();
            }
        }
        return Ok("Template added.");
    }

    [HttpDelete]
    [Route("/templates/{Id}")]
    public IActionResult DeleteTemplateById(int Id)
    {
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"DELETE FROM Templates WHERE Id=@Id";
                command.Parameters.AddWithValue("@Id", Id);
                command.ExecuteNonQuery();
            }
        }
        return Ok("Template deleted.");
    }

    [HttpDelete]
    [Route("/template/delete")]
    public IActionResult DeleteAllTemplates([FromHeader(Name = "X-AdminAPI")] string AdminAPI)
    {
        if (AdminAPI == null || !AdminAPI.Equals(CorrectAdminAPI))
        {
            return Unauthorized();
        }
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = $@"DELETE FROM Tempaltes";
                command.ExecuteNonQuery();
            }
        }
        return Ok("Templates deleted.");
    }

    [HttpPut]
    [Route("/bookings/{Id}")]
    public IActionResult PatchTemplate(int Id, [FromBody] TemplateModel Template)
    {
        using (var db = Database.OpenDatabase())
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = @"UPDATE Bookings SET Name=@Name WHERE Id=@Id;";
                command.Parameters.AddWithValue("@Id", Id);
                command.Parameters.AddWithValue("@Name", Template.Name);
                command.ExecuteNonQuery();
            }
        }
        return Ok("Template updated.");
    }
}