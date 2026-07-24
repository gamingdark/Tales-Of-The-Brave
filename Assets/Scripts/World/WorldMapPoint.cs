namespace TalesOfTheBrave.Simulation.World
{
    public readonly struct WorldMapPoint
    {
        public float X { get; }
        public float Y { get; }

        public WorldMapPoint(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
