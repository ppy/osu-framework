// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Framework.Text
{
    /// <summary>
    /// Configuration for handling combining characters in text rendering.
    /// </summary>
    public class CombiningCharacter
    {
        /// <summary>
        /// Gets the default configuration for Thai language combining characters.
        /// </summary>
        public static CombiningCharacter Thai { get; } = new CombiningCharacter
        {
            UpperCombiningRanges = new List<CharacterRange>
            {
                new CharacterRange('\u0E31', '\u0E31'),  // ไม้หันอากาศ ( ◌ั )
                new CharacterRange('\u0E34', '\u0E37'),  // สระอิ อี อึ อื ( ◌ิ ◌ี ◌ึ ◌ื )
                new CharacterRange('\u0E47', '\u0E4E'),  // ไม้ไต่คู้ วรรณยุกต์ และเครื่องหมายเหนือตัวอักษร ( ◌็ ◌่ ◌้ ◌๊ ◌๋ )
            },
            LowerCombiningRanges = new List<CharacterRange>
            {
                new CharacterRange('\u0E38', '\u0E3A'),  // สระอุ อู พินทุ ( ◌ุ ◌ู ◌ฺ )
            },
            StackingOffset = 0.20f
        };

        /// <summary>
        /// Gets an empty configuration with no combining character rules.
        /// </summary>
        public static CombiningCharacter None { get; } = new CombiningCharacter();

        /// <summary>
        /// The ranges of characters that are upper combining marks (appear above the base character).
        /// </summary>
        public List<CharacterRange> UpperCombiningRanges { get; init; } = new List<CharacterRange>();

        /// <summary>
        /// The ranges of characters that are lower combining marks (appear below the base character).
        /// </summary>
        public List<CharacterRange> LowerCombiningRanges { get; init; } = new List<CharacterRange>();

        /// <summary>
        /// The vertical offset multiplier for stacked combining marks.
        /// This value is multiplied by the font size to determine the spacing between stacked marks.
        /// Default is 0.20 (20% of font size).
        /// </summary>
        public float StackingOffset { get; init; } = 0.20f;

        /// <summary>
        /// Checks if a character is an upper combining mark.
        /// </summary>
        /// <param name="character">The character to check.</param>
        /// <returns>True if the character is an upper combining mark.</returns>
        public bool IsUpperCombining(char character)
        {
            foreach (var range in UpperCombiningRanges)
            {
                if (character >= range.Start && character <= range.End)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a character is a lower combining mark.
        /// </summary>
        /// <param name="character">The character to check.</param>
        /// <returns>True if the character is a lower combining mark.</returns>
        public bool IsLowerCombining(char character)
        {
            foreach (var range in LowerCombiningRanges)
            {
                if (character >= range.Start && character <= range.End)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a character is any type of combining mark (upper or lower).
        /// </summary>
        /// <param name="character">The character to check.</param>
        /// <returns>True if the character is a combining mark.</returns>
        public bool IsCombining(char character) => IsUpperCombining(character) || IsLowerCombining(character);
    }

    /// <summary>
    /// Represents a range of Unicode characters.
    /// </summary>
    public readonly struct CharacterRange
    {
        /// <summary>
        /// The start character of the range (inclusive).
        /// </summary>
        public char Start { get; }

        /// <summary>
        /// The end character of the range (inclusive).
        /// </summary>
        public char End { get; }

        /// <summary>
        /// Creates a new character range.
        /// </summary>
        /// <param name="start">The start character (inclusive).</param>
        /// <param name="end">The end character (inclusive).</param>
        public CharacterRange(char start, char end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// Creates a new character range for a single character.
        /// </summary>
        /// <param name="character">The character.</param>
        public CharacterRange(char character)
            : this(character, character)
        {
        }
    }
}
