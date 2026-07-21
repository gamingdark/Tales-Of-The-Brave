using NUnit.Framework;
using TalesOfVoyages.Graphics;

public sealed class SpriteCatalogNamingTests
{
    [Test]
    public void SingleSpriteUsesItsAssetName()
    {
        Assert.That(SpriteCatalogNaming.CreateName("img-riga", "img-riga_0", false), Is.EqualTo("img-riga"));
    }

    [Test]
    public void MultipleSpriteUsesDotSeparatedSliceSuffix()
    {
        Assert.That(SpriteCatalogNaming.CreateName("icons", "icons_4", true), Is.EqualTo("icons.4"));
    }

    [Test]
    public void MultipleSpriteRetainsCustomSliceNameAsSuffix()
    {
        Assert.That(SpriteCatalogNaming.CreateName("icons", "anchor", true), Is.EqualTo("icons.anchor"));
    }
}
