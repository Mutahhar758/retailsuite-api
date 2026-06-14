using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Retailer.Shared.Common.Constants;

public static class DocumentConstants
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".bmp", "image/bmp" },
        { ".tiff", "image/tiff" },
        { ".webp", "image/webp" },
        { ".heic", "image/heic" },
        { ".heif", "image/heif" },
        { ".ico", "image/x-icon" },
        { ".svg", "image/svg+xml" },

        // Videos
        { ".mp4", "video/mp4" },
        { ".avi", "video/x-msvideo" },
        { ".mov", "video/quicktime" },
        { ".wmv", "video/x-ms-wmv" },
        { ".flv", "video/x-flv" },
        { ".mkv", "video/x-matroska" },
        { ".webm", "video/webm" },
        { ".3gp", "video/3gpp" },
        { ".3g2", "video/3gpp2" },
        { ".ogg", "video/ogg" },
        { ".ts", "video/MP2T" },
        { ".m4v", "video/x-m4v" },

        // Audio
        { ".m4a", "audio/mp4" },

        // Documents
        { ".pdf", "application/pdf" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".txt", "text/plain" },
        { ".csv", "text/csv" },
    };

    public static string GetContentType(string extension)
    {
        return ContentTypes.TryGetValue(extension, out string? contentType) ? contentType : "application/octet-stream";
    }
}
