// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FreeTypeSharp;
using osu.Framework.Extensions;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using static FreeTypeSharp.FT;
using static FreeTypeSharp.FT_Error;
using static FreeTypeSharp.FT_FACE_FLAG;
using static FreeTypeSharp.FT_Kerning_Mode_;
using static FreeTypeSharp.FT_LOAD;

namespace osu.Framework.Text
{
    /// <summary>
    /// Handles outline fonts using FreeType.
    /// </summary>
    public class OutlineFont : IDisposable
    {
        /// <summary>
        /// An instance of the FreeType library.
        /// </summary>
        private static readonly unsafe FT_LibraryRec_* library;

        /// <summary>
        /// Locks <see cref="library"/> for opening and closing fonts.
        /// </summary>
        private static readonly object library_lock = new object();

        /// <summary>
        /// The underlying unmanaged font handle.
        /// </summary>
        private unsafe FT_FaceRec_* face => (FT_FaceRec_*)completionSource.Task.GetResultSafely();

        /// <summary>
        /// Locks <see cref="face"/> and its glyph slot for exclusive access.
        /// </summary>
        private readonly object faceLock = new object();

        private readonly TaskCompletionSource<nint> completionSource = new TaskCompletionSource<nint>();

        private readonly Dictionary<string, uint> axes = new Dictionary<string, uint>();

        private readonly Dictionary<string, uint> namedInstances = new Dictionary<string, uint>();

        /// <summary>
        /// The name of the underlying asset.
        /// </summary>
        public string AssetPath { get; }

        public string AssetName => AssetPath.Split('/').Last();

        /// <summary>
        /// The index of the face to use.
        /// </summary>
        public int FaceIndex { get; }

        /// <summary>
        /// The resolution of the rendered glyphs in pixels per line.
        /// </summary>
        public uint Resolution { get; init; } = 100;

        public float Baseline => 84.0f;

        protected readonly ResourceStore<byte[]> Store;

        /// <summary>
        /// Initialize FreeType for loading outline fonts.
        /// </summary>
        /// <exception cref="FreeTypeException">FreeType failed to initialize.</exception>
        static unsafe OutlineFont()
        {
            FT_Error error;

            fixed (FT_LibraryRec_** pp = &library)
            {
                error = FT_Init_FreeType(pp);
            }

            if (error != FT_Err_Ok) throw new FreeTypeException(error);
        }

        /// <summary>
        /// Open an outline font.
        /// </summary>
        /// <param name="store">The resource store to use.</param>
        /// <param name="assetName">The font to open.</param>
        /// <param name="faceIndex">The index of the face to use.</param>
        public OutlineFont(IResourceStore<byte[]> store, string assetName, int faceIndex = 0)
        {
            Store = new ResourceStore<byte[]>(store);

            Store.AddExtension("ttf");
            Store.AddExtension("otf");
            Store.AddExtension("woff");
            Store.AddExtension("ttc");

            AssetPath = assetName;
            FaceIndex = faceIndex;
        }

        ~OutlineFont()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual unsafe void Dispose(bool isDisposing)
        {
            completionSource.Task.WaitSafely();

            if (completionSource.Task.IsCompletedSuccessfully)
            {
                lock (library_lock)
                {
                    FT_Done_Face(face);
                }
            }
        }

        /// <summary>
        /// Load the requested font.
        /// </summary>
        /// <exception cref="FileNotFoundException">The requested font does not exist.</exception>
        /// <exception cref="FreeTypeException">FreeType refused to open the font.</exception>
        public Task LoadAsync() => Task.Run(async () =>
        {
            // check if the requested font is loading or loaded
            if (completionSource.Task.IsCompleted || !Monitor.TryEnter(faceLock))
            {
                await completionSource.Task.ConfigureAwait(false);
                return;
            }

            try
            {
                // check again to prevent race conditions
                if (!completionSource.Task.IsCompleted)
                    completionSource.SetResult(loadFont());
            }
            catch (Exception)
            {
                completionSource.SetResult(0);
                throw;
            }
            finally
            {
                Monitor.Exit(faceLock);
            }
        });

