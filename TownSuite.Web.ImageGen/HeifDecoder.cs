using LibHeifSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Runtime.InteropServices;

namespace TownSuite.Web.ImageGen
{
    public static class HeifDecoder
    {
        public static Image<Rgba32> ConvertHeifToSharp(Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
   
            using var heifContext = new HeifContext(ms.ToArray());
            using var srcImageHandle = heifContext.GetPrimaryImageHandle();
            using var srcImage = srcImageHandle.Decode(HeifColorspace.Rgb, HeifChroma.InterleavedRgba32);

            var outImage = new Image<Rgba32>(srcImage.Width, srcImage.Height);

            var planeData = srcImage.GetPlane(HeifChannel.Interleaved);
            IntPtr startPtr = planeData.Scan0;
            int stride = planeData.Stride;

            for (int y = 0; y < srcImage.Height; y++)
            {
               for (int x = 0; x < srcImage.Width; x++)
                {
                   var ptr = startPtr + (y * stride) + (x * 4);
                   var r = Marshal.ReadByte(ptr);
                   var g = Marshal.ReadByte(ptr + 1);
                   var b = Marshal.ReadByte(ptr + 2);
                   var a = Marshal.ReadByte(ptr + 3);
                   outImage[x, y] = new Rgba32(r, g, b, a);
               }
            }
            return outImage;
        }
    }
}
