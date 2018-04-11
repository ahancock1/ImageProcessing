using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing.EdgeDetection
{
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Types;
    using Matrix = global::ImageProcessing.Types.Matrix;

    public static class IProcessing
    {
        public static class Helpers
        {

            public static double Interpolate(
                double[] X, double[] Y,
                double[,] Values,
                double x, double y)
            {
                var q11 = Values[0, 0] * (X[1] - x) * (Y[1] - y) / ((X[1] - X[0]) * (Y[1] - Y[0]));
                var q21 = Values[1, 0] * (x - X[0]) * (Y[1] - y) / ((X[1] - X[0]) * (Y[1] - Y[0]));
                var q12 = Values[0, 1] * (X[1] - x) * (y - Y[0]) / ((X[1] - X[0]) * (Y[1] - Y[0]));
                var q22 = Values[1, 1] * (x - X[0]) * (y - Y[0]) / ((X[1] - X[0]) * (Y[1] - Y[0]));

                return q11 + q21 + q12 + q22;
            }

            public static float Max(Matrix input)
            {
                var result = float.MinValue;

                for (var x = 0; x < input.Width; x++)
                {
                    for (var y = 0; y < input.Height; y++)
                    {
                        if (input[x, y] > result)
                        {
                            result = input[x, y];
                        }
                    }
                }

                return result;
            }

            public static float Min(Matrix input)
            {
                var result = float.MaxValue;

                for (var x = 0; x < input.Width; x++)
                {
                    for (var y = 0; y < input.Height; y++)
                    {
                        if (input[x, y] < result)
                        {
                            result = input[x, y];
                        }
                    }
                }

                return result;
            }
        }
    }

    public class Canny : IFilter<Matrix, Matrix>
    {
        public Matrix Apply(Matrix input)
        {
            var guassian = ImageFilters.Gaussian(0.6f, 5);

            var r = new ImageData(input.Width, input.Height, PixelFormats.Gray8);

            var blurred = ImageProcessing.Convolve(input, guassian);

            r[0] = ImageProcessing.Scale(blurred, 0, 255);
            r.Save<JpegBitmapEncoder>(@"C:\imageprocessing\blurred.jpg");

            var gx = ImageProcessing.Convolve(input, new float[,]
            {
                {-1, 0, 1},
                {-2, 0, 2},
                {-1, 0, 1}
            });

            r[0] = ImageProcessing.Scale(gx, 0, 255);
            r.Save<JpegBitmapEncoder>(@"C:\imageprocessing\gx.jpg");

            var gy = ImageProcessing.Convolve(input, new float[,]
            {
                {1, 2, 1},
                {0, 0, 0},
                {-1, -2, -1}
            });
            
            r[0] = ImageProcessing.Scale(gy, 0, 255);
            r.Save<JpegBitmapEncoder>(@"C:\imageprocessing\gy.jpg");

            var gradient = Matrix.Sqrt(Matrix.Pow(gx, 2) + Matrix.Pow(gy, 2));

            r[0] = ImageProcessing.Scale(gradient, 0, 255);
            r.Save<JpegBitmapEncoder>(@"C:\imageprocessing\gradient.jpg");


            var angle = new Matrix(input.Width, input.Height);

            for (var x = 1; x < gradient.Width - 1; x++)
            {
                for (var y = 1; y < gradient.Height - 1; y++)
                {
                    var orientation = (float)(Math.Atan2(gy[x, y], gx[x, y]) * 180f / Math.PI);
                    if (orientation < 0)
                    {
                        orientation += 180f;
                    }
                    angle[x, y] = orientation;
                }
            }

            r[0] = ImageProcessing.Scale(angle, 0, 255);
            r.Save<JpegBitmapEncoder>(@"C:\imageprocessing\angle.jpg");

            var result = new Matrix(input.Width, input.Height);

            for (var x = 1; x < gradient.Width - 1; x++)
            {
                for (var y = 1; y < gradient.Height - 1; y++)
                {
                    var orientation = angle[x, y];

                    var val = gradient[x,y];

                    // N-S
                    if (orientation <= 22.5 || orientation >= 157.5)
                    {
                        if (gradient[x, y] > gradient[x, y - 1] &&
                            gradient[x, y] > gradient[x, y + 1])
                        {
                            result[x, y] = val;
                        }
                    }

                    // E-W
                    if (orientation >= 67.5 && orientation <= 112.5)
                    {
                        if (gradient[x, y] > gradient[x - 1, y] &&
                            gradient[x, y] > gradient[x + 1, y])
                        {
                            result[x, y] = val;
                        }
                    }

                    // NE-SW
                    if (orientation >= 22.5 && orientation <= 67.5)
                    {
                        if (gradient[x, y] > gradient[x + 1, y - 1] &&
                            gradient[x, y] > gradient[x - 1, y + 1])
                        {
                            result[x, y] = val;
                        }
                    }

                    // SE-NW
                    if (orientation <= 157.5 && orientation >= 112.5)
                    {
                        if (gradient[x, y] > gradient[x - 1, y - 1] &&
                            gradient[x, y] > gradient[x + 1, y + 1])
                        {
                            result[x, y] = val;
                        }
                    }

                }
            }

            return result;
        }
    }
}
