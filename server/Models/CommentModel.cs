public class CommentModel 
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public int PostId { get; set; }
    public int TargetUserId { get; set; }
    public long Timestamp { get; set; }
    public string? Content { get; set; } = "";
    public string? FirstName { get; set; } = "";
    public string? LastName { get; set; } = "";
    public string? Username { get; set; } = "";
    public string? ImageSrc { get; set; } = "";

}