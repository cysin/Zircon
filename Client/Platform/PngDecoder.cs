using System;
using System.IO;
using System.IO.Compression;

namespace Client.Platform
{
    /// <summary>
    /// Minimal, dependency-free PNG decoder for the cross-platform (non-Windows) build,
    /// where System.Drawing.Common (GDI+) is unavailable. Decodes the common, non-interlaced
    /// PNG variants used by the game's data files (e.g. Data/Pallete.png) into a tightly-packed
    /// BGRA byte buffer suitable for direct upload via glTexImage2D(GL_BGRA, GL_UNSIGNED_BYTE).
    ///
    /// Supported: bit depth 8, colour types 0 (grey), 2 (RGB), 3 (indexed+PLTE/tRNS),
    /// 4 (grey+alpha), 6 (RGBA). Non-interlaced only. Returns false for anything else so
    /// callers can fall back gracefully.
    /// </summary>
    public static class PngDecoder
    {
        private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

        /// <summary>
        /// Decodes a PNG file into a tightly packed BGRA buffer (4 bytes/pixel, top-down rows).
        /// </summary>
        public static bool TryDecode(string path, out int width, out int height, out byte[] bgra)
        {
            width = 0; height = 0; bgra = null;
            try
            {
                byte[] file = File.ReadAllBytes(path);
                return TryDecode(file, out width, out height, out bgra);
            }
            catch
            {
                return false;
            }
        }

        public static bool TryDecode(byte[] file, out int width, out int height, out byte[] bgra)
        {
            width = 0; height = 0; bgra = null;

            if (file == null || file.Length < 8 + 25)
                return false;

            for (int i = 0; i < Signature.Length; i++)
                if (file[i] != Signature[i])
                    return false;

            int pos = 8;
            int w = 0, h = 0;
            int bitDepth = 0, colourType = 0, interlace = 0;
            byte[] palette = null;   // RGB triplets
            byte[] paletteAlpha = null;
            using var idat = new MemoryStream();

            while (pos + 8 <= file.Length)
            {
                int length = ReadBE32(file, pos);
                if (length < 0 || pos + 12 + length > file.Length)
                    break;

                string type = System.Text.Encoding.ASCII.GetString(file, pos + 4, 4);
                int dataPos = pos + 8;

                switch (type)
                {
                    case "IHDR":
                        w = ReadBE32(file, dataPos);
                        h = ReadBE32(file, dataPos + 4);
                        bitDepth = file[dataPos + 8];
                        colourType = file[dataPos + 9];
                        interlace = file[dataPos + 12];
                        break;

                    case "PLTE":
                        palette = new byte[length];
                        Array.Copy(file, dataPos, palette, 0, length);
                        break;

                    case "tRNS":
                        paletteAlpha = new byte[length];
                        Array.Copy(file, dataPos, paletteAlpha, 0, length);
                        break;

                    case "IDAT":
                        idat.Write(file, dataPos, length);
                        break;

                    case "IEND":
                        pos = file.Length; // stop
                        break;
                }

                pos = dataPos + length + 4; // skip data + CRC
            }

            if (w <= 0 || h <= 0 || bitDepth != 8 || interlace != 0)
                return false;

            int channels = colourType switch
            {
                0 => 1, // grey
                2 => 3, // RGB
                3 => 1, // indexed
                4 => 2, // grey + alpha
                6 => 4, // RGBA
                _ => 0
            };
            if (channels == 0)
                return false;
            if (colourType == 3 && palette == null)
                return false;

            byte[] raw = Inflate(idat);
            if (raw == null)
                return false;

            int bpp = channels;                 // bytes per pixel (bit depth 8)
            int stride = w * bpp;
            if (raw.Length < (stride + 1) * h)
                return false;

            // Unfilter scanlines in place into a contiguous pixel buffer.
            byte[] pixels = new byte[stride * h];
            byte[] prevRow = new byte[stride];
            int src = 0;
            for (int y = 0; y < h; y++)
            {
                int filter = raw[src++];
                int rowStart = y * stride;
                for (int x = 0; x < stride; x++)
                {
                    int a = x >= bpp ? pixels[rowStart + x - bpp] : 0; // left
                    int b = prevRow[x];                                 // up
                    int c = x >= bpp ? prevRow[x - bpp] : 0;            // up-left
                    int val = raw[src++];
                    int outv = filter switch
                    {
                        0 => val,
                        1 => val + a,
                        2 => val + b,
                        3 => val + ((a + b) >> 1),
                        4 => val + Paeth(a, b, c),
                        _ => val
                    };
                    pixels[rowStart + x] = (byte)outv;
                }
                Array.Copy(pixels, rowStart, prevRow, 0, stride);
            }

            // Convert to BGRA.
            bgra = new byte[w * h * 4];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int si = y * stride + x * bpp;
                    int di = (y * w + x) * 4;
                    byte r, g, bl, al;
                    switch (colourType)
                    {
                        case 0: // grey
                            r = g = bl = pixels[si]; al = 255;
                            break;
                        case 2: // RGB
                            r = pixels[si]; g = pixels[si + 1]; bl = pixels[si + 2]; al = 255;
                            break;
                        case 3: // indexed
                        {
                            int idx = pixels[si];
                            int p = idx * 3;
                            r = (byte)(p + 0 < palette.Length ? palette[p] : 0);
                            g = (byte)(p + 1 < palette.Length ? palette[p + 1] : 0);
                            bl = (byte)(p + 2 < palette.Length ? palette[p + 2] : 0);
                            al = (byte)(paletteAlpha != null && idx < paletteAlpha.Length ? paletteAlpha[idx] : 255);
                            break;
                        }
                        case 4: // grey + alpha
                            r = g = bl = pixels[si]; al = pixels[si + 1];
                            break;
                        default: // 6 RGBA
                            r = pixels[si]; g = pixels[si + 1]; bl = pixels[si + 2]; al = pixels[si + 3];
                            break;
                    }
                    bgra[di + 0] = bl; // B
                    bgra[di + 1] = g;  // G
                    bgra[di + 2] = r;  // R
                    bgra[di + 3] = al; // A
                }
            }

            width = w;
            height = h;
            return true;
        }

        private static byte[] Inflate(MemoryStream zlib)
        {
            zlib.Position = 0;
            try
            {
                // .NET 6+ understands the zlib (RFC 1950) wrapper directly.
                using var z = new ZLibStream(zlib, CompressionMode.Decompress, leaveOpen: true);
                using var outMs = new MemoryStream();
                z.CopyTo(outMs);
                return outMs.ToArray();
            }
            catch
            {
                // Fall back to raw DEFLATE, skipping the 2-byte zlib header.
                try
                {
                    zlib.Position = 2;
                    using var d = new DeflateStream(zlib, CompressionMode.Decompress, leaveOpen: true);
                    using var outMs = new MemoryStream();
                    d.CopyTo(outMs);
                    return outMs.ToArray();
                }
                catch
                {
                    return null;
                }
            }
        }

        private static int Paeth(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);
            if (pa <= pb && pa <= pc) return a;
            return pb <= pc ? b : c;
        }

        private static int ReadBE32(byte[] data, int offset)
        {
            return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
        }
    }
}
