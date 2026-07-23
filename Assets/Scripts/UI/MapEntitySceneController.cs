using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TalesOfVoyages.Simulation.Core;
using TalesOfVoyages.Simulation.Movement;
using TalesOfVoyages.Simulation.World;
using TalesOfVoyages.Graphics;
using TalesOfVoyages.Simulation.Entities;

namespace TalesOfVoyages.Unity.UI
{
    public sealed class MapEntitySceneController : MonoBehaviour
    {
        private readonly Dictionary<string, MapEntityView> views = new Dictionary<string, MapEntityView>();
        private readonly Dictionary<string, LineRenderer> routeRenderers = new Dictionary<string, LineRenderer>();
        private readonly Dictionary<string, SpriteRenderer> nodeRenderers = new Dictionary<string, SpriteRenderer>();
        private readonly Dictionary<string, SpriteRenderer> nodeHighlights = new Dictionary<string, SpriteRenderer>();
        private readonly Dictionary<string, Transform> nodeAnchors = new Dictionary<string, Transform>();
        private GameContext context;
        private Transform bottomLeft;
        private Transform topRight;
        private ExternalGraphicsCatalog graphics;
        private float iconScale;
        private Transform entityRoot;
        private Material routeMaterial;
        private Material roundedMapMaterial;
        private Shader roundedMapShader;
        private Texture2D highlightTexture;
        private Sprite highlightSprite;
        private SpriteRenderer sceneBackgroundRenderer;
        private string hoveredNodeId;

        public string HoveredNodeId => hoveredNodeId;
        public string SelectedNodeId { get; private set; }

        public void Initialize(
            GameContext gameContext,
            Transform mapBottomLeft,
            Transform mapTopRight,
            ExternalGraphicsCatalog graphicsCatalog,
            string mapBackgroundSprite,
            string sceneBackgroundSprite,
            Shader mapPanelShader,
            float mapIconScale)
        {
            context = gameContext ?? throw new ArgumentNullException(nameof(gameContext));
            bottomLeft = mapBottomLeft;
            topRight = mapTopRight;
            graphics = graphicsCatalog ?? throw new ArgumentNullException(nameof(graphicsCatalog));
            roundedMapShader = mapPanelShader;
            iconScale = Mathf.Max(0.01f, mapIconScale);

            entityRoot = new GameObject("Runtime Map Entities").transform;
            entityRoot.SetParent(transform, false);
            CreateSceneBackground(graphics.GetSprite(sceneBackgroundSprite));
            CreateMapBackground(graphics.GetSprite(mapBackgroundSprite));
            CreateRouteViews();
            CreateViews();
            RefreshViews();
        }

        private void CreateMapBackground(Sprite sprite)
        {
            if (sprite == null || bottomLeft == null || topRight == null) return;

            var background = new GameObject("Map Background");
            background.transform.SetParent(entityRoot, false);
            var renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 0;

            var lower = bottomLeft.position;
            var upper = topRight.position;
            var width = Mathf.Abs(upper.x - lower.x);
            var height = Mathf.Abs(upper.y - lower.y);
            background.transform.position = new Vector3(
                (lower.x + upper.x) * 0.5f,
                (lower.y + upper.y) * 0.5f,
                Mathf.Max(lower.z, upper.z) + 0.1f);
            background.transform.localScale = new Vector3(
                width / sprite.bounds.size.x,
                height / sprite.bounds.size.y,
                1f);

            var roundedShader = roundedMapShader ?? Shader.Find("TalesOfVoyages/Rounded Sprite");
            if (roundedShader != null)
            {
                roundedMapMaterial = new Material(roundedShader) { name = "Runtime Rounded Map Material" };
                roundedMapMaterial.SetFloat("_Aspect", width / height);
                roundedMapMaterial.SetFloat("_Radius", 0.018f);
                renderer.sharedMaterial = roundedMapMaterial;
            }
        }

        private void Update()
        {
            if (context == null) return;
            RefreshSceneBackground();
            RefreshViews();
        }

        private void CreateSceneBackground(Sprite sprite)
        {
            var background = new GameObject("Scene Background");
            background.transform.SetParent(entityRoot, false);
            sceneBackgroundRenderer = background.AddComponent<SpriteRenderer>();
            sceneBackgroundRenderer.sprite = sprite;
            sceneBackgroundRenderer.sortingOrder = -10;
            RefreshSceneBackground();
        }

