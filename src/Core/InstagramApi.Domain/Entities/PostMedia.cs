using InstagramApi.Domain.Common;

namespace InstagramApi.Domain.Entities;

public class PostMedia : BaseEntity
{
    public Guid PostId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string MediaType { get; set; } = "image"; // image, video
    public int Order { get; set; } = 0;
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? FileSizeBytes { get; set; }
    public double? Duration { get; set; } // for videos

    public virtual Post Post { get; set; } = null!;
}
