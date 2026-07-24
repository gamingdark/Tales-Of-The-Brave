using System;

namespace TalesOfTheBrave.Graphics
{
    public static class SpriteCatalogNaming
    {
        public static string CreateName(string assetName, string spriteName, bool isMultiple)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
                throw new ArgumentException("A sprite name is required.", nameof(spriteName));
            if (!isMultiple)
            {
                if (string.IsNullOrWhiteSpace(assetName))
                    throw new ArgumentException("An asset name is required.", nameof(assetName));
                return assetName;
            }
            if (string.IsNullOrWhiteSpace(assetName))
                throw new ArgumentException("An asset name is required for a sliced sprite.", nameof(assetName));

            var prefix = assetName + "_";
            var suffix = spriteName.StartsWith(prefix, StringComparison.Ordinal)
                ? spriteName.Substring(prefix.Length)
                : spriteName;
            return assetName + "." + suffix;
        }
    }
}
