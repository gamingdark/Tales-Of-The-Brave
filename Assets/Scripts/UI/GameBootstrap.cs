using UnityEngine;
using TalesOfVoyages.Simulation.Core;
using TalesOfVoyages.Graphics;
using TalesOfVoyages.Simulation.Rulesets;

namespace TalesOfVoyages.Unity.UI
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Map Graphics")]
        [SerializeField] private ExternalGraphicsCatalog externalGraphics;
        [SerializeField] private Sprite portWindowFrame;
        [SerializeField] private Material portPortraitMaterial;
        [SerializeField] private Shader roundedMapShader;
        [SerializeField, Min(0.01f)] private float mapIconScale = 0.4f;

        [Header("Map Bounds")]
        [SerializeField] private Transform mapBottomLeft;
        [SerializeField] private Transform mapTopRight;

        [Header("Left Menu Bounds")]
        [SerializeField] private Transform leftMenuBottomLeft;
        [SerializeField] private Transform leftMenuTopRight;

        [Header("Bottom Menu Bounds")]
        [SerializeField] private Transform bottomMenuBottomLeft;
        [SerializeField] private Transform bottomMenuTopRight;

        public GameContext Context { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrapExists()
        {
            if (FindFirstObjectByType<GameBootstrap>() != null) return;
            new GameObject("Tales of Voyages MVP").AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            var definition = MvpWorldDefinition.CreateDefault();
            MvpWorldDefinitionValidator.Validate(definition, externalGraphics);
            Context = MvpGameFactory.Create(definition);
            var mapController = gameObject.AddComponent<MapEntitySceneController>();
            mapController.Initialize(
                Context,
                mapBottomLeft,
                mapTopRight,
                externalGraphics,
                definition.MapBackgroundSprite,
                definition.SceneBackgroundSprite,
                roundedMapShader,
                mapIconScale);
            gameObject.AddComponent<MvpDemoUI>().Initialize(
                Context,
                mapBottomLeft,
                mapTopRight,
                leftMenuBottomLeft,
                leftMenuTopRight,
                bottomMenuBottomLeft,
                bottomMenuTopRight,
                externalGraphics,
                portWindowFrame,
                portPortraitMaterial,
                mapController);
        }

        private void Update() => Context.Tick(UnityEngine.Time.deltaTime);

#if UNITY_EDITOR
        private void OnValidate()
        {
            var changed = false;
            if (portPortraitMaterial == null)
            {
                portPortraitMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Graphics/PortPortrait.mat");
                changed = portPortraitMaterial != null;
            }
            if (roundedMapShader == null)
            {
                roundedMapShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(
                    "Assets/Shaders/RoundedSprite.shader");
                changed = changed || roundedMapShader != null;
            }
            if (portWindowFrame == null)
            {
                var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Graphics/window-port.png");
                Sprite largest = null;
                foreach (var asset in assets)
                {
                    if (!(asset is Sprite sprite)) continue;
                    if (largest == null || sprite.rect.width * sprite.rect.height > largest.rect.width * largest.rect.height)
                        largest = sprite;
                }
                if (largest != null)
                {
                    portWindowFrame = largest;
                    changed = true;
                }
            }
            if (changed) UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
