public class LikeModel
{
    public int Id { get; set; }
    public int TargetId { get; set; } // If photo: photoId, if post: postId
    public int OwnerId { get; set; }
    public string Target { get; set; } = ""; // Photo or Post
    public int TargetUserId { get; set; } // The one who will receive the notif
    public int AlbumId { get; set; } // For photo
    public string Username { get; set;} = ""; // For photo
}