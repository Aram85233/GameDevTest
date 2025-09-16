namespace TileMap.Objects;

public class GameObject
{
    public string Id { get; }
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    public GameObject(string id, int x, int y, int width, int height)
    {
        Id = id;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    // Проверяет пересечение с другим объектом
    public bool Intersects(GameObject other)
    {
        return X < other.X + other.Width &&
               X + Width > other.X &&
               Y < other.Y + other.Height &&
               Y + Height > other.Y;
    }

    // Проверка вхождения в область
    public bool IsInsideArea(int x1, int y1, int x2, int y2)
    {
        return X + Width - 1 >= x1 &&
               Y + Height - 1 >= y1 &&
               X <= x2 &&
               Y <= y2;
    }
}