using Microsoft.AspNetCore.Mvc;

public class ConfigureController : Controller
{
    private string? CorrectAdminAPI = Environment.GetEnvironmentVariable("ADMIN_API_KEY");
    // Set ADMIN_API_KEY = mysecretapi
    [HttpPost]
    [Route("/configure")]
    public IActionResult PostConfigure([FromHeader(Name = "X-AdminAPI")] string AdminAPI, [FromBody] ConfigureModel Configuration)
    {
        // Must check if null first!
        // Assumes "pastebookdb" database is already created
        if (AdminAPI == null || !AdminAPI.Equals(CorrectAdminAPI))
        {
            return Unauthorized();
        }

        // If "Action" property was not supplied
        if (Configuration.Action == null)
        {
            return Ok("Invalid configuration.");
        }

        // Create All Tables
        // On the body, {"Action": "CreateTables"}
        if (Configuration.Action.Equals("CreateTables"))
        {
            using (var db = Database.OpenDatabase())
            {
                using (var command = db.CreateCommand())
                {
                    command.CommandText = $@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' and xtype='U')
                        CREATE TABLE Users (
                            Id INT NOT NULL IDENTITY PRIMARY KEY,
                            Password VARCHAR(255),
                            FirstName VARCHAR(255),
                            LastName VARCHAR(255),
                            EmailAddress VARCHAR(255) NOT NULL,
                            Birthday BIGINT,
                            Gender VARCHAR(255),
                            MobileNumber VARCHAR(255)
                        );";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserProfiles' and xtype='U')
                        CREATE TABLE UserProfiles (
                            Id INT NOT NULL IDENTITY PRIMARY KEY,
                            OwnerId INT,
                            Username VARCHAR(255),
                            Bio VARCHAR(2000),
                            ImageSrc VARCHAR(MAX)
                        );";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Activities' and xtype='U')
                        CREATE TABLE Activities (
                            Id INT NOT NULL IDENTITY PRIMARY KEY,
                            OwnerId INT,
                            Action VARCHAR(255),
                            TargetId INT,
                            Timestamp BIGINT,
                            State VARCHAR(255),
                            Link VARCHAR(255)
                        );";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Posts' and xtype='U')
                        CREATE TABLE Posts (
                            Id INT NOT NULL IDENTITY PRIMARY KEY,
                            OwnerId INT,
                            Timestamp BIGINT,
                            Timeline INT,
                            Content VARCHAR(1000),
                            ImageSrc VARCHAR(MAX),
                        );";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Comments' and xtype='U')
                        CREATE TABLE Comments (
                            Id INT NOT NULL IDENTITY PRIMARY KEY,
                            OwnerId INT,
                            PostId INT,
                            Timestamp BIGINT,
                            Content VARCHAR(1000)
                        );";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Friends' and xtype='U')
                        CREATE TABLE Friends (
                            Id INT NOT NULL IDENTITY PRIMARY KEY,
                            OwnerId INT,
                            FriendId INT,
                        );";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Albums' and xtype='U')
                        CREATE TABLE Albums (
                            Id INT NOT NULL IDENTITY PRIMARY KEY,
                            OwnerId INT,
                            AlbumName VARCHAR(255),
                            Timestamp BIGINT
                        );";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Photos' and xtype='U')
                        CREATE TABLE Photos (
                            Id INT NOT NULL IDENTITY PRIMARY KEY,
                            AlbumId INT,
                            Timestamp BIGINT,
                            ImageSrc VARCHAR(MAX),
                        );";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Likes' and xtype='U')
                        CREATE TABLE Likes (
                            Id INT NOT NULL IDENTITY PRIMARY KEY,
                            OwnerId INT,
                            Target VARCHAR(255),
                            TargetId INT
                        );";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='FriendRequests' and xtype='U')
                        CREATE TABLE FriendRequests (
                            Id INT NOT NULL IDENTITY PRIMARY KEY,
                            OwnerId INT,
                            TargetUserId INT,
                            Timestamp BIGINT,
                            Status VARCHAR(255)
                        );";
                    command.ExecuteNonQuery();

                }
            }
            return Ok("Tables created.");
        }

        // Drop Single Table
        // On the body, {"Action": "DropTable", "Table":"Templates"}
        else if (Configuration.Action.Equals("DropTable"))
        {
            using (var db = Database.OpenDatabase())
            {
                using (var command = db.CreateCommand())
                {
                    command.CommandText = $@"DROP TABLE {Configuration.Table};";
                    command.ExecuteNonQuery();
                }
            }
            return Ok("Table dropped.");
        }

        // Drop All Tables
        // On the body, {"Action": "DropTables"}
        else if (Configuration.Action.Equals("DropAllTables"))
        {
            using (var db = Database.OpenDatabase())
            {
                using (var command = db.CreateCommand())
                {
                    command.CommandText = $@"DROP TABLE IF EXISTS UserProfiles;";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"DROP TABLE IF EXISTS Activities;";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"DROP TABLE IF EXISTS Comments;";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"DROP TABLE IF EXISTS Friends;";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"DROP TABLE IF EXISTS Photos;";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"DROP TABLE IF EXISTS Likes;";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"DROP TABLE IF EXISTS FriendRequests;";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"DROP TABLE IF EXISTS Posts;";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"DROP TABLE IF EXISTS Albums;";
                    command.ExecuteNonQuery();
                    command.CommandText = $@"DROP TABLE IF EXISTS Users;";
                    command.ExecuteNonQuery();
                }
            }
            return Ok("All tables dropped.");
        }

        else
        {
            return Ok("Invalid configuration.");
        }
    }
}