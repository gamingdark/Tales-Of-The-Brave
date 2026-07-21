using UnityEngine;

namespace TalesOfVoyages.Unity.UI
{
    [ExecuteAlways]
    public sealed class MapCoordinateProbe : MonoBehaviour
    {
        [Header("Map Bounds")]
        [SerializeField] private Transform bottomLeft;
        [SerializeField] private Transform topRight;

        [Header("Live Coordinate")]
        [SerializeField] private Vector2 normalizedMapPosition;
        [SerializeField] private bool isInsideMapBounds;

        public Vector2 NormalizedMapPosition => normalizedMapPosition;
        public bool IsInsideMapBounds => isInsideMapBounds;

        private void Update() => RefreshCoordinate();

        private void OnValidate() => RefreshCoordinate();

        private void RefreshCoordinate()
        {
            if (bottomLeft == null || topRight == null) return;

            var lower = bottomLeft.position;
            var upper = topRight.position;
            normalizedMapPosition = new Vector2(
                InverseLerpUnclamped(lower.x, upper.x, transform.position.x),
                InverseLerpUnclamped(lower.y, upper.y, transform.position.y));
            isInsideMapBounds =
                normalizedMapPosition.x >= 0f && normalizedMapPosition.x <= 1f &&
                normalizedMapPosition.y >= 0f && normalizedMapPosition.y <= 1f;
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
