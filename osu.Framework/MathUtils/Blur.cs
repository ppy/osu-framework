// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.MathUtils
{
    /// <summary>
    /// Helper methods for blur calculations.
    /// </summary>
    public static class Blur
    {
        /// <summary>
        /// Evaluates a 1-D gaussian distribution with 0 mean and sigma standard deviation at position x.
        /// </summary>
        /// <param name="x">The position to evaluate the distribution at.</param>
        /// <param name="sigma">Standard deviation of the distribution.</param>
        /// <returns>The probability density of the distribution at x.</returns>
        public static double EvalGaussian(float x, float sigma)
        {
            const double inv_sqrt_2_pi = 0.39894;
            return inv_sqrt_2_pi * Math.Exp(-0.5 * x * x / (sigma * sigma)) / sigma;
        }

        /// <summary>
        /// Finds the guassian blur kernel size where the magnitude of the gaussian distribution within the kernel
        /// is greater than or equal to cutoffThreshold times the maximum of the distribution.
        /// </summary>
        /// <param name="sigma">Standard deviation of the distribution.</param>
        /// <param name="cutoffThreshold">The threshold that defines the kernel size.</param>
        /// <returns>The size of the kernel satisfying the requested threshold.</returns>
        public static int KernelSize(float sigma, float cutoffThreshold = 0.1f)
        {
            if (sigma == 0)
                return 0;

            const int max_radius = 200;

            double center = EvalGaussian(0, sigma);
            double threshold = cutoffThreshold * center;
            for (int i = 0; i < max_radius; ++i)
                if (EvalGaussian(i, sigma) < threshold)
                    return Math.Max(i - 1, 0);

            return max_radius;
        }
    }
}
