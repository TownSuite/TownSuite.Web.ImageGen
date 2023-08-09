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
            if (!Available())
                throw new Exception("HeifDecoder is not available");
            if (!stream.CanRead)
                throw new ArgumentException("Invalid stream", stream.GetType().Name);
            if (stream.Length < 1)
                return new Image<Rgba32>(1, 1);
            
            using var heifContext = new HeifContext(stream);
            using HeifImageHandle srcImageHandle = heifContext.GetPrimaryImageHandle();
            using HeifImage srcImage = srcImageHandle.Decode(HeifColorspace.Rgb, HeifChroma.InterleavedRgba32);

            var outImage = new Image<Rgba32>(srcImage.Width, srcImage.Height);

            HeifPlaneData planeData = srcImage.GetPlane(HeifChannel.Interleaved);
            IntPtr startPtr = planeData.Scan0;
            int stride = planeData.Stride;

            for (int y = 0; y < srcImage.Height; y++)
            {
               for (int x = 0; x < srcImage.Width; x++)
                {
                   IntPtr ptr = startPtr + (y * stride) + (x * 4);
                   var r = Marshal.ReadByte(ptr);
                   var g = Marshal.ReadByte(ptr + 1);
                   var b = Marshal.ReadByte(ptr + 2);
                   var a = Marshal.ReadByte(ptr + 3);
                   outImage[x, y] = new Rgba32(r, g, b, a);
               }
            }
            return outImage;
        }
        public static bool Available()
        {
            return LibHeifInfo.HaveDecoder(HeifCompressionFormat.Av1) && LibHeifInfo.HaveDecoder(HeifCompressionFormat.Hevc); 
        }
    }
}
