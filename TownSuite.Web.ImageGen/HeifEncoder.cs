﻿/*
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
    public static class HeifEncoder
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

        public static (bool isGrayscale, bool hasTransparency) AnalyzeImage(Image<Rgba32> image)
        {
            bool isGrayscale = true;
            bool hasTransparency = false;
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> src = accessor.GetRowSpan(y);
                    for (int x = 0; x < accessor.Width; x++)
                    {
                        ref Rgba32 pixel = ref src[x];
                        if (isGrayscale && !(pixel.R == pixel.G && pixel.G == pixel.B))
                        {
                            isGrayscale = false;
                        }
                        if (!hasTransparency && pixel.A < 255)
                        {
                            hasTransparency = true;
                        }
                        if (!isGrayscale && hasTransparency)
                        {
                            return;
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
            HeifPlaneData grayPlane = heifImage.GetPlane(HeifChannel.Y);
            IntPtr grayStartPtr = grayPlane.Scan0;
            int grayPlaneStride = grayPlane.Stride;
            if (hasTransparency)
            {
                HeifPlaneData alphaPlane = heifImage.GetPlane(HeifChannel.Alpha);
                IntPtr alphaStartPtr = grayPlane.Scan0;
                int alphaPlaneStride = alphaPlane.Stride;

                ProcessPixelRows(image, (pixel, x, y) =>
                {
                    SetAlpha(pixel.A, alphaStartPtr + x + (y * alphaPlaneStride));
                    SetGrayscale(pixel.R, grayStartPtr + x + (y * grayPlaneStride));
                });
            }
            else
            {
                ProcessPixelRows(image, (pixel, x, y) => SetGrayscale(pixel.R, grayStartPtr + x + (y * grayPlaneStride)));
            }
        }

        private static void CopyRgb(Image<Rgba32> image, HeifImage heifImage, bool hasTransparency)
        {
            HeifPlaneData interleavedData = heifImage.GetPlane(HeifChannel.Interleaved);
            IntPtr startPtr = interleavedData.Scan0;
            int srcStride = interleavedData.Stride;

            if (hasTransparency)
            {
                ProcessPixelRows(image, (pixel, x, y) =>
                    SetRgba(pixel.R, pixel.G, pixel.B, pixel.A, startPtr + (x * 4) + (y * srcStride)));
            }
            else
            {
                ProcessPixelRows(image, (pixel, x, y) =>
                    SetRgb(pixel.R, pixel.G, pixel.B, startPtr + (x * 3) + (y * srcStride)));
            }
        }
        private static void ProcessPixelRows(Image<Rgba32> image, Action<Rgba32, int, int> action)
        {
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> src = accessor.GetRowSpan(y);
                    for (int x = 0; x < accessor.Width; x++)
                    {
                        ref Rgba32 pixel = ref src[x];
                        action(pixel, x, y);
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
        public static bool Available()
        {
            return LibHeifInfo.HaveEncoder(HeifCompressionFormat.Av1) && LibHeifInfo.HaveEncoder(HeifCompressionFormat.Hevc);
        }
    }
}