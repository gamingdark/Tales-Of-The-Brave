using UnityEngine;
using TalesOfVoyages.Simulation.Core;

namespace TalesOfVoyages.Unity.UI
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Map Icons")]
        [SerializeField] private Sprite klaipedaIcon;
        [SerializeField] private Sprite rigaIcon;
        [SerializeField] private Sprite helsinkiIcon;
        [SerializeField] private Sprite playerShipIcon;
        [SerializeField, Min(0.01f)] private float mapIconScale = 0.4f;

        [Header("Map Bounds")]
        [SerializeField] private Transform mapBottomLeft;
        [SerializeField] private Transform mapTopRight;

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
            Context = MvpGameFactory.Create();
            gameObject.AddComponent<MvpDemoUI>().Initialize(Context, mapBottomLeft, mapTopRight);
            gameObject.AddComponent<MapEntitySceneController>().Initialize(
                Context, mapBottomLeft, mapTopRight,
                klaipedaIcon, rigaIcon, helsinkiIcon, playerShipIcon, mapIconScale);
        }

        private void Update() => Context.Time.Tick(UnityEngine.Time.deltaTime);
    }
}
