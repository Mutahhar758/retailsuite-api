using System.ComponentModel;

namespace Retailer.Domain.Common.Enums;

public enum FileType
{
    [Description(".jpg,.jpeg,.png,.gif,.bmp,.tiff,.webp,.heic,.heif,.ico,.svg")]
    Image,
    [Description(".pdf,.docx,.xlsx,.txt,.csv")]
    Document,
    [Description(".mp4,.avi,.mov,.wmv,.flv,.mkv,.webm,.3gp,.3g2,.ogg,.ts,.m4v")]
    Video
}