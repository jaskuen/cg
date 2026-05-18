using System.Drawing;
using System.Numerics;

namespace SilkOpenGL.Helpers;

public static class ColorHelper
{
    public static Color ChangeColorBrightness(Color color, float correctionFactor)
    {
        float red = (float)color.R;
        float green = (float)color.G;
        float blue = (float)color.B;

        if (correctionFactor < 0)
        {
            correctionFactor = 1 + correctionFactor;
            red *= correctionFactor;
            green *= correctionFactor;
            blue *= correctionFactor;
        }
        else
        {
            red = Math.Min((255 - red) * correctionFactor + red, 255);
            green = Math.Min((255 - green) * correctionFactor + green, 255);
            blue = Math.Min((255 - blue) * correctionFactor + blue, 255);
        }

        return Color.FromArgb(color.A, (int)red, (int)green, (int)blue);
    }

    public static Vector3 ColorToVector3(Color color)
    {
        return new Vector3(color.R / 255f, color.G / 255f, color.B / 255f);
    }
}