        private unsafe nint loadFont()
        {
            Stream s = Store.GetStream(AssetPath) ?? throw new FileNotFoundException();
            var handle = GCHandle.Alloc(s);

            FT_StreamRec_* ftStream;
            FT_FaceRec_* native = null;

            // set up unmanaged object to allow use of stream from FreeType
            try
            {
                ftStream = (FT_StreamRec_*)NativeMemory.AllocZeroed((nuint)sizeof(FT_StreamRec_));
            }
            catch (Exception)
            {
                s.Dispose();
                handle.Free();
                throw;
            }

            ftStream->size = new CULong((nuint)s.Length);
            ftStream->descriptor.pointer = (void*)(nint)handle;
            ftStream->read = &streamReadCallback;
            ftStream->close = &streamCloseCallback;

            // open the font
            var openArgs = new FT_Open_Args_
            {
                flags = 0x2, // FT_OPEN_STREAM
                stream = ftStream,
                num_params = 0,
            };

            try
            {
                FT_Error error;

                lock (library_lock)
                {
                    error = FT_Open_Face(library, &openArgs, new CLong(FaceIndex), &native);
                }

                if (error != 0) throw new FreeTypeException(error);

                // ReSharper disable CSharpWarnings::CS8603

                // apply fixed scale relative to em box, as a compromise between
                // CSS-compatible sizing and existing BMFont usage
                uint emResolution = (uint)Math.Ceiling(Resolution / 1.2);
                error = FT_Set_Pixel_Sizes(native, 0, emResolution);

                if (error != 0) throw new FreeTypeException(error);

                if (((FT_FACE_FLAG)native->face_flags.Value).HasFlagFast(FT_FACE_FLAG_MULTIPLE_MASTERS))
                {
                    loadVariableFontData(native);
                }

                return (nint)native;
                // ReSharper restore CSharpWarnings::CS8603
            }
            catch (Exception)
            {
                // At this point FreeType owns all unmanaged resources allocated above, and
                // FT_Done_Face should release them all.
                if (native is not null)
                {
                    lock (library_lock)
                    {
                        FT_Done_Face(native);
                    }
                }

                throw;
            }
        }

        /// <summary>
        /// Stream read and seek callback used by FreeType.
        /// </summary>
        [UnmanagedCallersOnly]
        private static unsafe CULong streamReadCallback(FT_StreamRec_* ftStream, CULong offset, byte* buffer, CULong count)
        {
            try
            {
                var s = (Stream)((GCHandle)(nint)ftStream->descriptor.pointer).Target!;

                s.Seek((long)offset.Value, SeekOrigin.Begin);

                if (count.Value != 0)
                {
                    return new CULong((uint)s.Read(new Span<byte>(buffer, (int)count.Value)));
                }
                else
                {
                    // Caller expects seek only operations return 0 on success.
                    return new CULong(0);
                }
            }
            catch (Exception)
            {
                // The caller excepts status to be reported in the return value
                // instead of as an exception. It expects non-zero to be returned
                // for failed seek-only operations, and zero for failed seek-and-
                // read operations.
                return new CULong(count.Value == 0 ? 1u : 0);
            }
        }

        /// <summary>
        /// Stream close callback used by FreeType.
        /// </summary>
        [UnmanagedCallersOnly]
        private static unsafe void streamCloseCallback(FT_StreamRec_* ftStream)
        {
            var handle = (GCHandle)(nint)ftStream->descriptor.pointer;

            var s = (Stream?)handle.Target;

            handle.Free();
            s?.Dispose();

            // FreeType allows a `FT_Stream *` to free itself here.
            NativeMemory.Free(ftStream);
        }

