using System;

namespace TalesOfTheBrave.Simulation.Rulesets
{
    [Serializable]
    public sealed class RouteMapPointDefinition
    {
        public float X;
        public float Y;

        public RouteMapPointDefinition() { }

        public RouteMapPointDefinition(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