        private void RefreshSceneBackground()
        {
            if (sceneBackgroundRenderer == null || sceneBackgroundRenderer.sprite == null) return;
            var camera = Camera.main;
            if (camera == null || !camera.orthographic) return;

            var height = camera.orthographicSize * 2f;
            var width = height * camera.aspect;
            var background = sceneBackgroundRenderer.transform;
            background.position = new Vector3(
                camera.transform.position.x,
                camera.transform.position.y,
                camera.transform.position.z + camera.farClipPlane * 0.5f);
            background.localScale = new Vector3(
                width / sceneBackgroundRenderer.sprite.bounds.size.x,
                height / sceneBackgroundRenderer.sprite.bounds.size.y,
                1f);
        }

        private void OnGUI()
        {
            if (context == null) return;

            hoveredNodeId = FindNodeAtGuiPosition(Event.current.mousePosition);
            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 1)
                {
                    SelectedNodeId = null;
                    Event.current.Use();
                }
                else if (Event.current.button == 0 && hoveredNodeId != null)
                {
                    SelectedNodeId = hoveredNodeId;
                    Event.current.Use();
                }
            }
            RefreshNodeHighlights();
        }

        private void CreateViews()
        {
            foreach (var entity in context.Entities)
            {
                if (entity.HasBehavior<PlayerControlledBehavior>()) continue;
                var drawable = entity.GetBehavior<DrawableBehavior>();
                var view = CreateView(entity.Id, graphics.GetSprite(drawable.MapIconSprite), 10);
                view.InitializeEntity(entity);
                var worldEntity = entity.GetBehavior<WorldEntityBehavior>();
                var renderer = view.GetComponentInChildren<SpriteRenderer>();
                if (renderer != null && !nodeRenderers.ContainsKey(worldEntity.StartingNodeId))
                {
                    nodeRenderers.Add(worldEntity.StartingNodeId, renderer);
                    nodeAnchors.Add(worldEntity.StartingNodeId, view.transform);
                    nodeHighlights.Add(
                        worldEntity.StartingNodeId,
                        CreateNodeHighlight(view.transform, renderer.sprite));
                }
            }
            CreateMissingNodeInteractions();

            var ship = context.PlayerShip;
            var shipView = CreateView(ship.Id, graphics.GetSprite(ship.MapIconSprite), 20);
            shipView.InitializeTransport(ship);
        }

        private void CreateMissingNodeInteractions()
        {
            foreach (var node in context.World.Nodes)
            {
                if (!node.IsDiscovered || nodeAnchors.ContainsKey(node.Id)) continue;

                var anchor = new GameObject($"Node - {node.DisplayName}").transform;
                anchor.SetParent(entityRoot, false);
                var highlight = CreateNodeHighlight(anchor, null);
                nodeAnchors.Add(node.Id, anchor);
                nodeHighlights.Add(node.Id, highlight);
                nodeRenderers.Add(node.Id, highlight);
            }
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

        private SpriteRenderer CreateNodeHighlight(Transform nodeTransform, Sprite iconSprite)
        {
            EnsureHighlightSprite();
            var highlight = new GameObject("Node Highlight").transform;
            highlight.SetParent(nodeTransform, false);
            highlight.localPosition = iconSprite == null
                ? Vector3.zero
                : -iconSprite.bounds.center * iconScale;
            highlight.localScale = new Vector3(
                iconSprite == null ? 0.3f : iconSprite.bounds.size.x * iconScale + 0.2f,
                iconSprite == null ? 0.3f : iconSprite.bounds.size.y * iconScale + 0.2f,
                1f);
            var renderer = highlight.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = highlightSprite;
            renderer.sortingOrder = 9;
            renderer.enabled = false;
            return renderer;
        }

        private void EnsureHighlightSprite()
        {
            if (highlightSprite != null) return;
            highlightTexture = new Texture2D(1, 1) { name = "Runtime Node Highlight Texture" };
            highlightTexture.SetPixel(0, 0, Color.white);
            highlightTexture.Apply();
            highlightSprite = Sprite.Create(
                highlightTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            highlightSprite.name = "Runtime Node Highlight";
        }

        private string FindNodeAtGuiPosition(Vector2 guiPosition)
        {
            var camera = Camera.main;
            if (camera == null) return null;

            foreach (var pair in nodeRenderers)
            {
                var bounds = pair.Value.bounds;
                var a = camera.WorldToScreenPoint(bounds.min);
                var b = camera.WorldToScreenPoint(bounds.max);
                var rect = Rect.MinMaxRect(
                    Mathf.Min(a.x, b.x) - 4f,
                    Screen.height - Mathf.Max(a.y, b.y) - 4f,
                    Mathf.Max(a.x, b.x) + 4f,
                    Screen.height - Mathf.Min(a.y, b.y) + 4f);
                if (rect.Contains(guiPosition)) return pair.Key;
            }
            return null;
        }

        private void RefreshNodeHighlights()
        {
            foreach (var pair in nodeHighlights)
            {
                var selected = pair.Key == SelectedNodeId;
                var hovered = pair.Key == hoveredNodeId;
                pair.Value.enabled = selected || hovered;
                if (selected) pair.Value.color = new Color(1f, 0.82f, 0.15f, 0.85f);
                else if (hovered) pair.Value.color = new Color(0.45f, 0.45f, 0.45f, 0.8f);
            }
        }

        private void RefreshViews()
        {
            if (bottomLeft == null || topRight == null) return;
            var highlightedEdges = GetHighlightedRouteEdges();
            foreach (var edge in context.World.Edges)
            {
                var nodeA = context.World.GetNode(edge.NodeAId);
                var nodeB = context.World.GetNode(edge.NodeBId);
                var line = routeRenderers[edge.Id];
                var routeColor = highlightedEdges.Contains(edge.Id)
                    ? new Color(0.12f, 0.78f, 0.24f, 0.95f)
                    : new Color(0.3f, 0.25f, 0.2f, 0.75f);
                line.startColor = routeColor;
                line.endColor = routeColor;
                line.SetPosition(0, MapToWorld(nodeA.MapX, nodeA.MapY, 0f));
                for (var i = 0; i < edge.MapWaypoints.Count; i++)
                {
                    var waypoint = edge.MapWaypoints[i];
                    line.SetPosition(i + 1, MapToWorld(waypoint.X, waypoint.Y, 0f));
                }
                line.SetPosition(line.positionCount - 1, MapToWorld(nodeB.MapX, nodeB.MapY, 0f));
            }

            foreach (var entity in context.Entities)
            {
                if (entity.HasBehavior<PlayerControlledBehavior>()) continue;
                var node = context.World.GetNode(entity.GetBehavior<WorldEntityBehavior>().StartingNodeId);
                views[entity.Id].transform.position = MapToWorld(node.MapX, node.MapY);
            }

            foreach (var pair in nodeAnchors)
            {
                if (nodeRenderers[pair.Key] != nodeHighlights[pair.Key]) continue;
                var node = context.World.GetNode(pair.Key);
                pair.Value.position = MapToWorld(node.MapX, node.MapY);
            }

            var ship = context.PlayerShip;
            var shipView = views[ship.Id];
            shipView.RefreshTransport(ship, context.Time.DayProgress);
            shipView.transform.position = GetTransportPosition(ship);
        }

        private HashSet<string> GetHighlightedRouteEdges()
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            var travel = context.PlayerShip.Travel;
            IReadOnlyList<string> routeNodes = null;

            if (travel.IsTravelling)
            {
                routeNodes = new[] { travel.CurrentNodeId }
                    .Concat(travel.RemainingRoute)
                    .ToArray();
            }
            else
            {
                var destinationNodeId = travel.HasPlannedAction
                    ? travel.PlannedDestinationNodeId
                    : SelectedNodeId;
                if (!string.IsNullOrWhiteSpace(destinationNodeId) &&
                    destinationNodeId != travel.CurrentNodeId)
                    routeNodes = context.World.FindRoute(travel.CurrentNodeId, destinationNodeId);
            }

            if (routeNodes == null) return result;
            for (var i = 1; i < routeNodes.Count; i++)
            {
                var edge = context.World.GetConnectingEdge(routeNodes[i - 1], routeNodes[i]);
                if (edge != null) result.Add(edge.Id);
            }
            return result;
        }

        private Vector3 GetTransportPosition(Transport transport)
        {
            var travel = transport.Travel;
            if (!travel.IsTravelling)
            {
                var node = context.World.GetNode(travel.CurrentNodeId);
                return MapToWorld(node.MapX, node.MapY);
            }

            var visualSegment = travel.GetVisualSegment(context.Time.DayProgress);
            var edge = context.World.GetEdge(visualSegment?.EdgeId ?? travel.CurrentEdgeId);
            var progress = travel.GetVisualEdgeProgress(context.Time.DayProgress);
            var originNodeId = visualSegment?.FromNodeId ?? travel.CurrentNodeId;
            if (originNodeId == edge.NodeBId) progress = 1f - progress;
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
            if (roundedMapMaterial != null) Destroy(roundedMapMaterial);
            if (highlightSprite != null) Destroy(highlightSprite);
            if (highlightTexture != null) Destroy(highlightTexture);
        }
    }
}
