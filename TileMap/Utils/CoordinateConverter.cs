namespace TileMap.Utils
{
    public static class CoordinateConverter
    {
        private const double Scale = 0.001; // 1 тайл = 0.001 градуса (пример)

        public static (double lon, double lat) ToRedisCoordinates(int x, int y)
        {
            double lon = x * Scale;
            double lat = y * Scale;
            return (lon, lat);
        }

        public static (int x, int y) FromRedisCoordinates(double lon, double lat)
        {
            int x = (int)(lon / Scale);
            int y = (int)(lat / Scale);
            return (x, y);
        }
    }
}
