namespace TileMap
{
        public static class TileProperties
        {
            [Flags]
            public enum Flags : byte
            {
                None = 0,
                CanPlaceObject = 1 << 0
            }

            private static readonly Flags[] flagsByType;

            static TileProperties()
            {
                var max = Enum.GetValues(typeof(TileType)).Length;
                flagsByType = new Flags[max];

                flagsByType[(int)TileType.Plain] = Flags.CanPlaceObject;
                flagsByType[(int)TileType.Mountain] = Flags.None;
            }

            public static bool CanPlaceObject(TileType type) => (flagsByType[(int)type] & Flags.CanPlaceObject) != 0;

            public static void SetFlags(TileType type, Flags flags)
            {
                flagsByType[(int)type] = flags;
            }

            public static Flags GetFlags(TileType type) => flagsByType[(int)type];
        }
    }
