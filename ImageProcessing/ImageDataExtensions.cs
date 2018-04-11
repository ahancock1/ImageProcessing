// -----------------------------------------------------------------------
//   Copyright (C) 2017 Adam Hancock
//    
//   ImageDataExtensions.cs can not be copied and/or distributed without the express
//   permission of Adam Hancock
// -----------------------------------------------------------------------

namespace ImageProcessing
{
    using System.Threading.Tasks;
    using Types;

    public static class ImageDataExtensions
    {
        public static ImageData Crop(this ImageData image, int x, int y, int width, int height)
        {
            var result = new ImageData(width, height, image.Format)
            {
                DpiX = image.DpiX,
                DpiY = image.DpiY
            };

            Parallel.For(x, x + width, px =>
            {
                for (var py = y; py < y + height; py++)
                {
                    for (var e = 0; e < image.Elements; e++)
                    {
                        result[e][px - x, py - y] = image[e][px, py];
                    }
                }
            });

            return result;
        }


        public static float[,] Overlay(this float[,] image, float[,] overlay)
        {
            var width = image.GetLength(0);
            var height = image.GetLength(1);

            var result = new float[width, height];

            Parallel.For(0, width, x =>
            {
                for (var y = 0; y < height; y++)
                {
                    var value = (image[x, y] + overlay[x, y]) / 2;
                    result[x, y] = value;
                }
            });

            return result;
        }

        public static float[,] Subtract(this float[,] image, float[,] overlay)
        {
            var width = image.GetLength(0);
            var height = image.GetLength(1);

            var result = new float[width, height];

            Parallel.For(0, width, x =>
            {
                for (var y = 0; y < height; y++)
                {
                    var value = image[x, y] - overlay[x, y];
                    result[x, y] = value;
                }
            });

            return result;
        }

        public static ImageData Clone(this ImageData image)
        {
            var result = new ImageData(image.Width, image.Height, image.Format)
            {
                Palette = image.Palette,
                DpiX = image.DpiX,
                DpiY = image.DpiY
            };

            Parallel.For(0, image.Width, x =>
            {
                for (var y = 0; y < image.Height; y++)
                {
                    for (var e = 0; e < image.Elements; e++)
                    {
                        result[e][x, y] = image[e][x, y];
                    }
                }
            });

            return result;
        }
    }
}