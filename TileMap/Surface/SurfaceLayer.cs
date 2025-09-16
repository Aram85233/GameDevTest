using System.Runtime.CompilerServices;

namespace TileMap.Surface
{
    public sealed class SurfaceLayer
    {
        private readonly byte[] tiles;
        public int Width { get; }
        public int Height { get; }
        public int Count => checked(Width * Height);

        public SurfaceLayer(int width, int height, TileType defaultType = TileType.Plain)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            Width = width;
            Height = height;
            tiles = new byte[checked(width * height)];

            if (defaultType != TileType.Plain)
            {
                byte b = (byte)defaultType;
                for (int i = 0; i < tiles.Length; i++) tiles[i] = b;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Index(int x, int y)
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
                throw new ArgumentOutOfRangeException($"Координаты вне карты: ({x},{y})");
            return x + y * Width;
        }

        public TileType GetTile(int x, int y) => (TileType)tiles[Index(x, y)];

        public bool TryGetTile(int x, int y, out TileType type)
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
            {
                type = default;
                return false;
            }
            type = (TileType)tiles[x + y * Width];
            return true;
        }

        public void SetTile(int x, int y, TileType type) => tiles[Index(x, y)] = (byte)type;

        public static SurfaceLayer FromArray(int width, int height, TileType[] source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source.Length != width * height) throw new ArgumentException("Длина массива не совпадает с размером карты");

            var layer = new SurfaceLayer(width, height);
            Buffer.BlockCopy(source, 0, layer.tiles, 0, source.Length);
            return layer;
        }

        public void FillArea(int x1, int y1, int x2, int y2, TileType type)
        {
            if (x1 > x2) (x1, x2) = (x2, x1);
            if (y1 > y2) (y1, y2) = (y2, y1);

            if (x2 < 0 || y2 < 0 || x1 >= Width || y1 >= Height) return;

            x1 = Math.Max(x1, 0);
            y1 = Math.Max(y1, 0);
            x2 = Math.Min(x2, Width - 1);
            y2 = Math.Min(y2, Height - 1);

            byte b = (byte)type;

            for (int y = y1; y <= y2; y++)
            {
                int rowStart = y * Width + x1;
                int len = x2 - x1 + 1;
                for (int i = 0; i < len; i++)
                    tiles[rowStart + i] = b;
            }
        }

        public bool CanPlaceObjectsInArea(int x1, int y1, int x2, int y2)
        {
            if (x1 > x2) (x1, x2) = (x2, x1);
            if (y1 > y2) (y1, y2) = (y2, y1);

            if (x1 < 0 || y1 < 0 || x2 >= Width || y2 >= Height) return false;

            for (int y = y1; y <= y2; y++)
            {
                int rowStart = y * Width + x1;
                int len = x2 - x1 + 1;
                for (int i = 0; i < len; i++)
                {
                    var t = (TileType)tiles[rowStart + i];
                    if (!TileProperties.CanPlaceObject(t)) return false;
                }
            }
            return true;
        }

        public void Print()
        {
            for (int y = 0; y < Height; y++)
            {
                int rowStart = y * Width;
                for (int x = 0; x < Width; x++)
                {
                    char symbol = tiles[rowStart + x] switch
                    {
                        (byte)TileType.Plain => '.',
                        (byte)TileType.Mountain => '^',
                        (byte)TileType.Water => '~',
                        _ => '?'
                    };
                    Console.Write(symbol);
                }
                Console.WriteLine();
            }
        }

        public long EstimatedMemoryBytes() => tiles.Length;
    }
}
