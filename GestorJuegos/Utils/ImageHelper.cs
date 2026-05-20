using System;
using System.IO;
using SkiaSharp;

namespace GestorJuegos.Utils
{
    public static class ImageHelper
    {
        public static byte[]? GenerateThumbnail(byte[]? imageData, int maxWidth = 200, int maxHeight = 300)
        {
            if (imageData == null || imageData.Length == 0) return null;

            try
            {
                using var input = new MemoryStream(imageData);
                using var bitmap = SKBitmap.Decode(input);
                if (bitmap == null) return null;

                int width = bitmap.Width;
                int height = bitmap.Height;

                float ratio = Math.Min((float)maxWidth / width, (float)maxHeight / height);
                if (ratio >= 1) return imageData; // Ya es pequeña

                int newWidth = (int)(width * ratio);
                int newHeight = (int)(height * ratio);

                using var resized = new SKBitmap(newWidth, newHeight);
                bitmap.ScalePixels(resized, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
                
                using var image = SKImage.FromBitmap(resized);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 80);
                return data.ToArray();
            }
            catch
            {
                return null;
            }
        }
    }
}
