using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing.EdgeDetection
{

    public interface IFilter<in TInput, out TOutput>
    {
        TOutput Apply(TInput input);
    }

    public class SobelValues
    {
        public int Width { get; }
        public int Height { get; }
        public double[,] Magnitude { get; }
        public float[,] Orientation { get; }
        public float[,] Horizontal { get; }
        public float[,] Vertical { get; }

        public SobelValues(int width, int height)
        {
            Width = width;
            Height = height;
            Magnitude = new double[width, height];
            Orientation = new float[width, height];
            Horizontal = new float[width, height];
            Vertical = new float[width, height];
        }
    }

    public class Sobel : IFilter<float[,], SobelValues>
    {
        private readonly int[,] _sobelX =
        {
            {-1, 0, 1},
            {-2, 0, 2},
            {-1, 0, 1}
        };

        private readonly int[,] _sobelY =
        {
            {-1,-2,-1},
            { 0, 0, 0},
            { 1, 2, 1}
        };

        public SobelValues Apply(float[,] input)
        {
            var width = input.GetLength(0);
            var height = input.GetLength(1);

            var result = new SobelValues(width, height);

            for (var x = 1; x < width - 1; x++)
            {
                for (var y = 1; y < height - 1; y++)
                {
                    var gx = 0d;
                    var gy = 0d;

                    for (var px = 0; px < _sobelX.GetLength(0); px++)
                    {
                        for (var py = 0; py < _sobelY.GetLength(1); py++)
                        {
                            gx += _sobelX[px, py] * input[px + x + -1, py + y + -1];
                            gy += _sobelY[px, py] * input[px + x + -1, py + y + -1];
                        }
                    }

                    result.Horizontal[x, y] = (float)gx;
                    result.Vertical[x, y] = (float) gy;

                    var angle = Math.Atan2(gy, gx) * 180f / Math.PI;

                    result.Orientation[x, y] = (float)angle;
                    result.Magnitude[x, y] = Math.Abs(gx) + Math.Abs(gy);
                }
            }

            return result;
        }
    }

    public class NonMaxSupression : IFilter<SobelValues, float[,]>
    {
        public float[,] Apply(SobelValues input)
        {
            var width = input.Width;
            var height = input.Height;

            var result = new float[width, height];

            for (var x = 1; x < input.Width - 1; x++)
            {
                for (var y = 0; y < height - 1; y++)
                {
                    result[x, y] = (float) input.Magnitude[x, y];
                }
            };

            for (var x = 1; x < input.Width - 1; x++)
            {
                for (var y = 1; y < input.Height - 1; y++)
                {
                    var orientation = input.Orientation[x, y];
                    var magnitude = input.Magnitude[x, y];

                    // N-S - WORKS
                    if (orientation < 22.5 && orientation > -22.5 ||
                        orientation >= 157.5 || orientation <= -157.5)
                    {
                        if (magnitude <= input.Magnitude[x, y - 1] ||
                            magnitude <= input.Magnitude[x, y + 1])
                        {
                            result[x, y] = 0;
                        }
                        continue;
                    }

                    // E-W - WORKS
                    if (orientation >= 67.5 && orientation <= 112.5 ||
                        orientation >= -112.5 && orientation <= -67.5)
                    {
                        if (magnitude <= input.Magnitude[x - 1, y] ||
                            magnitude <= input.Magnitude[x + 1, y])
                        {
                            result[x, y] = 0;
                        }
                        continue;
                    }

                    // SE-NW - WORKS
                    if (orientation <= 67.5 && orientation >= 22.5 ||
                        orientation >= -157.5 && orientation <= -112.5)
                    {
                        if (magnitude < input.Magnitude[x - 1, y - 1] ||
                            magnitude < input.Magnitude[x + 1, y + 1])
                        {
                            result[x, y] = 0;
                        }
                        continue;
                    }

                    // NE-SW - WORKS
                    if (orientation <= -22.5 && orientation >= -67.5 ||
                        orientation >= 112.5 && orientation <= 157.5)
                    {
                        if (magnitude < input.Magnitude[x + 1, y - 1] ||
                            magnitude < input.Magnitude[x - 1, y + 1])
                        {
                            result[x, y] = 0;
                        }
                        continue;
                    }


                    //switch (input.Orientation[x, y])
                    //{
                    //    case 0:
                    //        {
                    //            if (input.Magnitude[x, y] <= input.Magnitude[x + 1, y] ||
                    //                input.Magnitude[x, y] <= input.Magnitude[x - 1, y])
                    //            {
                    //                result[x, y] = 0;
                    //            }
                    //            else
                    //            {
                    //                result[x, y] = input.Magnitude[x, y];
                    //            }
                    //            break;
                    //        }
                    //case 45:
                    //    {
                    //        if (input.Magnitude[x, y] <= input.Magnitude[x + 1, y - 1] ||
                    //            input.Magnitude[x, y] <= input.Magnitude[x - 1, y + 1])
                    //        {
                    //            result[x, y] = 0;
                    //        }
                    //        else
                    //        {
                    //            result[x, y] = input.Magnitude[x, y];
                    //        }
                    //        break;
                    //    }
                    //case 90:
                    //    {
                    //        if (input.Magnitude[x, y] <= input.Magnitude[x, y - 1] ||
                    //            input.Magnitude[x, y] <= input.Magnitude[x, y + 1])
                    //        {
                    //            result[x, y] = 0;
                    //        }
                    //        else
                    //        {
                    //            result[x, y] = input.Magnitude[x, y];
                    //        }
                    //        break;
                    //    }
                    //case 135:
                    //    {
                    //        if (input.Magnitude[x, y] <= input.Magnitude[x - 1, y - 1] ||
                    //            input.Magnitude[x, y] <= input.Magnitude[x + 1, y + 1])
                    //        {
                    //            result[x, y] = 0;
                    //        }
                    //        else
                    //        {
                    //            result[x, y] = input.Magnitude[x, y];
                    //        }
                    //        break;
                    //    }
                    //default:
                    //    {
                    //        result[x, y] = input.Magnitude[x, y];
                    //        break;
                    //    }
                    //}
                }
            }

            return result;
        }
    }
}
