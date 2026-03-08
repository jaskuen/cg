using System.Numerics;

namespace SilkOpenGL.Helpers;

public static class CoordinateHelper
{
    public static Vector2 FromViewportToNdc(Vector2 pos, Vector2 frame)
    {
        // 1. Нормализуем координаты в диапазон от 0 до 1
        float nx = pos.X / frame.X;
        float ny = pos.Y / frame.Y;

        // 2. Переводим в диапазон от -1 до 1
        // Для X: 0 -> -1, 1 -> 1
        float x = (nx * 2.0f) - 1.0f;

        // Для Y: 0 (верх) -> 1 (верх NDC), 1 (низ) -> -1 (низ NDC)
        // Поэтому инвертируем: 1.0f - (ny * 2.0f)
        float y = 1.0f - (ny * 2.0f);

        return new Vector2(x, y);
    }
}