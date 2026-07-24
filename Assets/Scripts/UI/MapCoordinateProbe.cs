using UnityEngine;

namespace TalesOfTheBrave.Unity.UI
{
    [ExecuteAlways]
    public sealed class MapCoordinateProbe : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Camera layoutCamera;
        [SerializeField] private Transform layoutDivider;

        [Header("Live Coordinate")]
        [SerializeField] private Vector2 normalizedMapPosition;
        [SerializeField] private bool isInsideMapBounds;
        private MapEntitySceneController mapController;

        public Vector2 NormalizedMapPosition => normalizedMapPosition;
        public bool IsInsideMapBounds => isInsideMapBounds;

        private void Update() => RefreshCoordinate();

        private void OnValidate() => RefreshCoordinate();

        private void RefreshCoordinate()
        {
            ResolveLayoutReferences();
            if (Application.isPlaying && mapController == null)
                mapController = FindFirstObjectByType<MapEntitySceneController>();
            var hasBounds = mapController != null
                ? mapController.TryGetMapWorldBounds(out var lower, out var upper)
                : ScreenLayout.TryGetMainWorldBounds(
                    layoutCamera,
                    layoutDivider,
                    out lower,
                    out upper);
            if (!hasBounds)
                return;
            normalizedMapPosition = new Vector2(
                InverseLerpUnclamped(lower.x, upper.x, transform.position.x),
                InverseLerpUnclamped(lower.y, upper.y, transform.position.y));
            isInsideMapBounds =
                normalizedMapPosition.x >= 0f && normalizedMapPosition.x <= 1f &&
                normalizedMapPosition.y >= 0f && normalizedMapPosition.y <= 1f;
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

        private static float InverseLerpUnclamped(float a, float b, float value)
        {
            if (Mathf.Approximately(a, b)) return 0f;
            return (value - a) / (b - a);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isInsideMapBounds ? Color.cyan : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.12f);
            Gizmos.DrawLine(transform.position + Vector3.left * 0.2f, transform.position + Vector3.right * 0.2f);
            Gizmos.DrawLine(transform.position + Vector3.down * 0.2f, transform.position + Vector3.up * 0.2f);
        }
    }
}
