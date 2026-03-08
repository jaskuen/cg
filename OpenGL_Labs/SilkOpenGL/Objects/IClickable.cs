using System.Numerics;

namespace SilkOpenGL.Objects;

public interface IClickable
{
    public uint ColorId { get; set; }

    public void OnMouseDown(Vector3 position);
    public void OnMouseUp(Vector3 position);
    public void OnMouseMove(Vector3 position);
    public void OnMouseEnter();
    public void OnMouseLeave();
}