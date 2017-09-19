// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Text;
using System.Linq;
using osu.Framework.Graphics.Sprites;
using System;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container where you can add text and specify how it should be formatted
    /// </summary>
    /// <typeparam name="T">A type that the derived class uses to represent markers</typeparam>
    public abstract class FormattedTextFlowContainer<T> : TextFlowContainer
    {
        /// <summary>
        /// Contains strings that tell the parser what a marker is
        /// </summary>
        protected abstract Dictionary<string, T> MarkerDelimeters { get; }

        /// <summary>
        /// If a marker needs both a start and an end or if it should continue until the end if not closed
        /// </summary>
        protected virtual bool MarkerNeedsEnd => true;

        /// <summary>
        /// Overrides any existing text and adds the new one with <see cref="AddFormattedText(string, Action{SpriteText})"/>
        /// </summary>
        public string FormattedText
        {
            set
            {
                Clear();
                AddFormattedText(value);
            }
        }

        /// <summary>
        /// Is called everytime a formatted text gets added to format it
        /// </summary>
        /// <param name="markers">A list of markers that tell the implementation how it should format the text</param>
        /// <param name="text">The <see cref="SpriteText"/> to format</param>
        protected abstract void FormatText(List<T> markers, SpriteText text);

        /// <summary>
        /// Add new formatted text to this text flow. The \n character will create a new paragraph, not just a line break. If you need \n to be a line break, use <see cref="AddFormattedParagraph(string, Action{SpriteText})"/> instead.
        /// </summary>
        /// <returns>A collection of the <see cref="SpriteText" /> objects for each word created from the given text.</returns>
        /// <param name="text">The formatted text to add.</param>
        /// <param name="creationParameters">A callback providing any <see cref="SpriteText" /> instances created for this new text.</param>
        public IEnumerable<SpriteText> AddFormattedText(string text, Action<SpriteText> creationParameters = null) => addFormattedLine(text, creationParameters, true);

        /// <summary>
        /// Add a new formatted paragraph to this text flow. The \n character will create a line break. If you need \n to be a new paragraph, not just a line break, use <see cref="AddFormattedText(string, Action{SpriteText})"/> instead.
        /// </summary>
        /// <returns>A collection of the <see cref="SpriteText" /> objects for each word created from the given text.</returns>
        /// <param name="paragraph">The formatted paragraph to add.</param>
        /// <param name="creationParameters">A callback providing any <see cref="SpriteText" /> instances created for this new paragraph.</param>
        public IEnumerable<SpriteText> AddFormattedParagraph(string paragraph, Action<SpriteText> creationParameters = null) => addFormattedLine(paragraph, creationParameters, false);


        private IEnumerable<SpriteText> addFormattedLine(string text, Action<SpriteText> creationParameter, bool paragraph)
        {
            List<SpriteText> lines = new List<SpriteText>();

            //Select the right function dependeping on if a paragraph should be added
            var addLineAction = paragraph ? AddText : (Func<string, Action<SpriteText>, IEnumerable<SpriteText>>)AddParagraph;

            Dictionary<T, bool>  markerActive = MarkerDelimeters.Values.ToDictionary(type => type, type => false);

            List<SplitMarker> markers = new List<SplitMarker>();
            foreach (KeyValuePair<string, T> pair in MarkerDelimeters)
                markers.AddRange(parseSplitMarkers(ref text, pair.Key, pair.Value, MarkerNeedsEnd));

            // Add a sentinel marker for the end of the string such that the entire string is rendered
            // without requiring code duplication.
            markers.Add(new SplitMarker { Index = text.Length, Length = 0 });

            // Sort markers from earliest to latest
            markers.Sort();

            // Cut up string into parts according to all found markers
            int lastIndex = 0;
            foreach (var marker in markers)
            {
                // We do not need to add empty strings if we have 2 consecutive markers
                if (lastIndex < marker.Index)
                    lines.AddRange(addLineAction(text.Substring(lastIndex, marker.Index - lastIndex), sprite => FormatText(markerActive.Where(pair => pair.Value).Select(pair => pair.Key).ToList(), sprite)));

                lastIndex = marker.Index + marker.Length;

                //Check if this isn't the decoy marker
                if(marker.Length != 0)
                    // Switch marker type state that we just encountered.
                    markerActive[marker.Type] ^= true;
            }

            if(creationParameter != null)
                lines.ForEach(creationParameter);

            return lines;
        }

        private struct SplitMarker : IComparable<SplitMarker>
        {
            public int Index;
            public int Length;
            public T Type;

            public int CompareTo(SplitMarker other) => Index.CompareTo(other.Index);
        }

        private static List<SplitMarker> parseSplitMarkers(ref string toParse, string delimiter, T type, bool needsEnd)
        {
            List<SplitMarker> escapeMarkers = new List<SplitMarker>();
            List<SplitMarker> delimiterMarkers = new List<SplitMarker>();

            // The output string will contain toParse with all successfully parsed
            // delimiters replaced by spaces.
            StringBuilder outputString = new StringBuilder(toParse);

            // For each char in toParse...
            for (int i = 0; i < toParse.Length; i++)
            {
                // ...check whether delimiter is matched char-by-char.
                for (int j = 0; j + i < toParse.Length && j < delimiter.Length; j++)
                {
                    if (toParse[j + i] != delimiter[j])
                        break;
                    else if (j == delimiter.Length - 1)
                    {
                        // Were we escaped? In this case put a marker skipping the escape character
                        if (i > 0 && toParse[i - 1] == '\\')
                            escapeMarkers.Add(new SplitMarker { Index = i - 1, Type = default(T), Length = 1 });
                        else
                        {
                            delimiterMarkers.Add(new SplitMarker { Index = i, Type = type, Length = delimiter.Length });

                            // Replace parsed delimiter with spaces such that future delimiters which may be substrings
                            // do not parse a second time. One specific usecase are ** and * for markdown.
                            for (int k = i; k < i + delimiter.Length; ++k)
                                outputString[k] = ' ';

                            // Make sure we advance beyond the end of the discovered delimiter
                            i += delimiter.Length - 1;
                        }
                    }
                }
            }

            // Disregard trailing marker if we have an odd amount
            if (needsEnd && delimiterMarkers.Count % 2 == 1)
            {
                SplitMarker marker = delimiterMarkers[delimiterMarkers.Count - 1];
                outputString.Replace(new string(' ', marker.Length), delimiter, marker.Index, 1);
                delimiterMarkers.RemoveAt(delimiterMarkers.Count - 1);
            }

            toParse = outputString.ToString();

            // Return a single list containing all markers
            escapeMarkers.AddRange(delimiterMarkers);
            return escapeMarkers;
        }
    }
}
