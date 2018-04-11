// -----------------------------------------------------------------------
//  <copyright file="Histogram.cs" company="Solentim">
//      Copyright (c) Solentim 2018. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

namespace ImageProcessing.Types
{
    using System.Linq;

    public class Histogram
    {
        private readonly int[] _values;

        public Histogram(float[,] input, int max)
        {
            _values = new int[max + 1];

            Generate(input);
        }

        public int this[int index] => _values[index];

        private void Generate(float[,] input)
        {
            var data = input.Cast<float>().ToArray();

            foreach (var pixel in data)
            {
                var value = (int) pixel;

                value = value < 0 ? 0 : value > byte.MaxValue ? byte.MaxValue : value;

                if (value > 0 && value < Min)
                {
                    Min = value;
                }

                if (value > Max)
                {
                    Max = value;
                }

                _values[value]++;
            }
        }

        public int Min { get; private set; } = int.MaxValue;

        public int Max { get; private set; } = int.MinValue;
    }
}