public class FriendRequestModel
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public int TargetUserId { get; set; }
    public long Timestamp { get; set; }
    public string Status { get; set; } = "";
}