        private unsafe void loadVariableFontData(FT_FaceRec_* face)
        {
            FT_MM_Var_* amaster;
            FT_Error error = FT_Get_MM_Var(face, &amaster);

            if (error != 0) throw new FreeTypeException(error);

            Span<byte> tag = stackalloc byte[4];

            // enumerate variable axes
            for (uint i = 0; i < amaster->num_axis; ++i)
            {
                FT_Var_Axis_* axis = &amaster->axis[i];
                nuint t = axis->tag.Value;
                tag[0] = (byte)(t >> 24);
                tag[1] = (byte)(t >> 16);
                tag[2] = (byte)(t >> 8);
                tag[3] = (byte)t;
                axes.Add(Encoding.ASCII.GetString(tag), i);
            }

            // load SFNT names
            var nameRecords = new Dictionary<uint, string>();
            var nameEntry = new FT_SfntName_();
            uint nameCount = FT_Get_Sfnt_Name_Count(face);

            for (uint i = 0; i < nameCount; ++i)
            {
                error = FT_Get_Sfnt_Name(face, i, &nameEntry);

                if (error != 0) throw new FreeTypeException(error);

                string? name = decodeNameEntry(&nameEntry);

                if (name is not null)
                    nameRecords[nameEntry.name_id] = name;
            }

            string fontName = nameRecords.GetValueOrDefault(16u) ?? nameRecords[1];

            // get names for named styles
            for (uint i = 0; i < amaster->num_namedstyles; ++i)
            {
                FT_Var_Named_Style_* namedStyle = &amaster->namedstyle[i];

                if (namedStyle->psid != 0xffff)
                {
                    // try to get the instance's PostScript name first
                    namedInstances.Add(nameRecords[namedStyle->psid], i);
                }
                else
                {
                    // failing that, generate one according to
                    // <https://download.macromedia.com/pub/developer/opentype/tech-notes/5902.AdobePSNameGeneration.html>.
                    string s = alphanumerify(nameRecords[namedStyle->strid]);
                    namedInstances.Add($@"{alphanumerify(fontName)}-{s}", i);
                }
            }

            lock (library_lock)
            {
                FT_Done_MM_Var(library, amaster);
            }

            Logger.Log($"Variable axes in {AssetName}: {string.Join(", ", axes.Keys)}", level: LogLevel.Debug);
            Logger.Log($"Named instances in {AssetName}: {string.Join(", ", namedInstances.Keys)}", level: LogLevel.Debug);

            static string alphanumerify(string s)
            {
                var result = new StringBuilder();

                foreach (char c in s)
                {
                    if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                        result.Append(c);
                }

                return result.ToString();
            }
        }

        /// <summary>
        /// Decode a name entry string.
        /// </summary>
        /// <param name="nameEntry">The name entry to decode.</param>
        /// <returns>The name entry in UTF-16.</returns>
        /// <exception cref="InvalidDataException">
        /// The name entry encoding cannot be recognized.
        /// </exception>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name"/>
        private static unsafe string? decodeNameEntry(FT_SfntName_* nameEntry)
        {
            var span = new ReadOnlySpan<byte>(nameEntry->@string, (int)nameEntry->string_len);

            switch (nameEntry->platform_id)
            {
                case TT_PLATFORM_APPLE_UNICODE:
                    // > Strings for the Unicode platform must be encoded
                    // > in UTF-16BE.
                    return Encoding.BigEndianUnicode.GetString(span);

                case TT_PLATFORM_MICROSOFT:
                    // > If a font has records for encoding IDs 3, 4 or 5,
                    // > the corresponding string data should be encoded
                    // > using code pages 936, 950 and 949, respectively.
                    // > Otherwise, all string data for platform 3 must be
                    // > encoded in UTF-16BE.
                    //
                    // The special case where a `name` record with a special
                    // encoding ID (3, 4, 5) is encoded in UTF-16BE is not
                    // supported.
                    switch (nameEntry->encoding_id)
                    {
                        case 3: // GBK
                            return Encoding.GetEncoding(936).GetString(span);

                        case 4: // Big5
                            return Encoding.GetEncoding(950).GetString(span);

                        case 5: // Wansung
                            return Encoding.GetEncoding(949).GetString(span);

                        default:
                            return Encoding.BigEndianUnicode.GetString(span);
                    }

                default:
                    // `name` records for Classic Mac OS and custom encodings
                    // are not supported.
                    return null;
            }
        }

