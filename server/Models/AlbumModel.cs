public class AlbumModel
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string AlbumName { get; set; } = "";
    public long Timestamp { get; set; }
    public string Thumbnail { get; set; } = "";
    public int NumPhotos { get; set; }
}