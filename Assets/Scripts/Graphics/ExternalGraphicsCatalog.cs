using System;
using System.Collections.Generic;
using UnityEngine;

namespace TalesOfVoyages.Graphics
{
    public sealed class ExternalGraphicsCatalog : MonoBehaviour, ISpriteNameLookup
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private string name;
            [SerializeField] private Sprite sprite;

            public string Name => name;
            public Sprite Sprite => sprite;

            public Entry(string name, Sprite sprite)
            {
                this.name = name;
                this.sprite = sprite;
            }
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, Sprite> Sprites
        {
            get
            {
                RebuildDictionary();
                return sprites;
            }
        }

        public bool ContainsSprite(string name)
        {
            RebuildDictionary();
            return !string.IsNullOrWhiteSpace(name) && sprites.ContainsKey(name);
        }

        public Sprite GetSprite(string name)
        {
            RebuildDictionary();
            if (!sprites.TryGetValue(name, out var sprite))
                throw new KeyNotFoundException($"Sprite '{name}' is not present in the external graphics catalog.");
            return sprite;
        }

        public void ReplaceEntries(IEnumerable<Entry> newEntries)
        {
            if (newEntries == null) throw new ArgumentNullException(nameof(newEntries));
            entries = new List<Entry>(newEntries);
            RebuildDictionary();
        }

        private void Awake() => RebuildDictionary();
        private void OnValidate() => RebuildDictionary();

        private void RebuildDictionary()
        {
            sprites.Clear();
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Name) || entry.Sprite == null) continue;
                if (!sprites.TryAdd(entry.Name, entry.Sprite))
                    throw new InvalidOperationException($"Duplicate external sprite name '{entry.Name}'.");
            }
        }
    }
}