        public RawFontVariation? DecodeFontVariation(FontVariation? variation)
        {
            if (variation is null)
                return null;

            uint rawNamedInstance = 0;
            CLong[] rawAxes = Array.Empty<CLong>();

            if (variation.Axes is not null)
            {
                rawAxes = new CLong[axes.Count];

                foreach (var (axis, value) in variation.Axes)
                {
                    // Non-existent axes for this font should have no effect.
                    if (axes.TryGetValue(axis, out uint index))
                        rawAxes[index] = new CLong((nint)Math.Round(value * 65536));
                }
            }
            else if (variation.NamedInstance is not null)
            {
                // FreeType reserves instance 0 for the default instance; the actual
                // named instance passed into FreeType starts at 1.
                rawNamedInstance = namedInstances[variation.NamedInstance] + 1;
            }

            return new RawFontVariation
            {
                NamedInstance = rawNamedInstance,
                Axes = rawAxes,
            };
        }

        /// <summary>
        /// Get the glyph index of a character.
        /// </summary>
        /// <param name="c">A character.</param>
        /// <returns>
        /// <para>
        /// The character's glyph index, on success.
        /// </para>
        /// <para>
        /// If the font is not loaded, or if the does not contain a glyph for the
        /// character in question, returns 0.
        /// </para>
        /// </returns>
        public unsafe uint GetGlyphIndex(int c)
        {
            if (face is null)
                return 0;

            lock (faceLock)
            {
                return FT_Get_Char_Index(face, new CULong((uint)c));
            }
        }

        /// <summary>
        /// Get the glyph index of a character asynchronously.
        /// </summary>
        /// <param name="c">A character.</param>
        /// <returns>
        /// <para>
        /// The character's glyph index, on success.
        /// </para>
        /// <para>
        /// If the does not contain a glyph for the character in question, returns 0.
        /// </para>
        /// </returns>
        public async Task<uint> GetGlyphIndexAsync(int c)
        {
            nint native = await completionSource.Task.ConfigureAwait(false);

            unsafe
            {
                lock (faceLock)
                {
                    return FT_Get_Char_Index((FT_FaceRec_*)native, new CULong((nuint)c));
                }
            }
        }

        /// <summary>
        /// Whether the font has a glyph for the specified character.
        /// </summary>
        /// <param name="c">A character.</param>
        /// <returns>
        /// If the font has the specified character, returns true. Otherwise,
        /// returns false.
        /// </returns>
        public bool HasGlyph(int c) => GetGlyphIndex(c) != 0;

        /// <summary>
        /// Set the parameters of the font.
        /// </summary>
        /// <param name="face">An unmanaged face object.</param>
        /// <param name="variation">
        /// The parameters of the font. If null, the default parameters will be
        /// used. This parameter must be null if the font is static.
        /// </param>
        /// <exception cref="FreeTypeException">
        /// The font parameters are invalid.
        /// </exception>
        /// <remarks>
        /// This method is not thread safe. The caller is responsible for locking
        /// <see cref="faceLock"/> before calling.
        /// </remarks>
        private static unsafe void setVariation(FT_FaceRec_* face, RawFontVariation? variation)
        {
            if (variation is null)
            {
                // This check is needed to support non-variable fonts.
                if (((FT_FACE_FLAG)face->face_flags.Value).HasFlagFast(FT_FACE_FLAG_MULTIPLE_MASTERS))
                {
                    uint defaultInstance;
                    var error = FT_Get_Default_Named_Instance(face, &defaultInstance);

                    if (error != 0) throw new FreeTypeException(error);

                    error = FT_Set_Named_Instance(face, defaultInstance);

                    if (error != 0) throw new FreeTypeException(error);
                }
            }
            else if (!variation.Axes.IsEmpty)
            {
                FT_Error error;

                fixed (CLong* p = variation.Axes.Span)
                {
                    error = FT_Set_Var_Design_Coordinates(face, (uint)variation.Axes.Length, p);
                }

                if (error != 0) throw new FreeTypeException(error);
            }
            else
            {
                var error = FT_Set_Named_Instance(face, variation.NamedInstance);

                if (error != 0) throw new FreeTypeException(error);
            }
        }

