public class UserModel
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string EmailAddress { get; set; } = "";
    public long Birthday { get; set; } = 0;
    public string Gender { get; set; } = "";
    public string MobileNumber { get; set; } = "";

    public string CurrentPassword { get; set; } = "";
}