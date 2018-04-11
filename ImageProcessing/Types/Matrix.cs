namespace ImageProcessing.Types
{
    using System;
    using System.Linq;

    public class Matrix
    {
        public int Width { get; }
        public int Height { get; }


        public float Mean() => _data.Sum() / _data.Length;

        public float Variance()
        {
            var mean = Mean();
            return (float)_data.Sum(v => Math.Pow(v - mean, 2)) / _data.Length;
        }
        
        public float StandardDeviation()
        {
            return (float) Math.Sqrt(Variance());
        }

        public float Min()
        {
            return _data.Min();
        }

        public float Max()
        {
            return _data.Max();
        }

        private readonly float[] _data;

        public int Length => Width * Height;

        public Matrix(int size) 
            : this(size, size) { }

        public Matrix(int width, int height)
        {
            Width = width;
            Height = height;
            _data = new float[width * height];
        }

        public Matrix(float[,] data)
            : this(data.GetLength(0), data.GetLength(1))
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    this[x, y] = data[x, y];
                }
            }
        }

        public Matrix(int width, int height, float[] data)
            :this(width, height)
        {
            _data = data;
        }

        public float this[int index]
        {
            get => _data[index];
            set => _data[index] = value; 
        }

        public float this[int x, int y]
        {
            get => this[y * Width + x];
            set => this[y * Width + x] = value;
        }

        public static Matrix operator +(Matrix a, Matrix b)
        {
            if (a.Width != b.Width || a.Height != b.Height)
            {
                throw new ArgumentException("Different dimensions");
            }

            var result = new Matrix(a.Width, a.Height);

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = a[i] + b[i];
            }

            return result;
        }

        public static Matrix operator -(Matrix a, Matrix b)
        {
            if (a.Width != b.Width || a.Height != b.Height)
            {
                throw new ArgumentException("Different dimensions");
            }

            var result = new Matrix(a.Width, a.Height);

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = a[i] - b[i];
            }

            return result;
        }

        public static Matrix operator -(Matrix matrix, float sub)
        {
            var result = new Matrix(matrix.Width, matrix.Height);

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = matrix[i] - sub;
            }

            return result;
        }

        public static Matrix operator *(Matrix a, Matrix b)
        {
            if (a.Width != b.Width || a.Height != b.Height)
            {
                throw new ArgumentException("Different dimensions");
            }

            var result = new Matrix(a.Width, a.Height);

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = a[i] * b[i];
            }

            return result;
        }

        public static Matrix operator *(Matrix matrix, float mul)
        {
            var result = new Matrix(matrix.Width, matrix.Height);

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = matrix[i] * mul;
            }

            return result;
        }

        public static Matrix operator /(Matrix a, Matrix b)
        {
            if (a.Width != b.Width || a.Height != b.Height)
            {
                throw new ArgumentException("Different dimensions");
            }

            var result = new Matrix(a.Width, a.Height);

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = a[i] / b[i];
            }

            return result;
        }

        public static Matrix operator /(Matrix matrix, float div)
        {
            var result = new Matrix(matrix.Width, matrix.Height);

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = matrix[i] / div;
            }

            return result;
        }

        public static implicit operator float[,] (Matrix matrix)
        {
            var result = new float[matrix.Width, matrix.Height];

            for (var x = 0; x < matrix.Width; x++)
            {
                for (var y = 0; y < matrix.Height; y++)
                {
                    result[x, y] = matrix[x, y];
                }
            }

            return result;
        }

        public static Matrix Sqrt(Matrix matrix)
        {
            var result = new Matrix(matrix.Width, matrix.Height);

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = (float) Math.Sqrt(matrix[i]);
            }

            return result;
        }

        public static Matrix Abs(Matrix matrix)
        {
            var result = new Matrix(matrix.Width, matrix.Height);

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = Math.Abs(matrix[i]);
            }

            return result;
        }

        public static Matrix Pow(Matrix matrix, float pow)
        {
            var result = new Matrix(matrix.Width, matrix.Height);

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = (float) Math.Pow(matrix[i], pow);
            }

            return result;
        }

        public static implicit operator float[] (Matrix matrix)
        {
            return matrix._data;
        }

        public static implicit operator Matrix(float[,] data)
        {
            return new Matrix(data);
        }
    }
}