        /// <summary>
        /// Load metrics for a glyph.
        /// </summary>
        /// <param name="glyphIndex">The index of the glyph.</param>
        /// <param name="variation">
        /// The parameters of the font. If null, the default parameters will be
        /// used. This parameter must be null if the font is static.
        /// </param>
        /// <returns>
        /// A new <see cref="CharacterGlyph"/> containing the glyph metrics.
        /// </returns>
        /// <remarks>
        /// The Character property of returned <see cref="CharacterGlyph"/>
        /// is always U+FFFF, as the codepoint for the corresponding character
        /// is not available here. As such, callers with knowledge of the
        /// original code point should construct a new
        /// <see cref="CharacterGlyph"/> from the return value of this method,
        /// with the appropriate character, to get meaningful results from
        /// methods like <see cref="CharacterGlyph.GetKerning{T}(T)"/>.
        /// </remarks>
        public unsafe CharacterGlyph? GetMetrics(uint glyphIndex, RawFontVariation? variation)
        {
            if (face is null)
                return null;

            nint horiBearingX;
            nint horiBearingY;
            nint horiAdvance;

            lock (faceLock)
            {
                setVariation(face, variation);
                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                FT_Error error = FT_Load_Glyph(face, glyphIndex, FT_LOAD_NO_BITMAP | FT_LOAD_NO_HINTING);

                if (error != 0)
                {
                    return null;
                }

                horiBearingX = face->glyph->metrics.horiBearingX.Value;
                horiBearingY = face->glyph->metrics.horiBearingY.Value;
                horiAdvance = face->glyph->metrics.horiAdvance.Value;
            }

            // FreeType outputs metric data in 26.6 fixed point. Convert to floating point accordingly.
            float xOffset = horiBearingX / 64.0f;
            float yOffset = Baseline - (horiBearingY / 64.0f);
            float advance = horiAdvance / 64.0f;

            // The noncharacter indicates that the original character is not available.
            return new CharacterGlyph('\uffff', xOffset, yOffset, advance, Baseline, null);
        }

        /// <summary>
        /// Get the kerning between two glyphs.
        /// </summary>
        /// <param name="left">The glyph index of the left glyph.</param>
        /// <param name="right">The glyph index of the right glyph.</param>
        /// <param name="variation">
        /// The parameters of the font. If null, the default parameters will be
        /// used. This parameter must be null if the font is static.
        /// </param>
        /// <returns>The amount of kerning.</returns>
        public unsafe int GetKerning(uint left, uint right, RawFontVariation? variation)
        {
            if (face is null)
                return 0;

            FT_Vector_ kerning;
            FT_Error error;

            lock (faceLock)
            {
                setVariation(face, variation);
                error = FT_Get_Kerning(face, left, right, FT_KERNING_DEFAULT, &kerning);
            }

            if (error != 0) return 0;

            return (int)kerning.x.Value / 64;
        }

        /// <summary>
        /// Rasterize a glyph.
        /// </summary>
        /// <param name="glyphIndex">The index of the glyph.</param>
        /// <param name="variation">
        /// The parameters of the font. If null, the default parameters will be
        /// used. This parameter must be null if the font is static.
        /// </param>
        /// <returns>The rasterized glyph suitable for rendering.</returns>
        /// <exception cref="FreeTypeException">Rasterization of the texture failed.</exception>
        public unsafe TextureUpload? RasterizeGlyph(uint glyphIndex, RawFontVariation? variation)
        {
            if (face is null)
                return null;

            return rasterizeGlyphInner((nint)face, glyphIndex, variation);
        }

