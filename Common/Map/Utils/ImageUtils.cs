/*




88        88  88888888888  88888888ba   88b           d88  88888888888  ad88888ba
88        88  88           88      "8b  888b         d888  88          d8"     "8b
88        88  88           88      ,8P  88`8b       d8'88  88          Y8,
88aaaaaaaa88  88aaaaa      88aaaaaa8P'  88 `8b     d8' 88  88aaaaa     `Y8aaaaa,
88""""""""88  88"""""      88""""88'    88  `8b   d8'  88  88"""""       `"""""8b,
88        88  88           88    `8b    88   `8b d8'   88  88                  `8b
88        88  88           88     `8b   88    `888'    88  88          Y8a     a8P
88        88  88888888888  88      `8b  88     `8'     88  88888888888  "Y88888P"


                            MESSENGER OF THE MACHINES

*/


using System;
using Godot;

namespace Hermes.Common.Map.Utils;

public static class ImageUtils
{
    /// <summary>
    /// Determines the type of image format based on raw image data
    /// </summary>
    /// <param name="imageData"></param>
    /// <returns>A MapImageType enum containing the specific image type</returns>
    public static ImageType GetImageFormat(byte[] imageData)
    {
        // Check if we have enough bytes to check the header
        if (imageData == null || imageData.Length < 4)
        {
            return ImageType.UNKNOWN;
        }

        // JPEG starts with FF D8 FF
        if (imageData[0] == 0xFF && imageData[1] == 0xD8 && imageData[2] == 0xFF)
        {
            return ImageType.JPEG;
        }

        // PNG starts with 89 50 4E 47 0D 0A 1A 0A
        if (imageData[0] == 0x89 && imageData[1] == 0x50 && imageData[2] == 0x4E && imageData[3] == 0x47)
        {
            return ImageType.PNG;
        }

        // GIF starts with GIF87a or GIF89a
        if (imageData[0] == 0x47 && imageData[1] == 0x49 && imageData[2] == 0x46 && imageData[3] == 0x38)
        {
            return ImageType.GIF;
        }

        // BMP starts with BM
        if (imageData[0] == 0x42 && imageData[1] == 0x4D)
        {
            return ImageType.BMP;
        }

        // TIFF starts with II (little endian) or MM (big endian)
        if ((imageData[0] == 0x49 && imageData[1] == 0x49) ||
            (imageData[0] == 0x4D && imageData[1] == 0x4D))
        {
            return ImageType.TIFF;
        }

        return ImageType.UNKNOWN;
    }

    /// <summary>
    /// Converts a raw byte array to an image texture.
    /// Detects whether the image is a JPEG, PNG, or BMP, and returns the
    /// appropriate ImageTexture
    /// </summary>
    public static ImageTexture ByteArrayToImageTexture(byte[] rawMapData)
    {
        ImageType imageType = GetImageFormat(rawMapData);

        Image image = new Image();

        if (imageType == ImageType.JPEG)
        {
            image.LoadJpgFromBuffer(rawMapData);
        }

        if (imageType == ImageType.PNG)
        {
            image.LoadPngFromBuffer(rawMapData);
        }

        if (imageType == ImageType.BMP)
        {
            image.LoadBmpFromBuffer(rawMapData);
        }

        ImageTexture texture = new ImageTexture();
        texture.SetImage(image);
        return texture;
    }

    /// <summary>
    /// Returns the width and height of an image based on raw image data. Automatically determines image type.
    /// </summary>
    /// <param name="imageData">Raw image data</param>
    /// <returns>Width and height of the image in pixels</returns>
    /// <exception cref="ArgumentException">Thrown if the image data is invalid, or image type is not supported</exception>
    public static (int width, int height) GetImageDimensions(byte[] imageData)
    {
        // Check if we have enough data for header detection
        if (imageData == null || imageData.Length < 24)
        {
            throw new ArgumentException("Invalid image data");
        }

        int width = 0, height = 0;

        ImageType imageType = GetImageFormat(imageData);

        switch (imageType)
        {
            case ImageType.JPEG:
                (width, height) = GetJPEGDimensions(imageData);
                break;
            case ImageType.PNG:
                (width, height) = GetPNGDimensions(imageData);
                break;
            case ImageType.BMP:
                (width, height) = GetBMPDimensions(imageData);
                break;
            default:
                throw new ArgumentException("Invalid image type");
        }

        return (width, height);
    }

    public static (int, int) GetJPEGDimensions(byte[] imageData)
    {
        int pos = 2;
        while (pos < imageData.Length - 1)
        {
            if (imageData[pos] != 0xFF) throw new ArgumentException("Invalid JPEG data");

            byte marker = imageData[pos + 1];
            if (marker == 0xC0 || marker == 0xC1 || marker == 0xC2)
            {
                // SOF marker found - next bytes contain dimensions
                int height = (imageData[pos + 5] << 8) | imageData[pos + 6];
                int width = (imageData[pos + 7] << 8) | imageData[pos + 8];
                return (width, height);
            }

            // Skip to next marker
            pos += 2 + ((imageData[pos + 2] << 8) | imageData[pos + 3]);
        }

        throw new ArgumentException("Could not find JPEG dimensions");
    }

    public static (int, int) GetPNGDimensions(byte[] data)
    {
        // PNG stores dimensions at bytes 16-23
        int width = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
        int height = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];
        return (width, height);
    }

    public static (int, int) GetBMPDimensions(byte[] data)
    {
        // BMP dimensions are stored at bytes 18-25
        int width = BitConverter.ToInt32(data, 18);
        int height = BitConverter.ToInt32(data, 22);
        return (width, Math.Abs(height)); // Height can be negative in BMPs
    }
}
