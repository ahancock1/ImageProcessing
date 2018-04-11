// -----------------------------------------------------------------------
//   Copyright (C) 2017 Adam Hancock
//    
//   ImageData.cs can not be copied and/or distributed without the express
//   permission of Adam Hancock
// -----------------------------------------------------------------------

namespace ImageProcessing.Types
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    public class ImageData
    {
        private Matrix[] _data;

        public ImageData(int width, int height, PixelFormat format, BitmapPalette palette = null)
        {
            Initialise(width, height, format, palette);
        }

        public ImageData(string filename) : this(new FileInfo(filename))
        {
        }

        public ImageData(FileInfo file)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(file.FullName);
            image.EndInit();

            Initialise(image);
        }

        public ImageData(BitmapSource image)
        {
            Initialise(image);
        }

        public BitmapSource Image => BitmapSource.Create(Width, Height, DpiX, DpiY,
            Format, null, Pack(_data), Stride);

        public BitmapPalette Palette { get; set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public double DpiX { get; set; }

        public double DpiY { get; set; }

        public PixelFormat Format { get; private set; }

        public int Elements { get; private set; }

        public int Stride { get; private set; }

        public int Depth { get; private set; }

        public Matrix this[int element]
        {
            get => _data[element];
            set => _data[element] = value;
        }
        
        private void Initialise(BitmapSource image)
        {
            Width = image.PixelWidth;
            Height = image.PixelHeight;
            Stride = Width * ((image.Format.BitsPerPixel + 1) / 8);
            DpiX = image.DpiX;
            DpiY = image.DpiY;
            Format = image.Format;
            Palette = image.Palette;
            Elements = Format.BitsPerPixel / 8 + (Format.BitsPerPixel % 8 > 0 ? 1 : 0);
            Depth = Elements * 8;

            var bytes = new byte[Stride * Height];
            image.CopyPixels(bytes, Stride, 0);
            _data = Unpack(bytes);
        }

        private void Initialise(int width, int height, PixelFormat format, BitmapPalette palette = null,
            double dpiX = 72, double dpiY = 72, Matrix[] data = null)
        {
            Width = width;
            Height = height;
            Stride = Width * ((format.BitsPerPixel + 1) / 8);
            DpiX = dpiX;
            DpiY = dpiY;
            Format = format;
            Palette = palette;
            Elements = Format.BitsPerPixel / 8 + (Format.BitsPerPixel % 8 > 0 ? 1 : 0);
            Depth = Elements * 8;
            _data = data;
            
            if (data == null)
            {
                _data = new Matrix[Elements];
                for (var i = 0; i < Elements; i++)
                {
                    _data[i] = new Matrix(width, height);
                }
            }
        }

        private Matrix[] Unpack(byte[] bytes)
        {
            var data = Array.ConvertAll(bytes, b => (float)b);

            var pixels = new Matrix[Elements];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Matrix(Width, Height);
            }

            Parallel.For(0, Height, y =>
            {
                for (var x = 0; x < Width; x++)
                {
                    for (var e = 0; e < Elements; e++)
                    {
                        pixels[e][x, y] = data[y * Stride + x * Elements + e];
                    }
                }
            });
            return pixels;
        }

        private byte[] Pack(Matrix[] data)
        {
            var pixels = new byte[Stride * Height];

            Parallel.For(0, Height, y =>
            {
                for (var x = 0; x < Width; x++)
                {
                    for (var e = 0; e < Elements; e++)
                    {
                        var value = data[e][x, y];
                        var pixel = (byte)(value > byte.MaxValue ? byte.MaxValue : value < 0 ? 0 : value);
                        pixels[y * Stride + x * Elements + e] = pixel;
                    }
                }
            });
            return pixels;
        }

        public void Save<TEncoder>(string filepath)
            where TEncoder : BitmapEncoder, new()
        {
            var encoder = new TEncoder();
            encoder.Frames.Add(BitmapFrame.Create(Image));

            var extension = EncoderExtension(encoder);
            if (!filepath.EndsWith(extension))
            {
                filepath = $"{filepath}{extension}";
            }

            using (var stream = new FileStream(filepath, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }

        private string EncoderExtension(BitmapEncoder encoder)
        {
            switch (encoder)
            {
                case BmpBitmapEncoder _:
                    return ".bmp";
                case GifBitmapEncoder _:
                    return ".gif";
                case JpegBitmapEncoder _:
                    return ".jpg";
                case PngBitmapEncoder _:
                    return ".png";
                case TiffBitmapEncoder _:
                    return ".tif";
                case WmpBitmapEncoder _:
                    return ".wmp";
                default:
                    return string.Empty;
            }
        }
    }
}