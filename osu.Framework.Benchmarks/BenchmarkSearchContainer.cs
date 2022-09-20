// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkSearchContainer : BenchmarkTest
    {
        private static Random random = new Random();
        private string[] searchTerms = null!;
        private string searchableTexts = null!;
        private const int test_numbers = 1000;
        private const string chars = "ABCDEFGHIJKL MNOPQRSTUVWXYZ 0123456789 ĄĆĘŁŃÓŚŹŻ abcdefghijkl mnopqrstuvwxyz ąćęłńóśźż ";

        public override void SetUp()
        {
            base.SetUp();

            searchTerms = Enumerable.Range(0, test_numbers)
                                        .Select(_ => RandomString(20))
                                        .ToArray();

            searchableTexts = RandomString(1000);
        }

        [Benchmark]
        public void SearchIgnoreNonSpace()
        {
            foreach (var searchTerm in searchTerms)
                checkTerm(searchableTexts, searchTerm, false, true);
        }

        [Benchmark]
        public void SearchWithNonSpace()
        {
            foreach (var searchTerm in searchTerms)
                checkTerm(searchableTexts, searchTerm, false, false);
        }

        [Benchmark]
        public void SearchNonContiguousIgnoreNonSpace()
        {
            foreach (var searchTerm in searchTerms)
                checkTerm(searchableTexts, searchTerm, true, true);
        }

        [Benchmark]
        public void SearchNonContiguousWithNonSpace()
        {
            foreach (var searchTerm in searchTerms)
                checkTerm(searchableTexts, searchTerm, true, false);
        }

        private static string RandomString(int length)
        {
            return new string(Enumerable.Repeat(chars, length)
                            .Select(s => s[random.Next(s.Length)])
                            .ToArray());
        }
        private static bool checkTerm(string haystack, string needle, bool nonContiguous, bool ignoreNonSpaceCharacters)
        {
            var compareOptions = ignoreNonSpaceCharacters
                ? CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase
                : CompareOptions.OrdinalIgnoreCase;

            if (!nonContiguous)
            {
                if (ignoreNonSpaceCharacters)
                    return CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle, compareOptions) >= 0;

                return haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
            }

            int index = 0;

            for (int i = 0; i < needle.Length; i++)
            {
                int found = CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle[i], index, compareOptions);
                if (found < 0)
                    return false;

                index = found + 1;
            }

            return true;
        }
    }
}
