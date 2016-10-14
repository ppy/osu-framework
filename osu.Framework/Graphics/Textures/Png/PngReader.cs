// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace osu.Framework.Graphics.Textures.Png
{
    public class PngReader
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        private int bitsPerSample;
        private int bytesPerSample;
        private int bytesPerPixel;
        private int bytesPerScanline;
        private IList<PngChunk> chunks;
        private IList<PngChunk> dataChunks;
        private ColorType colorType;
        private Palette palette;
        private byte[] data;
        
        public PngReader()
        {
            chunks = new List<PngChunk>();
            dataChunks = new List<PngChunk>();
        }

        /// <summary>
        /// Returns RGBA pixels.
        /// </summary>
        public byte[] Read(Stream inputStream)
        {
            if (IsPngImage(inputStream) == false)
            {
                throw new Exception("File does not have PNG signature.");
            }

            inputStream.Position = 8;

            while (inputStream.Position != inputStream.Length)
            {
                byte[] chunkDataLengthBytes = new byte[4];
                inputStream.Read(chunkDataLengthBytes, 0, 4);
                uint chunkDataLength = chunkDataLengthBytes.ToUInt();

                inputStream.Position -= 4;

                byte[] chunkBytes = new byte[12 + chunkDataLength];
                inputStream.Read(chunkBytes, 0, (int)(12 + chunkDataLength));

                ProcessChunk(chunkBytes);
            }

            UnpackDataChunks();
            
            return data;
        }

        public static bool IsPngImage(Stream stream)
        {
            stream.Position = 0;
            
            byte[] signature = new byte[8];
            stream.Read(signature, 0, 8);

            bool result = signature.SequenceEqual(HeaderChunk.PngSignature);

            stream.Position = 0;

            return result;
        }

        private void ProcessChunk(byte[] chunkBytes)
        {
            string chunkType = PngChunk.GetChunkTypeString(chunkBytes.Skip(4).Take(4).ToArray());

            switch (chunkType)
            {
                case "IHDR":

                    var headerChunk = new HeaderChunk();
                    headerChunk.Decode(chunkBytes);
                    Width = (int)headerChunk.Width;
                    Height = (int)headerChunk.Height;
                    bitsPerSample = (int)headerChunk.BitDepth;
                    colorType = headerChunk.ColorType;
                    chunks.Add(headerChunk);

                    break;

                case "PLTE":

                    var paletteChunk = new PaletteChunk();
                    paletteChunk.Decode(chunkBytes);
                    palette = paletteChunk.Palette;
                    chunks.Add(paletteChunk);

                    break;

                case "tRNS":

                    var transparencyChunk = new TransparencyChunk();
                    transparencyChunk.Decode(chunkBytes);
                    palette.AddAlphaToColors(transparencyChunk.PaletteTransparencies);
                    break;

                case "IDAT":

                    var dataChunk = new DataChunk();
                    dataChunk.Decode(chunkBytes);
                    dataChunks.Add(dataChunk);

                    break;

                default:
                    break;
            }
        }

        private void UnpackDataChunks()
        {
            var dataByteList = new List<byte>();

            foreach (var dataChunk in dataChunks)
            {
                if (dataChunk.Type == "IDAT")
                {
                    dataByteList.AddRange(dataChunk.Data);
                }
            }

            var compressedStream = new MemoryStream(dataByteList.ToArray());
            var decompressedStream = new MemoryStream();

            try
            {
                using (var deflateStream = new ZlibStream(compressedStream, CompressionMode.Decompress))
                {
                    deflateStream.CopyTo(decompressedStream);
                }
            }
            catch (Exception exception)
            {
                throw new Exception("An error occurred during DEFLATE decompression.", exception);
            }

            var decompressedBytes = decompressedStream.ToArray();
            var pixelData = DeserializePixelData(decompressedBytes);

            DecodePixelData(pixelData);
        }

        private byte[][] DeserializePixelData(byte[] pixelData)
        {
            bytesPerPixel = CalculateBytesPerPixel();
            bytesPerSample = bitsPerSample / 8;
            bytesPerScanline = (bytesPerPixel * Width) + 1;
            int scanlineCount = pixelData.Length / bytesPerScanline;

            if (pixelData.Length % bytesPerScanline != 0)
            {
                throw new Exception("Malformed pixel data - total length of pixel data not multiple of ((bytesPerPixel * width) + 1)");
            }

            var result = new byte[scanlineCount][];

            for (int y = 0; y < scanlineCount; y++)
            {
                result[y] = new byte[bytesPerScanline];
                
                for (int x = 0; x < bytesPerScanline; x++)
                {
                    result[y][x] = pixelData[y * bytesPerScanline + x];
                }
            }
            
            return result;
        }

        private void DecodePixelData(byte[][] pixelData)
        {
            data = new byte[Width * Height * 4];
            
            byte[] previousScanline = new byte[bytesPerScanline];

            for (int y = 0; y < Height; y++)
            {
                var scanline = pixelData[y];

                FilterType filterType = (FilterType)scanline[0];
                byte[] defilteredScanline;

                switch (filterType)
                {
                    case FilterType.None:

                        defilteredScanline = NoneFilter.Decode(scanline);

                        break;

                    case FilterType.Sub:

                        defilteredScanline = SubFilter.Decode(scanline, bytesPerPixel);

                        break;

                    case FilterType.Up:

                        defilteredScanline = UpFilter.Decode(scanline, previousScanline);

                        break;

                    case FilterType.Average:

                        defilteredScanline = AverageFilter.Decode(scanline, previousScanline, bytesPerPixel);

                        break;

                    case FilterType.Paeth:

                        defilteredScanline = PaethFilter.Decode(scanline, previousScanline, bytesPerPixel);

                        break;

                    default:
                        throw new Exception("Unknown filter type.");
                }

                previousScanline = defilteredScanline;
                ProcessDefilteredScanline(defilteredScanline, y);
            }
        }

        private void ProcessDefilteredScanline(byte[] defilteredScanline, int y)
        {
            switch (colorType)
            {
                case ColorType.Grayscale:

                    for (int x = 0; x < Width; x++)
                    {
                        int offset = 1 + (x * bytesPerPixel);

                        byte intensity = defilteredScanline[offset];

                        data[(y * Width * 4) + x * 4 + 0] = intensity;
                        data[(y * Width * 4) + x * 4 + 1] = intensity;
                        data[(y * Width * 4) + x * 4 + 2] = intensity;
                        data[(y * Width * 4) + x * 4 + 3] = 255;
                    }

                    break;

                case ColorType.GrayscaleWithAlpha:

                    for (int x = 0; x < Width; x++)
                    {
                        int offset = 1 + (x * bytesPerPixel);

                        byte intensity = defilteredScanline[offset];
                        byte alpha = defilteredScanline[offset + bytesPerSample];

                        data[(y * Width * 4) + x * 4 + 0] = intensity;
                        data[(y * Width * 4) + x * 4 + 1] = intensity;
                        data[(y * Width * 4) + x * 4 + 2] = intensity;
                        data[(y * Width * 4) + x * 4 + 3] = alpha;
                    }

                    break;

                case ColorType.Palette:

                    for (int x = 0; x < Width; x++)
                    {
                        var pixelColor = palette[defilteredScanline[x + 1]];

                        data[(y * Width * 4) + x * 4 + 0] = pixelColor.R;
                        data[(y * Width * 4) + x * 4 + 1] = pixelColor.G;
                        data[(y * Width * 4) + x * 4 + 2] = pixelColor.B;
                        data[(y * Width * 4) + x * 4 + 3] = pixelColor.A;
                    }

                    break;

                case ColorType.Rgb:

                    for (int x = 0; x < Width; x++)
                    {
                        int offset = 1 + (x * bytesPerPixel);
                        
                        byte red = defilteredScanline[offset];
                        byte green = defilteredScanline[offset + bytesPerSample];
                        byte blue = defilteredScanline[offset + 2 * bytesPerSample];

                        data[(y * Width * 4) + x * 4 + 0] = red;
                        data[(y * Width * 4) + x * 4 + 1] = green;
                        data[(y * Width * 4) + x * 4 + 2] = blue;
                        data[(y * Width * 4) + x * 4 + 3] = 255;
                    }

                    break;

                case ColorType.RgbWithAlpha:

                    for (int x = 0; x < Width; x++)
                    {
                        int offset = 1 + (x * bytesPerPixel);

                        byte red = defilteredScanline[offset];
                        byte green = defilteredScanline[offset + bytesPerSample];
                        byte blue = defilteredScanline[offset + 2 * bytesPerSample];
                        byte alpha = defilteredScanline[offset + 3 * bytesPerSample];
                        
                        data[(y * Width * 4) + x * 4 + 0] = red;
                        data[(y * Width * 4) + x * 4 + 1] = green;
                        data[(y * Width * 4) + x * 4 + 2] = blue;
                        data[(y * Width * 4) + x * 4 + 3] = alpha;
                    }

                    break;

                default:
                    break;
            }
        }

        private int CalculateBytesPerPixel()
        {
            switch (colorType)
            {
                case ColorType.Grayscale:
                    return bitsPerSample / 8;

                case ColorType.GrayscaleWithAlpha:
                    return (2 * bitsPerSample) / 8;

                case ColorType.Palette:
                    return bitsPerSample / 8;

                case ColorType.Rgb:
                    return (3 * bitsPerSample) / 8;

                case ColorType.RgbWithAlpha:
                    return (4 * bitsPerSample) / 8;

                default:
                    throw new Exception("Unknown color type.");
            }
        }
    }
}