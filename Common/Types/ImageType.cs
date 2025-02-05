
public class ImageType
{
    public const int BMP = 0;
    public const int JPEG = 1;
    public const int GIF = 2;
    public const int TIFF = 3;
    public const int PNG = 4;
    public const int UNKNOWN = int.MaxValue;

    public static string ToString(int imageType)
    {
        switch(imageType)
        {
            case BMP:
                return "BMP";
            case JPEG:
                return "JPEG";
            case GIF:
                return "GIF";
            case TIFF:
                return "TIFF";
            case PNG:
                return "PNG";
            default:
                return "UNKNOWN";
        }
    }
}
