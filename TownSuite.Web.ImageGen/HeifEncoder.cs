/*
 * This file is part of heif-enc, an example encoder application for libheif-sharp
 *
 * The MIT License (MIT)
 *
 * Copyright (c) 2020, 2021, 2022, 2023 Nicholas Hayes
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 *
 */

using LibHeifSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;

namespace TownSuite.Web.ImageGen
{
    internal static class HeifEncoder
    {
        public static HeifImage ConvertSharpToHeif(Image<Rgba32> image)
        {
            (bool isGrayscale, bool hasTransparency) = AnalyzeImage(image);
            var colorspace = isGrayscale ? HeifColorspace.Monochrome : HeifColorspace.Rgb;
            HeifChroma chroma;
            if (colorspace == HeifColorspace.Monochrome)
            {
                chroma = HeifChroma.Monochrome;
            }
            else
            {
                chroma = hasTransparency ? HeifChroma.InterleavedRgba32 : HeifChroma.InterleavedRgb24;
            }
            HeifImage? heifImage = null;
            HeifImage? temp = null;
            try
            {
                temp = new HeifImage(image.Width, image.Height, colorspace, chroma);
                if (colorspace == HeifColorspace.Monochrome)
                {
                    temp.AddPlane(HeifChannel.Y, image.Width, image.Height, 8);
                    if (hasTransparency)
                    {
                        temp.AddPlane(HeifChannel.Alpha, image.Width, image.Height, 8);
                    }
                    CopyGrayscale(image, temp, hasTransparency);
                }
                else
                {
                    temp.AddPlane(HeifChannel.Interleaved, image.Width, image.Height, 8);
                    CopyRgb(image, temp, hasTransparency);
                }
                heifImage = temp;
                temp = null;
            }
            finally
            {
                temp?.Dispose();
            }
            return heifImage;
        }

        private static (bool isGrayscale, bool hasTransparency) AnalyzeImage(Image<Rgba32> image)
        {
            bool isGrayscale = true;
            bool hasTransparency = false;
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var src = accessor.GetRowSpan(y);
                    for (int x = 0; x < accessor.Width; x++)
                    {
                        ref var pixel = ref src[x];
                        if (!(pixel.R == pixel.G && pixel.G == pixel.B))
                        {
                            isGrayscale = false;
                        }
                        if (pixel.A < 255)
                        {
                            hasTransparency = true;
                        }
                    }
                }
            });
            return (isGrayscale, hasTransparency);
        }

        private static void CopyGrayscale(Image<Rgba32> image,
                                                 HeifImage heifImage,
                                                 bool hasTransparency)
        {
            var grayPlane = heifImage.GetPlane(HeifChannel.Y);
            IntPtr grayStartPtr = grayPlane.Scan0;
            int grayPlaneStride = grayPlane.Stride;
            if (hasTransparency)
            {
                var alphaPlane = heifImage.GetPlane(HeifChannel.Alpha);
                IntPtr alphaStartPtr = grayPlane.Scan0;
                int alphaPlaneStride = alphaPlane.Stride;

                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        var src = accessor.GetRowSpan(y);
                        for (int x = 0; x < accessor.Width; x++)
                        {
                            ref var pixel = ref src[x];
                            SetAlpha(pixel.A, alphaStartPtr + x + (y * alphaPlaneStride));
                            SetGrayscale(pixel.R, grayStartPtr + x + (y * grayPlaneStride));
                        }
                    }
                });
            }
            else
            {
                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        var src = accessor.GetRowSpan(y);
                        for (int x = 0; x < accessor.Width; x++)
                        {
                            ref var pixel = ref src[x];
                            SetGrayscale(pixel.R, grayStartPtr + x + (y * grayPlaneStride));
                        }
                    }
                });
            }
        }

        private static void CopyRgb(Image<Rgba32> image,
                                           HeifImage heifImage,
                                           bool hasTransparency)
        {
            var interleavedData = heifImage.GetPlane(HeifChannel.Interleaved);
            IntPtr startPtr = interleavedData.Scan0;
            int srcStride = interleavedData.Stride;
  
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var src = accessor.GetRowSpan(y);
                    for (int x = 0; x < accessor.Width; x++)
                    {
                        ref var pixel = ref src[x];
                        if (hasTransparency)
                        {
                            SetRgba(pixel.R, pixel.G, pixel.B, pixel.A, startPtr + (x * 4) + (y * srcStride));
                        } else
                        {
                            SetRgb(pixel.R, pixel.G, pixel.B, startPtr + (x * 3) + (y * srcStride));
                        }
                    }
                }
            });
        }

        private static void SetGrayscale(byte R, IntPtr pixelPtr)
        {
            Marshal.WriteByte(pixelPtr, R);
        }

        private static void SetAlpha(byte A, IntPtr pixelPtr)
        {
            Marshal.WriteByte(pixelPtr, A);
        }

        private static void SetRgb(byte R, byte G, byte B, IntPtr pixelPtr)
        {
            Marshal.WriteByte(pixelPtr, R);
            Marshal.WriteByte(pixelPtr + 1, G);
            Marshal.WriteByte(pixelPtr + 2, B);
        }

        public static void SetRgba(byte R, byte G, byte B, byte A, IntPtr pixelPtr)
        {
            Marshal.WriteByte(pixelPtr, R);
            Marshal.WriteByte(pixelPtr + 1, G);
            Marshal.WriteByte(pixelPtr + 2, B);
            Marshal.WriteByte(pixelPtr + 3, A);
        }
    }
}