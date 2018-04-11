// -----------------------------------------------------------------------
//   Copyright (C) 2017 Adam Hancock
//    
//   ImageProcessing.cs can not be copied and/or distributed without the express
//   permission of Adam Hancock
// -----------------------------------------------------------------------

namespace ImageProcessing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Alea;
    using Alea.CSharp;
    using Types;

    public struct Sobel
    {
        public float[,] Magnitude { get; set; }
        public float[,] Orientation { get; set; }
        public float[,] Horizontal { get; set; }
        public float[,] Vertical { get; set; }

        public Sobel(int width, int height)
        {
            Magnitude = new float[width, height];
            Orientation = new float[width, height];
            Horizontal = new float[width, height];
            Vertical = new float[width, height];
        }
    }

    public static class ImageProcessing
    {
        public static Matrix Convolve(Matrix input, Matrix kernel)
        {
            var width = input.Width;
            var height = input.Height;

            var halfKernelWidth = kernel.Width >> 1;
            var halfKernelHeight = kernel.Height >> 1;

            var result = new Matrix(width, height);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var sum = 0f;
                    for (var kx = -halfKernelWidth; kx <= halfKernelWidth; kx++)
                    {
                        for (var ky = -halfKernelHeight; ky <= halfKernelHeight; ky++)
                        {
                            if (x + kx >= 0 && x + kx < width && y + ky >= 0 && y + ky < height)
                            {
                                sum += input[x + kx, y + ky] * kernel[kx + halfKernelWidth, ky + halfKernelHeight];
                            }
                        }
                    }

                    result[x, y] = sum;
                }
            }

            return result;
        }

        public static Matrix Scale(float[,] input, float min, float max)
        {
            var data = input.Cast<float>().ToArray();
            var inputMin = data.Min();
            var inputMax = data.Max();
            var width = input.GetLength(0);
            var height = input.GetLength(1);

            var result = new float[width, height];

            Parallel.For(0, width, x =>
            {
                for (var y = 0; y < height; y++)
                {
                    var value = (max - min) * (input[x, y] - inputMin) / (inputMax - inputMin) + min;
                    result[x, y] = value;
                }
            });

            return result;
        }
        
        private static Action KernelBuilder(Action<int> a, int height) => () =>
        {
            var tid = blockIdx.x * blockDim.x + threadIdx.x;
            var threads = gridDim.x * blockDim.x;

            while (tid < height)
            {
                a(tid);

                tid += threads;
            }
        };

        private static int GetKernelDimension(int size)
        {
            return (int)Math.Round(Math.Pow(2, Math.Ceiling(Math.Log(Math.Sqrt(size), 2))));
        }

        public static Matrix Phansalkar(Matrix input, int radius)
        {
            var width = input.Width;
            var height = input.Height;
            
            var data = (float[]) input;
            var result = (float[])new Matrix(width, height);
            
            var p = 2.5f;
            var q = 10;
            var k = 0.15;
            var r = 0.4;

            var dimension = GetKernelDimension(height);

            Gpu.Default.Launch(KernelBuilder(tid =>
            {
                var index = tid * width;

                for (var x = 0; x < width; x++)
                {
                    var sum = 0f;
                    var count = 0;
                    
                    for (var ky = -radius; ky <= radius; ky++)
                    {
                        var py = tid + ky;
                        var pindex = py * width;

                        for (var kx = -radius; kx <= radius; kx++)
                        {
                            var px = x + kx;

                            if (px >= 0 && px < width && py >= 0 && py < height)
                            {
                                sum += data[pindex + px] / byte.MaxValue;
                                count++;
                            }
                        }
                    }

                    var mean = sum / count;
                    var variance = DeviceFunction.Abs(sum / count - mean);
                    var deviation = DeviceFunction.Sqrt(variance / count);

                    var threshold = mean * (1f + p * DeviceFunction.Exp(-q * mean) + k * (deviation / r - 1));

                    result[index + x] = data[index + x] / byte.MaxValue > threshold ? byte.MaxValue : 0;
                }

            }, height), new LaunchParam(dimension, dimension));

            return new Matrix(width, height, result);
        }

        public static Matrix AutoGamma(Matrix input, int depth)
        {
            var size = (int)Math.Pow(2, depth) - 1;

            var width = input.Width;
            var height = input.Height;

            var mean = input.Mean();

            var gamma = Math.Log((size / 2f) / size) / Math.Log(mean / size);

            var result = new float[width, height];

            Parallel.For(0, width, x =>
            {
                for (var y = 0; y < height; y++)
                {
                    result[x, y] = Clamp(size * Math.Pow((input[x, y] / size), gamma), 0, size);
                }
            });

            return result;
        }

        public static Matrix AutoContrast(Matrix input, int depth)
        {
            var size = (int)Math.Pow(2, depth) - 1;

            var histogram = new Histogram(input, size);

            var width = input.Width;
            var height = input.Height;

            var result = new float[width, height];

            var length = width * height;
            var limit = length / 100;
            var threshold = length / 500000;

            var min = histogram.Min;
            var max = histogram.Max;

            var count = 0;
            while (min < size)
            {
                count = histogram[min];
                if (count > limit)
                {
                    count = 0;
                }
                if (count > threshold)
                {
                    break;
                }
                min++;
            }
            
            while (max > 0)
            {
                count = histogram[max];
                if (count > limit)
                {
                    count = 0;
                }
                if (count > threshold)
                {
                    break;
                }
                max--;
            }

            var scale = size / (max+ 1 - min);

            Parallel.For(0, width, x =>
            {
                for (var y = 0; y < height; y++)
                {
                    result[x, y] = Clamp(((input[x, y] - min) * scale), 0, size);
                }
            });

            return result;
        }

        private static float Clamp(double value, float min, float max)
        {
            return (float)(value < min ? min : value > max ? max : value);
        }

        public static float[,] Hysteresis(float[,] input, int min, int max)
        {
            var width = input.GetLength(0);
            var height = input.GetLength(1);

            var result = new float[width, height];

            Parallel.For(0, width, x =>
            {
                for (var y = 0; y < height; y++)
                {
                    var value = 0f;
                    if (input[x, y] >= max)
                    {
                        value = float.MaxValue;
                        HysteresisRecursive(x, y, input, width, height, ref result, min);
                    }
                    result[x, y] = result[x, y] > 0 ? result[x, y] : value;
                }
            });

            return result;
        }

        private static void HysteresisRecursive(int x, int y, float[,] input, int width, int height, ref float[,] result,
            int min)
        {
            for (var tx = x - 1; tx <= x + 1; tx++)
            {
                for (var ty = y - 1; ty <= y + 1; ty++)
                {
                    if (tx == x && ty == y || result[tx, ty] > 0) continue;

                    if (tx >= 0 && tx < width && ty >= 0 && ty < height)
                    {
                        if (input[tx, ty] >= min)
                        {
                            result[tx, ty] = float.MaxValue;
                            HysteresisRecursive(tx, ty, input, width, height, ref result, min);
                        }
                    }
                }
            }
        }

        public static Sobel Sobel(float[,] input, int min = 1, int max = 2)
        {
            var width = input.GetLength(0);
            var height = input.GetLength(1);

            var result = new Sobel(width, height);

            Parallel.For(1, width - 1, x =>
            {
                for (var y = 1; y < height - 1; y++)
                {
                    var gx =
                        input[x + 1, y - 1] * min +
                        input[x + 1, y] * max +
                        input[x + 1, y + 1] * min -
                        input[x - 1, y - 1] * min -
                        input[x - 1, y] * max -
                        input[x - 1, y + 1] * min;

                    var gy =
                        input[x - 1, y + 1] * min +
                        input[x, y + 1] * max +
                        input[x + 1, y + 1] * min -
                        input[x - 1, y - 1] * min -
                        input[x, y - 1] * max -
                        input[x + 1, y - 1] * min;


                    result.Magnitude[x, y] = Math.Abs(gx) + Math.Abs(gy);
                    
                    result.Horizontal[x, y] = gx;
                    result.Vertical[x, y] = gy;
                }
            });

            return result;
        }

        public static Matrix Gaussian(Matrix input, Matrix kernel)
        {
            var width = input.Width;
            var height = input.Height;
            var kernelSize = (int) (Math.Sqrt(kernel.Length) - 1) / 2;

            var result = new Matrix(width, height);
            
            for(var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    float val = 0;
                    for (var kx = -kernelSize; kx <= kernelSize; kx++)
                    {
                        for (var ky = -kernelSize; ky <= kernelSize; ky++)
                        {
                            if (x + kx >= 0 && x + kx < width && y + ky >= 0 && y + ky < height)
                            {
                                val += input[x + kx, y + ky] * kernel[kx + kernelSize, ky + kernelSize];
                            }
                        }
                    }

                    result[x, y] = val;
                }
            }

            return result;
        }

        public static float[,] Canny(float[,] input, int min, int max)
        {
            return Canny(Sobel(input), min, max);
        }

        public static float[,] Canny(Sobel sobel, int min, int max)
        {
            var width = sobel.Orientation.GetLength(0);
            var height = sobel.Orientation.GetLength(1);

            var result = new float[width, height];

            Parallel.For(1, width - 1, x =>
            {
                for (var y = 0; y < height - 1; y++)
                {
                    result[x, y] = sobel.Magnitude[x, y];
                }
            });

            // Non maximum supression
            for (var x = 1; x < width - 1; x++)
            {
                for (var y = 1; y < height - 1; y++)
                {
                    var gx = sobel.Horizontal[x, y];
                    var gy = sobel.Vertical[x, y];
                    var magnitude = sobel.Magnitude[x, y];
                    var orientation = Math.Atan2(gy, gx) * 180f / Math.PI;

                    //if (orientation >= 0 && orientation <= 45 || 
                    //    orientation < -135 && orientation >= -180)
                    //{
                    //    var yBot = new[]
                    //    {
                    //        sobel.Magnitude[x, y + 1], sobel.Magnitude[x + 1, y + 1]
                    //    };
                    //    var yTop = new[]
                    //    {
                    //        sobel.Magnitude[x, y - 1], sobel.Magnitude[x - 1, y - 1]
                    //    };

                    //    var xEst = Math.Abs(gy / magnitude);

                    //    if (magnitude >= (yBot[1] - yBot[0]) * xEst + yBot[0] &&
                    //        magnitude >= (yTop[1] - yTop[0]) * xEst + yTop[0])
                    //    {
                    //        result[x, y] = magnitude;
                    //    }
                    //    else
                    //    {
                    //        result[x, y] = 0;
                    //    }
                    //    continue;
                    //}

                    //if (orientation > 45 && orientation <= 90 ||
                    //    orientation < -90 && orientation >= -135)
                    //{
                    //    var yBot = new[]
                    //    {
                    //        sobel.Magnitude[x + 1, y], sobel.Magnitude[x + 1, y + 1]
                    //    };
                    //    var yTop = new[]
                    //    {
                    //        sobel.Magnitude[x - 1, y], sobel.Magnitude[x - 1, y - 1]
                    //    };

                    //    var xEst = Math.Abs(gy / magnitude);

                    //    if (magnitude >= (yBot[1] - yBot[0]) * xEst + yBot[0] &&
                    //        magnitude >= (yTop[1] - yTop[0]) * xEst + yTop[0])
                    //    {
                    //        result[x, y] = magnitude;
                    //    }
                    //    else
                    //    {
                    //        result[x, y] = 0;
                    //    }
                    //    continue;
                    //}

                    //if (orientation > 90 && orientation <= 135 ||
                    //    orientation < -45 && orientation >= -90)
                    //{
                    //    var yBot = new[]
                    //    {
                    //        sobel.Magnitude[x + 1, y], sobel.Magnitude[x + 1, y - 1]
                    //    };
                    //    var yTop = new[]
                    //    {
                    //        sobel.Magnitude[x - 1, y], sobel.Magnitude[x - 1, y + 1]
                    //    };

                    //    var xEst = Math.Abs(gy / magnitude);

                    //    if (magnitude >= (yBot[1] - yBot[0]) * xEst + yBot[0] &&
                    //        magnitude >= (yTop[1] - yTop[0]) * xEst + yTop[0])
                    //    {
                    //        result[x, y] = magnitude;
                    //    }
                    //    else
                    //    {
                    //        result[x, y] = 0;
                    //    }
                    //    continue;
                    //}

                    //if (orientation > 135 && orientation <= 180 ||
                    //    orientation < 0 && orientation >= -45)
                    //{
                    //    var yBot = new[]
                    //    {
                    //        sobel.Magnitude[x, y - 1], sobel.Magnitude[x + 1, y - 1]
                    //    };
                    //    var yTop = new[]
                    //    {
                    //        sobel.Magnitude[x, y + 1], sobel.Magnitude[x - 1, y + 1]
                    //    };

                    //    var xEst = Math.Abs(gy / magnitude);

                    //    if (magnitude >= (yBot[1] - yBot[0]) * xEst + yBot[0] &&
                    //        magnitude >= (yTop[1] - yTop[0]) * xEst + yTop[0])
                    //    {
                    //        result[x, y] = magnitude;
                    //    }
                    //    else
                    //    {
                    //        result[x, y] = 0;
                    //    }
                    //}

                    // E-W
                    if (orientation <= 22.5 && orientation >= -22.5 ||
                        orientation >= 157.5 || orientation <= -157.5)
                    {
                        if (magnitude < sobel.Magnitude[x - 1, y] ||
                            magnitude < sobel.Magnitude[x + 1, y])
                        {
                            result[x, y] = 0;
                        }
                    }

                    // N-S
                    if (orientation >= 67.5 && orientation <= 112.5 ||
                        orientation >= -112.5 && orientation <= -67.5)
                    {
                        if (magnitude < sobel.Magnitude[x, y - 1] ||
                            magnitude < sobel.Magnitude[x, y + 1])
                        {
                            result[x, y] = 0;
                        }
                        continue;
                    }


                    // SE-NW
                    if (orientation <= -112.5 && orientation >= -157.5 ||
                        orientation >= 22.5 && orientation <= 67.5)
                    {
                        if (magnitude < sobel.Magnitude[x - 1, y - 1] ||
                            magnitude < sobel.Magnitude[x + 1, y + 1])
                        {
                            result[x, y] = 0;
                        }
                        continue;
                    }

                    if (orientation <= 157.5 && orientation >= 112.5 ||
                        orientation >= -67.5 && orientation <= -22.5)
                    {
                        if (magnitude < sobel.Magnitude[x + 1, y - 1] ||
                            magnitude < sobel.Magnitude[x - 1, y + 1])
                        {
                            result[x, y] = 0;
                        }
                        continue;
                    }

                }
            };

            return result;// Hysteresis(result, min, max);
        }

        public static float[,] Hough(float[,] input, int radius, int increment = 5, int threshold = 30)
        {
            var width = input.GetLength(0);
            var height = input.GetLength(1);

            var result = new float[width, height];

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (input[x, y] > 0)
                    {
                        for (var theta = 0; theta < 360; theta += increment)
                        {
                            var a = (int) (x - radius * Math.Cos(theta * Math.PI / 180));
                            var b = (int) (y - radius * Math.Sin(theta * Math.PI / 180));

                            if (a > 0 && a < width && b > 0 && b < height)
                            {
                                result[a, b] += 1;
                            }
                        }
                    }
                }
            }

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (result[x, y] < 360f / increment * threshold / 100f)
                    {
                        result[x, y] = 0;
                    }
                }
            }

            return result;
        }

        public static float[,] Invert(float[,] input, int max = 255)
        {
            var width = input.GetLength(0);
            var height = input.GetLength(1);

            var result = new float[width, height];

            Parallel.For(0, width, x =>
            {
                for (var y = 0; y < height; y++)
                {
                    result[x, y] = max - input[x, y];
                }
            });

            return result;
        }

        public static float[] Histogram(float[,] input)
        {
            var data = input.Cast<float>().ToArray();
            var result = new float[(int)data.Max() - 1];

            for (var i = 0; i < data.Length; i++)
            {
                result[(int)data[i]]++;
            }

            return result;
        }

        private class WatershedLabel
        {
            public const int Init = -1;
            public const int Mask = -2;
            public const int Watershed = 0;
        }

        private class WatershedPixel
        {
            public int X { get; set; }
            public int Y { get; set; }
            public float Intensity { get; set; }
            public int Label { get; set; }
            public int Distance { get; set; }

            internal WatershedPixel(int x, int y, float intensity)
            {
                X = x; Y = y; Intensity = intensity;
                Label = WatershedLabel.Init;
            }

            public override bool Equals(object obj)
            {
                WatershedPixel p = (WatershedPixel)obj;
                return (X == p.X && X == p.Y);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }
        
        private static List<List<WatershedPixel>> PixelHistogram(WatershedPixel[,] input)
        {
            var width = input.GetLength(0);
            var height = input.GetLength(1);
            var max = (int) input.Cast<WatershedPixel>().Max(p => p.Intensity);
            var result = new List<List<WatershedPixel>>();
            for (var i = 0; i < max + 1; i++)
            {
                result.Add(new List<WatershedPixel>());
            }

            for (var x = 0; x < width; x++)
            {
                for(var y = 0; y < height; y++)
                {
                    var pixel = input[x, y];
                    result[(int) pixel.Intensity].Add(pixel);
                }
            }

            return result;
        }

        private static WatershedPixel[,] PixelMap(float[,] input)
        {
            var width = input.GetLength(0);
            var height = input.GetLength(1);

            var result = new WatershedPixel[width, height];

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    result[x, y] = new WatershedPixel(x, y, input[x, y]);
                }
            }

            return result;
        }

        public static float[,] Watershed(float[,] input, byte color = 255)
        {
            var width = input.GetLength(0);
            var height = input.GetLength(1);

            
            var queue = new Queue<WatershedPixel>();
            var result = new float[width, height];// input.Clone() as float[,];

            var map = PixelMap(input);
            var histogram = PixelHistogram(map);
            var distance = 0;
            var end = new WatershedPixel(-1, -1, 0);
            var count = 0;
            var label = 0;

            for (var h = 0; h < histogram.Count; h++)
            {
                foreach (var pixel in histogram[h])
                {
                    pixel.Label = WatershedLabel.Mask;
                    
                    foreach (var neighbour in Neighbours(map, pixel.X, pixel.Y, width, height))
                    {
                        if (neighbour == null) continue;
                        
                        if (neighbour.Label > 0 || neighbour.Label == WatershedLabel.Watershed)
                        {
                            pixel.Distance = 1;
                            queue.Enqueue(pixel);
                            break;
                        }
                    }
                }

                distance = 1;
                queue.Enqueue(end);

                while(true)
                {
                    var pixel = queue.Dequeue();
                    if (pixel.Equals(end))
                    {
                        if (queue.Count == 0)
                        {
                            break;
                        }
                        else
                        {
                            queue.Enqueue(pixel);
                            distance++;
                            pixel = queue.Dequeue();
                        }
                    }

                    foreach(var neighbour in Neighbours(map, pixel.X, pixel.Y, width, height))
                    {
                        if (neighbour == null) continue;

                        if (neighbour.Distance <= distance && 
                            (neighbour.Label > 0 || neighbour.Label == WatershedLabel.Watershed))
                        {
                            if (neighbour.Label > 0)
                            {
                                if (pixel.Label == WatershedLabel.Mask)
                                {
                                    pixel.Label = neighbour.Label;
                                }
                                else if(pixel.Label != neighbour.Label)
                                {
                                    pixel.Label = WatershedLabel.Watershed;
                                    count++;
                                }
                            }
                            else if(pixel.Label == WatershedLabel.Mask)
                            {
                                pixel.Label = WatershedLabel.Mask;
                                count++;
                            }
                        }
                        else if(neighbour.Label == WatershedLabel.Mask && neighbour.Distance == 0)
                        {
                            neighbour.Distance = distance + 1;
                            queue.Enqueue(neighbour);
                        }
                    }
                }

                foreach (var pixel in histogram[h])
                {
                    pixel.Distance = 0;

                    if (pixel.Label == WatershedLabel.Mask)
                    {
                        label++;
                        pixel.Label = label;
                        queue.Enqueue(pixel);

                        while (queue.Count > 0)
                        {
                            var n = queue.Dequeue();
                            foreach (var neighbour in Neighbours(map, n.X, n.Y, width, height))
                            {
                                if (neighbour == null) continue;

                                if (neighbour.Label == WatershedLabel.Mask)
                                {
                                    neighbour.Label = label;
                                    queue.Enqueue(neighbour);
                                }
                            }
                        }
                    }
                }
            }

            if (count > 0)
            {
                for (var x = 0; x < width; x++)
                {
                    for(var y = 0; y < height; y++)
                    {
                        if (map[x, y].Label == WatershedLabel.Watershed)
                        {
                            result[x, y] = color;
                        }
                    }
                }
            }

            return result;
        }

        private static List<WatershedPixel> Neighbours(WatershedPixel[,] input, int x, int y, int width, int height)
        {
            var result = new List<WatershedPixel>();

            for (var px = -1; px <= 1; px++)
            {
                for (var py = -1; py <= 1; py++)
                {
                    if (px == 0 && py == 0) continue;

                    if (x + px >= 0 && y + py >= 0 && x + px < width && y + py < height)
                    {
                        result.Add(input[x + px, y + py]);
                    }
                }
            }

            return result;
        }


        #region Morphology filters

        public static class Morphology
        {


            public static float[,] Dilate(float[,] input, int size)
            {
                var width = input.GetLength(0);
                var height = input.GetLength(1);

                var radius = (size - 1) / 2;
                var result = new float[width, height];

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var value = 0f;
                        var set = false;
                        for (var nx = -radius; nx <= radius; nx++)
                        {
                            for (var ny = -radius; ny <= radius; ny++)
                            {
                                if (x + nx >= 0 && x + nx < width && y + ny >= 0 && y + ny < height)
                                {
                                    var r = Math.Sqrt(Math.Pow(nx, 2) + Math.Pow(ny, 2));
                                    if (r < radius)
                                    {
                                        value = Math.Max(value, input[x + nx, y + ny]);
                                        set = true;
                                    }
                                }
                            }
                        }

                        if (set)
                        {
                            result[x, y] = value;
                        }
                    }
                }

                return result;
            }

            public static float[,] Erode(float[,] input, int size)
            {
                var width = input.GetLength(0);
                var height = input.GetLength(1);

                var radius = (size - 1) / 2;
                var result = new float[width, height];

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var value = 255f;
                        var set = false;
                        for (var nx = -radius; nx <= radius; nx++)
                        {
                            for (var ny = -radius; ny <= radius; ny++)
                            {
                                if (x + nx >= 0 && x + nx < width && y + ny >= 0 && y + ny < height)
                                {
                                    var r = Math.Sqrt(Math.Pow(nx, 2) + Math.Pow(ny, 2));
                                    if (r < radius)
                                    {
                                        value = Math.Min(value, input[x + nx, y + ny]);
                                        set = true;
                                    }
                                }
                            }
                        }

                        if (set)
                        {
                            result[x, y] = value;
                        }
                    }
                }

                return result;
            }

            public static float[,] Close(float[,] input, int size)
            {
                var result = Dilate(input, size);
                result = Erode(result, size);
                return result;
            }

            public static float[,] Open(float[,] input, int size)
            {
                var result = Erode(input, size);
                result = Dilate(result, size);
                return result;
            }
        }
        #endregion
    }
}