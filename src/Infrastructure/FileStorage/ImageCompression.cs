using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Encoder = System.Drawing.Imaging.Encoder;

namespace Retailer.Infrastructure.FileStorage;
public class ImageCompression
{
#pragma warning disable CA1416 // Validate platform compatibility
    public static MemoryStream ResizeImage(Stream fileStream, int height, int width)
    {
        var img = Image.FromStream(fileStream);

        float aspectRatio = (float)img.Width / img.Height;
        int proportionalWidth = width;
        int proportionalHeight = (int)(width / aspectRatio);

        if (proportionalHeight > height)
        {
            proportionalHeight = height;
            proportionalWidth = (int)(height * aspectRatio);
        }

        Bitmap resizedImage = new Bitmap(img, new Size(proportionalWidth, proportionalHeight));
        using var stream = new MemoryStream();
        resizedImage.Save(stream, ImageFormat.Jpeg);
        byte[] bytes = stream.ToArray();

        return new MemoryStream(bytes);
    }

}
