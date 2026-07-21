using System;
using System.Collections.Generic;
using UnityEngine;
using TalesOfVoyages.Simulation.Core;
using TalesOfVoyages.Simulation.Movement;
using TalesOfVoyages.Simulation.World;

namespace TalesOfVoyages.Unity.UI
{
    public sealed class MapEntitySceneController : MonoBehaviour
    {
        private readonly Dictionary<string, MapEntityView> views = new Dictionary<string, MapEntityView>();
        private readonly Dictionary<string, LineRenderer> routeRenderers = new Dictionary<string, LineRenderer>();
        private GameContext context;
        private Transform bottomLeft;
        private Transform topRight;
        private Sprite klaipedaIcon;
        private Sprite rigaIcon;
        private Sprite helsinkiIcon;
        private Sprite playerShipIcon;
        private float iconScale;
        private Transform entityRoot;
        private Material routeMaterial;

        public void Initialize(
            GameContext gameContext,
            Transform mapBottomLeft,
            Transform mapTopRight,
            Sprite klaipedaSprite,
            Sprite rigaSprite,
            Sprite helsinkiSprite,
            Sprite playerShipSprite,
            float mapIconScale)
        {
            context = gameContext ?? throw new ArgumentNullException(nameof(gameContext));
            bottomLeft = mapBottomLeft;
            topRight = mapTopRight;
            klaipedaIcon = klaipedaSprite;
            rigaIcon = rigaSprite;
            helsinkiIcon = helsinkiSprite;
            playerShipIcon = playerShipSprite;
            iconScale = Mathf.Max(0.01f, mapIconScale);

            entityRoot = new GameObject("Runtime Map Entities").transform;
            entityRoot.SetParent(transform, false);
            CreateRouteViews();
            CreateViews();
            RefreshViews();
        }

        private void Update()
        {
            if (context != null) RefreshViews();
        }

        private void CreateViews()
        {
            foreach (var node in context.World.Nodes)
            {
                var icon = node.Id == "port_klaipeda" ? klaipedaIcon
                    : node.Id == "port_riga" ? rigaIcon
                    : node.Id == "port_helsinki" ? helsinkiIcon
                    : null;
                var view = CreateView(node.Id, icon, 10);
                view.InitializePort(node);
            }

            var ship = context.PlayerShip;
            var shipView = CreateView(ship.Id, playerShipIcon, 20);
            shipView.InitializeTransport(ship);
        }

        private void CreateRouteViews()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null) routeMaterial = new Material(shader) { name = "Runtime Route Material" };

            foreach (var edge in context.World.Edges)
            {
                var routeObject = new GameObject($"Route - {edge.Id}");
                routeObject.transform.SetParent(entityRoot, false);
                var line = routeObject.AddComponent<LineRenderer>();
                line.positionCount = edge.MapWaypoints.Count + 2;
                line.useWorldSpace = true;
                line.startWidth = 0.05f;
                line.endWidth = 0.05f;
                line.startColor = new Color(0.3f, 0.25f, 0.2f, 0.75f);
                line.endColor = line.startColor;
                line.numCapVertices = 2;
                line.sortingOrder = 5;
                if (routeMaterial != null) line.sharedMaterial = routeMaterial;
                routeRenderers.Add(edge.Id, line);
            }
        }

        private MapEntityView CreateView(string id, Sprite sprite, int sortingOrder)
        {
            var entityObject = new GameObject(id);
            entityObject.transform.SetParent(entityRoot, false);
            var view = entityObject.AddComponent<MapEntityView>();
            AddCenteredSprite(entityObject.transform, sprite, sortingOrder);
            views.Add(id, view);
            return view;
        }

        private void AddCenteredSprite(Transform entityTransform, Sprite sprite, int sortingOrder)
        {
            if (sprite == null) return;
            var visual = new GameObject("Visual").transform;
            visual.SetParent(entityTransform, false);
            visual.localScale = Vector3.one * iconScale;
            visual.localPosition = -sprite.bounds.center * iconScale;
            var renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
        }

        private void RefreshViews()
        {
            if (bottomLeft == null || topRight == null) return;
            foreach (var edge in context.World.Edges)
            {
                var nodeA = context.World.GetNode(edge.NodeAId);
                var nodeB = context.World.GetNode(edge.NodeBId);
                var line = routeRenderers[edge.Id];
                line.SetPosition(0, MapToWorld(nodeA.MapX, nodeA.MapY, 0f));
                for (var i = 0; i < edge.MapWaypoints.Count; i++)
                {
                    var waypoint = edge.MapWaypoints[i];
                    line.SetPosition(i + 1, MapToWorld(waypoint.X, waypoint.Y, 0f));
                }
                line.SetPosition(line.positionCount - 1, MapToWorld(nodeB.MapX, nodeB.MapY, 0f));
            }

            foreach (var node in context.World.Nodes)
                views[node.Id].transform.position = MapToWorld(node.MapX, node.MapY);

            var ship = context.PlayerShip;
            var shipView = views[ship.Id];
            shipView.RefreshTransport(ship, context.Time.DayProgress);
            shipView.transform.position = GetTransportPosition(ship);
        }

        private Vector3 GetTransportPosition(Transport transport)
        {
            var travel = transport.Travel;
            if (!travel.IsTravelling)
            {
                var node = context.World.GetNode(travel.CurrentNodeId);
                return MapToWorld(node.MapX, node.MapY);
            }

            var edge = context.World.GetEdge(travel.CurrentEdgeId);
            var progress = travel.GetVisualEdgeProgress(context.Time.DayProgress);
            if (travel.CurrentNodeId == edge.NodeBId) progress = 1f - progress;
            return GetPositionAlongEdge(edge, progress);
        }

        private Vector3 GetPositionAlongEdge(WorldEdge edge, float progressFromNodeA)
        {
            var points = new Vector3[edge.MapWaypoints.Count + 2];
            var nodeA = context.World.GetNode(edge.NodeAId);
            var nodeB = context.World.GetNode(edge.NodeBId);
            points[0] = MapToWorld(nodeA.MapX, nodeA.MapY);
            for (var i = 0; i < edge.MapWaypoints.Count; i++)
                points[i + 1] = MapToWorld(edge.MapWaypoints[i].X, edge.MapWaypoints[i].Y);
            points[points.Length - 1] = MapToWorld(nodeB.MapX, nodeB.MapY);

            var totalLength = 0f;
            for (var i = 1; i < points.Length; i++) totalLength += Vector3.Distance(points[i - 1], points[i]);
            var targetDistance = Mathf.Clamp01(progressFromNodeA) * totalLength;
            for (var i = 1; i < points.Length; i++)
            {
                var segmentLength = Vector3.Distance(points[i - 1], points[i]);
                if (targetDistance <= segmentLength)
                    return Vector3.Lerp(points[i - 1], points[i], segmentLength <= 0f ? 0f : targetDistance / segmentLength);
                targetDistance -= segmentLength;
            }
            return points[points.Length - 1];
        }

        private Vector3 MapToWorld(float normalizedX, float normalizedY, float depthOffset = -0.1f)
        {
            var lower = bottomLeft.position;
            var upper = topRight.position;
            return new Vector3(
                Mathf.Lerp(lower.x, upper.x, normalizedX),
                Mathf.Lerp(lower.y, upper.y, normalizedY),
                Mathf.Min(lower.z, upper.z) + depthOffset);
        }

        private void OnDestroy()
        {
            if (routeMaterial != null) Destroy(routeMaterial);
        }
    }
}