        /// <summary>
        /// Rasterize a character asynchronously.
        /// </summary>
        /// <param name="glyphIndex">The index of the glyph.</param>
        /// <param name="variation">
        /// The parameters of the font. If null, the default parameters will be
        /// used. This parameter must be null if the font is static.
        /// </param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The rasterized glyph suitable for rendering.</returns>
        /// <exception cref="AggregateException">The font failed to load.</exception>
        /// <exception cref="FreeTypeException">Rasterization of the texture failed.</exception>
        public async Task<TextureUpload> RasterizeGlyphAsync(uint glyphIndex, RawFontVariation? variation, CancellationToken cancellationToken = default)
        {
            nint native = await completionSource.Task.ConfigureAwait(false);

            return await Task.Run(() => rasterizeGlyphInner(native, glyphIndex, variation), cancellationToken).ConfigureAwait(false);
        }

        private unsafe TextureUpload rasterizeGlyphInner(nint intPtr, uint glyphIndex, RawFontVariation? variation)
        {
            Image<Rgba32> image;
            var native = (FT_FaceRec_*)intPtr;

            // rasterize
            lock (faceLock)
            {
                setVariation(face, variation);

                // ReSharper disable BitwiseOperatorOnEnumWithoutFlags
                FT_Error error = FT_Load_Glyph(native, glyphIndex, FT_LOAD_NO_BITMAP | FT_LOAD_NO_HINTING | FT_LOAD_RENDER);
                // ReSharper restore BitwiseOperatorOnEnumWithoutFlags

                if (error != 0) throw new FreeTypeException(error);

                // copy to TextureUpload
                var ftBitmap = &native->glyph->bitmap;
                int width = ftBitmap->width != 0 ? (int)ftBitmap->width : 1;
                int height = ftBitmap->rows != 0 ? (int)ftBitmap->rows : 1;
                image = new Image<Rgba32>(width, height, new Rgba32(0, 0, 0, byte.MaxValue));

                for (int y = 0; y < ftBitmap->rows; ++y)
                {
                    var srcRow = new ReadOnlySpan<byte>(ftBitmap->buffer + (y * ftBitmap->pitch), ftBitmap->pitch);
                    var dstRow = image.DangerousGetPixelRowMemory(y).Span;

                    for (int x = 0; x < ftBitmap->width; ++x)
                    {
                        dstRow[x] = new Rgba32(byte.MaxValue, byte.MaxValue, byte.MaxValue, srcRow[x]);
                    }
                }
            }

            return new TextureUpload(image);
        }

        private unsafe (uint codePoint, uint glyphIndex) getFirstChar()
        {
            if (face is null)
                return (0, 0);

            uint glyphIndex, codePoint;

            lock (faceLock)
                codePoint = (uint)FT_Get_First_Char(face, &glyphIndex).Value;

            return (codePoint, glyphIndex);
        }

        private unsafe (uint codePoint, uint glyphIndex) getNextChar(uint prevCodePoint)
        {
            if (face is null)
                return (0, 0);

            uint glyphIndex, codePoint;

            lock (faceLock)
                codePoint = (uint)FT_Get_Next_Char(face, new CULong(prevCodePoint), &glyphIndex).Value;

            return (codePoint, glyphIndex);
        }

        /// <summary>
        /// Get the set of characters available in the font.
        /// </summary>
        public IEnumerable<char> GetAvailableChars()
        {
            if (completionSource.Task.GetResultSafely() == 0) yield break;

            (uint codePoint, uint glyphIndex) = getFirstChar();

            while (glyphIndex != 0)
            {
                // Ignore glyphs for characters outside BMP for sanity.
                if (codePoint <= char.MaxValue)
                    yield return (char)codePoint;

                (codePoint, glyphIndex) = getNextChar(codePoint);
            }
        }
    }
}
