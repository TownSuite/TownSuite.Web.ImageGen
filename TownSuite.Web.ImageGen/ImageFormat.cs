namespace TownSuite.Web.ImageGen
{
    public class ImageFormat
    {
        public enum Format
        {
            jpeg,
            png,
            gif,
            bmp,
            tiff,
            webp,
            svg,
            heic,
            avif
        }
        public static bool IsFormat(string formatStr, Format type)
        {
            if (formatStr.StartsWith("image/"))
            {
                formatStr = formatStr[6..];
            }
            if (formatStr.Contains("jpg")) formatStr = "jpeg";
            if (formatStr == "heif") formatStr = "heic";
            if (formatStr.Contains("svg")) formatStr = "svg";
            return string.Equals(formatStr, type.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }

        public static Format GetFormat(string formatStr)
        {
            if (formatStr.StartsWith("image/"))
            {
                formatStr = formatStr[6..];
            }
            if (formatStr.Contains("jpg")) formatStr = "jpeg";
            if (formatStr == "heif") formatStr = "heic";
            if (formatStr.Contains("svg")) formatStr = "svg";
            return (Format)Enum.Parse(typeof(Format), formatStr, true);
            
        }

    }
}
