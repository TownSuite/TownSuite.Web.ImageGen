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
            return string.Equals(ParseFormatString(formatStr), type.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }
        public static Format GetFormat(string formatStr)
        {
            try { return (Format)Enum.Parse(typeof(Format), ParseFormatString(formatStr), true); }
            catch { return Format.png; }  
        }
        private static string ParseFormatString(string formatStr)
        {
            formatStr = formatStr.Trim();
            if (formatStr.All(c => !char.IsLetter(c))) return "png";
            if (formatStr.StartsWith("image/")) formatStr = formatStr[6..];
            if (formatStr.EndsWith("+xml")) formatStr = formatStr[..4];
            if (formatStr.Contains("jpg")) formatStr = "jpeg";
            if (formatStr.Contains("heif")) formatStr = "heic";
            if (formatStr.Contains("svg")) formatStr = "svg";
            return formatStr;
        }
    }
}
