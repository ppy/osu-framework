// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Platform.MacOS.Native
{
    public enum NSStringEncoding : uint
    {
        /// <summary>
        /// 7-bit ASCII encoding within 8 bit chars.
        /// </summary>
        ASCII = 1,

        /// <summary>
        /// 8-bit ASCII encoding with NEXTSTEP extensions.
        /// </summary>
        NEXTSTEP = 2,

        /// <summary>
        /// 8-bit EUC encoding for Japanese text.
        /// </summary>
        JapaneseEUC = 3,

        /// <summary>
        /// 8-bit representation of Unicode characters.
        /// </summary>
        UTF8 = 4,

        /// <summary>
        /// 8-bit ISO Latin 1 encoding.
        /// </summary>
        ISOLatin1 = 5,

        /// <summary>
        /// 8-bit Adobe Symbol encoding vector.
        /// </summary>
        Symbol = 6,

        /// <summary>
        /// 7-bit verbose ASCII to represent all Unicode characters.
        /// </summary>
        NonLossyASCII = 7,

        /// <summary>
        /// 8-bit Shift-JIS encoding for Japanese text.
        /// </summary>
        ShiftJIS = 8,

        /// <summary>
        /// 8-bit ISO Latin 2 encoding.
        /// </summary>
        ISOLatin2 = 9,

        /// <summary>
        /// The canonical Unicode encoding.
        /// </summary>
        Unicode = 10,

        /// <summary>
        /// Microsoft Windows codepage 1251, encoding Cyrillic characters.
        /// </summary>
        WindowsCP1251 = 11,

        /// <summary>
        /// Microsoft Windows codepage 1252, encoding Latin 1 characters.
        /// </summary>
        WindowsCP1252 = 12,

        /// <summary>
        /// Microsoft Windows codepage 1253, encoding Greek characters.
        /// </summary>
        WindowsCP1253 = 13,

        /// <summary>
        /// Microsoft Windows codepage 1254, encoding Turkish characters.
        /// </summary>
        WindowsCP1254 = 14,

        /// <summary>
        /// Microsoft Windows codepage 1250, encoding Latin 2 characters.
        /// </summary>
        WindowsCP1250 = 15,

        /// <summary>
        /// ISO 2022 Japanese encoding.
        /// </summary>
        ISO2022JP = 21,

        /// <summary>
        /// Classing Macintosh Roman encoding.
        /// </summary>
        MacOSRoman = 30,

        /// <summary>
        /// 16-bit UTF encoding.
        /// </summary>
        UTF16 = Unicode,

        /// <summary>
        /// <see cref="UTF16"/> encoding with explicit endianness.
        /// </summary>
        UTF16BigEndian = 0x90000100,

        /// <summary>
        /// <see cref="UTF16"/> encoding with explicit endianness.
        /// </summary>
        UTF16LittleEndian = 0x94000100,

        /// <summary>
        /// 32-bit UTF encoding.
        /// </summary>
        UTF32 = 0x8c000100,

        /// <summary>
        /// <see cref="UTF32"/> encoding with explicit endianness.
        /// </summary>
        UTF32BigEndian = 0x98000100,

        /// <summary>
        /// <see cref="UTF32"/> encoding with explicit endianness.
        /// </summary>
        UTF32LittleEndian = 0x9c000100
    }
}
