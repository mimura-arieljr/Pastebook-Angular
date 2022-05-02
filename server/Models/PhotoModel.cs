public class PhotoModel
{
    public int Id { get; set; }
    public int AlbumId { get; set; }
    public long Timestamp { get; set; }
    public string ImageSrc { get; set; } = "";
    public int NumLikes { get; set; }
}