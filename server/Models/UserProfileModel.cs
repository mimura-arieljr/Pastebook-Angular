public class UserProfileModel : UserModel
{
    public string Status { get; set; } = "";
    public int OwnerId { get; set; }
    public string Username { get; set; } = "";
    public string Bio { get; set; } = "";
    public string ImageSrc { get; set; } = "";
}