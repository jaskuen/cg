using Silk.NET.Maths;

namespace SilkOpenGL;

public class MyWindow
{
    public int Width { get; set; }
    public int Height { get; set; }

    public MyWindow(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public Vector2D<float> FromWindowCoordinates(int x, int y)
    {
        return new Vector2D<float>(Width / (float)x, Height / (float)y);
    }
}