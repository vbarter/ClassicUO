// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.InteropServices;
using ClassicUO.Utility.Platforms;

namespace ClassicUO.Utility
{
    public static class ZLib
    {
        // thanks ServUO :)

        private static readonly ICompressor _compressor;

        static ZLib()
        {
            if (OperatingSystem.IsIOS())
            {
                // iOS apps cannot dlopen arbitrary system libraries; use the runtime's bundled zlib
                _compressor = new DotNetCompressor();
            }
            else if (Environment.Is64BitProcess)
            {
                if(PlatformHelper.IsWindows)
                {
                    _compressor = new Compressor64();
                }
                else
                {
                    _compressor = new CompressorUnix64();
                }
            }
            else
            {
                _compressor = new ManagedUniversal();
            }
        }

        public static ZLibError Decompress(byte[] source, int offset, byte[] dest, int length)
        {
            return _compressor.Decompress(dest, ref length, source, source.Length - offset);
        }

        public static ZLibError Decompress(IntPtr source, int sourceLength, int offset, IntPtr dest, int length)
        {
            return _compressor.Decompress(dest, ref length, source, sourceLength - offset);
        }

        public static unsafe ZLibError Decompress(ReadOnlySpan<byte> source, Span<byte> dest)
        {
            fixed (byte* srcPtr = source)
            fixed (byte* destPtr = dest)
                return Decompress((IntPtr)srcPtr, source.Length, 0, (IntPtr)destPtr, dest.Length);
        }

        private enum ZLibQuality
        {
            Default = -1,

            None = 0,

            Speed = 1,
            Size = 9
        }

        public enum ZLibError
        {
            VersionError = -6,
            BufferError = -5,
            MemoryError = -4,
            DataError = -3,
            StreamError = -2,
            FileError = -1,

            Ok = 0,

            StreamEnd = 1,
            NeedDictionary = 2
        }


        private interface ICompressor
        {
            string Version { get; }

            ZLibError Compress(byte[] dest, ref int destLength, byte[] source, int sourceLength);
            ZLibError Compress(byte[] dest, ref int destLength, byte[] source, int sourceLength, ZLibQuality quality);

            ZLibError Decompress(byte[] dest, ref int destLength, byte[] source, int sourceLength);
            ZLibError Decompress(IntPtr dest, ref int destLength, IntPtr source, int sourceLength);
        }


        /// <summary>
        /// zlib through System.IO.Compression, which ships its own native zlib with the runtime.
        /// </summary>
        private sealed unsafe class DotNetCompressor : ICompressor
        {
            public string Version => "System.IO.Compression";

            public ZLibError Compress(byte[] dest, ref int destLength, byte[] source, int sourceLength)
            {
                return Compress(dest, ref destLength, source, sourceLength, ZLibQuality.Default);
            }

            public ZLibError Compress(byte[] dest, ref int destLength, byte[] source, int sourceLength, ZLibQuality quality)
            {
                System.IO.Compression.CompressionLevel level = quality switch
                {
                    ZLibQuality.None => System.IO.Compression.CompressionLevel.NoCompression,
                    ZLibQuality.Speed => System.IO.Compression.CompressionLevel.Fastest,
                    ZLibQuality.Size => System.IO.Compression.CompressionLevel.SmallestSize,
                    _ => System.IO.Compression.CompressionLevel.Optimal
                };

                try
                {
                    using var output = new System.IO.MemoryStream(dest, 0, destLength, true);

                    using (var zlib = new System.IO.Compression.ZLibStream(output, level, true))
                    {
                        zlib.Write(source, 0, sourceLength);
                    }

                    destLength = (int)output.Position;

                    return ZLibError.Ok;
                }
                catch (NotSupportedException)
                {
                    // The destination buffer is too small
                    return ZLibError.BufferError;
                }
            }

            public ZLibError Decompress(byte[] dest, ref int destLength, byte[] source, int sourceLength)
            {
                fixed (byte* destPtr = dest)
                fixed (byte* sourcePtr = source)
                {
                    return Decompress((IntPtr)destPtr, ref destLength, (IntPtr)sourcePtr, sourceLength);
                }
            }

            public ZLibError Decompress(IntPtr dest, ref int destLength, IntPtr source, int sourceLength)
            {
                try
                {
                    using var input = new System.IO.UnmanagedMemoryStream((byte*)source, sourceLength);
                    using var zlib = new System.IO.Compression.ZLibStream(input, System.IO.Compression.CompressionMode.Decompress);

                    var output = new Span<byte>((void*)dest, destLength);
                    int total = 0;

                    while (total < output.Length)
                    {
                        int read = zlib.Read(output.Slice(total));

                        if (read == 0)
                        {
                            break;
                        }

                        total += read;
                    }

                    destLength = total;

                    return ZLibError.Ok;
                }
                catch (System.IO.InvalidDataException)
                {
                    return ZLibError.DataError;
                }
            }
        }

