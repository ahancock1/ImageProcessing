// -----------------------------------------------------------------------
//  <copyright file="Program.cs" company="Solentim">
//      Copyright (c) Solentim 2018. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

namespace ImageProcessing.Console
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Types;

    internal class Program
    {
        private static readonly string _path = @"C:\imageprocessing\";

        private static void Main(string[] args)
        {
            // Clear directory
            foreach (var file in Directory.GetFiles(_path))
            {
                if (file.ToLower().Contains("base") ||
                    file.ToLower().Contains("illumination") ||
                    file.ToLower().Contains("input") ||
                    file.ToLower().Contains("green") ||
                    file.ToLower().Contains("copy"))
                {
                    continue;
                }

                File.Delete(file);
            }

            var input = new ImageData(_path + "base_5.tif");
            var illumination = new ImageData(_path + "green.tif");

            var timer = Stopwatch.StartNew();

            //var c = c1 / 2;
            //var c2 = ImageProcessing.AutoContrast(c1 - c, input.Depth);

            var contrast = ImageProcessing.AutoContrast(input[0], input.Depth);
            var phansalkar = ImageProcessing.Phansalkar(contrast, 15);
            var closed = ImageProcessing.Morphology.Close(phansalkar, 6);

            Console.WriteLine($"Completed: {timer.Elapsed.TotalMilliseconds}");

            SaveImage(phansalkar, "phansalkar");

            Console.ReadKey();

            return;
        }

        private static void SaveImage(Matrix input, string name)
        {
            var image = new ImageData(input.Width, input.Height, PixelFormats.Gray8)
            {
                [0] = input
            };
            image.Save<JpegBitmapEncoder>($"{_path}{name}.jpg");
        }
    }
}