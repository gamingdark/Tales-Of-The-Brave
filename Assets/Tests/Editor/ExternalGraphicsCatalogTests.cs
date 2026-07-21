using System;
using NUnit.Framework;
using TalesOfVoyages.Graphics;
using UnityEngine;

public sealed class ExternalGraphicsCatalogTests
{
    [Test]
    public void CatalogResolvesConfiguredSpriteByExternalName()
    {
        var gameObject = new GameObject("Graphics Catalog Test");
        var texture = new Texture2D(1, 1);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
        try
        {
            var catalog = gameObject.AddComponent<ExternalGraphicsCatalog>();
            catalog.ReplaceEntries(new[] { new ExternalGraphicsCatalog.Entry("icons.8", sprite) });

            Assert.That(catalog.ContainsSprite("icons.8"), Is.True);
            Assert.That(catalog.GetSprite("icons.8"), Is.SameAs(sprite));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sprite);
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void CatalogRejectsDuplicateExternalNames()
    {
        var gameObject = new GameObject("Graphics Catalog Test");
        var texture = new Texture2D(1, 1);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
        try
        {
            var catalog = gameObject.AddComponent<ExternalGraphicsCatalog>();
            Assert.Throws<InvalidOperationException>(() => catalog.ReplaceEntries(new[]
            {
                new ExternalGraphicsCatalog.Entry("icons.8", sprite),
                new ExternalGraphicsCatalog.Entry("icons.8", sprite)
            }));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sprite);
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }
}
