// -----------------------------------------------------------------------
//  <copyright file="ImageFilters.cs" company="Solentim">
//      Copyright (c) Solentim 2018. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

namespace ImageProcessing
{
    using System;
    using System.Runtime.InteropServices;
    using Types;

    public static class ImageFilters
    {
        public static readonly Matrix Sharpen = new Matrix(new [,]
        {
            {1 / 9f, 1 / 9f, 1 / 9f},
            {1 / 9f, 1, 1 / 9f},
            {1 / 9f, 1 / 9f, 1 / 9f}
        });

        public static Matrix Gaussian(float sigma, int size)
        {
            var radius = size >> 1;
            var result = new Matrix(size);
            var sum = 0f;

            var euler = 1.0 / (2.0 * Math.PI * Math.Pow(sigma, 2));

            for (var x = -radius + 1; x < radius; x++)
            {
                for (var y = -radius + 1; y < radius; y++)
                {
                    var distance = (Math.Pow(x, 2) + Math.Pow(y, 2)) / (2 * Math.Pow(sigma, 2));

                    result[x + radius, y + radius] = (float)(euler * Math.Exp(-distance));

                    sum += result[x + radius, y + radius];
                }
            }

            for (var x = 0; x < size; x++)
            {
                for (var y = 0; y < size; y++)
                {
                    result[x, y] *= 1 / sum;
                }
            }

            return result;
        }
        
        public static Matrix MexicanHat(float sigma, int size)
        {
            var radius = size >> 1;

            float Operator(float x, float y)
            {
                return (float) ((1 / (Math.PI * Math.Pow(sigma, 2))) * (1 - ((Math.Pow(x, 2) + Math.Pow(y, 2)) / (2 * Math.Pow(sigma, 2)))) * Math.Exp(-((Math.Pow(x, 2) + Math.Pow(y, 2)) / (2 * Math.Pow(sigma, 2)))));
            }

            var result = new Matrix(size);

            for (var x = -radius; x < radius; x++)
            {
                for (var y = -radius; y < radius; y++)
                {
                    result[x + radius, y + radius] = Operator(x, y);
                }
            }

            return result;
        }
    }
}