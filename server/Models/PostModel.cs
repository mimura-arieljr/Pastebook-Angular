public class PostModel
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public long Timestamp { get; set; }
    public int Timeline { get; set; }
    public string Content { get; set; } = "";
    public string ImageSrc { get; set; } = "";
    public int NumLikes { get; set; }
    public int NumComments { get; set; }
}