        private sealed class Compressor64 : ICompressor
        {
            public string Version => SafeNativeMethods.zlibVersion();

            public ZLibError Compress(byte[] dest, ref int destLength, byte[] source, int sourceLength)
            {
                return SafeNativeMethods.compress(dest, ref destLength, source, sourceLength);
            }

            public ZLibError Compress(byte[] dest, ref int destLength, byte[] source, int sourceLength, ZLibQuality quality)
            {
                return SafeNativeMethods.compress2
                (
                    dest,
                    ref destLength,
                    source,
                    sourceLength,
                    quality
                );
            }

            public ZLibError Decompress(byte[] dest, ref int destLength, byte[] source, int sourceLength)
            {
                return SafeNativeMethods.uncompress(dest, ref destLength, source, sourceLength);
            }

            public ZLibError Decompress(IntPtr dest, ref int destLength, IntPtr source, int sourceLength)
            {
                return SafeNativeMethods.uncompress(dest, ref destLength, source, sourceLength);
            }

            private class SafeNativeMethods
            {
                [DllImport("zlib")]
                public static extern string zlibVersion();

                [DllImport("zlib")]
                public static extern ZLibError compress(byte[] dest, ref int destLength, byte[] source, int sourceLength);

                [DllImport("zlib")]
                public static extern ZLibError compress2(byte[] dest, ref int destLength, byte[] source, int sourceLength, ZLibQuality quality);

                [DllImport("zlib")]
                public static extern ZLibError uncompress(byte[] dest, ref int destLen, byte[] source, int sourceLen);

                [DllImport("zlib")]
                public static extern ZLibError uncompress(IntPtr dest, ref int destLen, IntPtr source, int sourceLen);
            }
        }

        private sealed class CompressorUnix64 : ICompressor
        {
            public string Version => SafeNativeMethods.zlibVersion();

            public ZLibError Compress(byte[] dest, ref int destLength, byte[] source, int sourceLength)
            {
                long destLengthLong = destLength;
                ZLibError z = SafeNativeMethods.compress(dest, ref destLengthLong, source, sourceLength);
                destLength = (int) destLengthLong;

                return z;
            }

            public ZLibError Compress(byte[] dest, ref int destLength, byte[] source, int sourceLength, ZLibQuality quality)
            {
                long destLengthLong = destLength;

                ZLibError z = SafeNativeMethods.compress2
                (
                    dest,
                    ref destLengthLong,
                    source,
                    sourceLength,
                    quality
                );

                destLength = (int) destLengthLong;

                return z;
            }

            public ZLibError Decompress(byte[] dest, ref int destLength, byte[] source, int sourceLength)
            {
                long destLengthLong = destLength;
                ZLibError z = SafeNativeMethods.uncompress(dest, ref destLengthLong, source, sourceLength);
                destLength = (int) destLengthLong;

                return z;
            }

            public ZLibError Decompress(IntPtr dest, ref int destLength, IntPtr source, int sourceLength)
            {
                return SafeNativeMethods.uncompress(dest, ref destLength, source, sourceLength);
            }

            private class SafeNativeMethods
            {
                [DllImport("libz")]
                public static extern string zlibVersion();

                [DllImport("libz")]
                public static extern ZLibError compress(byte[] dest, ref long destLength, byte[] source, long sourceLength);

                [DllImport("libz")]
                public static extern ZLibError compress2(byte[] dest, ref long destLength, byte[] source, long sourceLength, ZLibQuality quality);

                [DllImport("libz")]
                public static extern ZLibError uncompress(byte[] dest, ref long destLen, byte[] source, long sourceLen);

                [DllImport("libz")]
                public static extern ZLibError uncompress(IntPtr dest, ref int destLen, IntPtr source, int sourceLen);
            }
        }

        private sealed class ManagedUniversal : ICompressor
        {
            public string Version => "1.2.11";

            public ZLibError Compress(byte[] dest, ref int destLength, byte[] source, int sourceLength)
            {
                ZLibManaged.Compress(dest, ref destLength, source);

                return ZLibError.Ok;
            }

            public ZLibError Compress(byte[] dest, ref int destLength, byte[] source, int sourceLength, ZLibQuality quality)
            {
                return Compress(dest, ref destLength, source, sourceLength);
            }

            public ZLibError Decompress(byte[] dest, ref int destLength, byte[] source, int sourceLength)
            {
                ZLibManaged.Decompress
                (
                    source,
                    0,
                    sourceLength,
                    0,
                    dest,
                    destLength
                );

                return ZLibError.Ok;
            }

            public ZLibError Decompress(IntPtr dest, ref int destLength, IntPtr source, int sourceLength)
            {
                ZLibManaged.Decompress
                (
                    source,
                    sourceLength,
                    0,
                    dest,
                    destLength
                );

                return ZLibError.Ok;
            }
        }
    }
}