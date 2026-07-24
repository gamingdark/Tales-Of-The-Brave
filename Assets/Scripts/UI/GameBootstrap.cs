using UnityEngine;
using TalesOfTheBrave.Simulation.Core;
using TalesOfTheBrave.Graphics;
using TalesOfTheBrave.Simulation.Rulesets;

namespace TalesOfTheBrave.Unity.UI
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Map Graphics")]
        [SerializeField] private ExternalGraphicsCatalog externalGraphics;
        [SerializeField] private Sprite locationWindowFrame;
        [SerializeField] private Material locationEntityraitMaterial;
        [SerializeField] private Shader roundedMapShader;
        [SerializeField, Min(0.01f)] private float mapIconScale = 0.4f;

        [Header("Layout")]
        [SerializeField] private Camera layoutCamera;
        [SerializeField] private Transform layoutDivider;

        public GameContext Context { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrapExists()
        {
            if (FindFirstObjectByType<GameBootstrap>() != null) return;
            new GameObject("Tales of the Brave").AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            ResolveLayoutReferences();
            var definition = WorldDefinition.CreateDefault();
            WorldDefinitionValidator.Validate(definition, externalGraphics);
            Context = GameFactory.Create(definition);
            var mapController = gameObject.AddComponent<MapEntitySceneController>();
            mapController.Initialize(
                Context,
                layoutCamera,
                layoutDivider,
                externalGraphics,
                definition.MapBackgroundSprite,
                definition.SceneBackgroundSprite,
                definition.MapWidth,
                definition.MapHeight,
                roundedMapShader,
                mapIconScale);
            gameObject.AddComponent<GameplayUI>().Initialize(
                Context,
                layoutCamera,
                layoutDivider,
                externalGraphics,
                locationWindowFrame,
                locationEntityraitMaterial,
                definition.UI,
                definition.TimeSystem,
                mapController);
        }

        private void ResolveLayoutReferences()
        {
            if (layoutCamera == null)
                layoutCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (layoutDivider == null)
            {
                var dividerObject = GameObject.Find("Layout Divider");
                if (dividerObject != null) layoutDivider = dividerObject.transform;
            }
        }

        private void Update() => Context.Tick(UnityEngine.Time.deltaTime);

#if UNITY_EDITOR
        private void OnValidate()
        {
            var changed = false;
            if (layoutCamera == null)
            {
                layoutCamera = Camera.main ?? FindFirstObjectByType<Camera>();
                changed = layoutCamera != null;
            }
            if (locationEntityraitMaterial == null)
            {
                locationEntityraitMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Graphics/LocationPortrait.mat");
                changed = locationEntityraitMaterial != null;
            }
            if (roundedMapShader == null)
            {
                roundedMapShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(
                    "Assets/Shaders/RoundedSprite.shader");
                changed = changed || roundedMapShader != null;
            }
            if (locationWindowFrame == null)
            {
                var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Graphics/window-location.png");
                Sprite largest = null;
                foreach (var asset in assets)
                {
                    if (!(asset is Sprite sprite)) continue;
                    if (largest == null || sprite.rect.width * sprite.rect.height > largest.rect.width * largest.rect.height)
                        largest = sprite;
                }
                if (largest != null)
                {
                    locationWindowFrame = largest;
                    changed = true;
                }
            }
            if (changed) UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
