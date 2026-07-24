using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TalesOfTheBrave.Simulation.Core;
using TalesOfTheBrave.Simulation.Movement;
using TalesOfTheBrave.Simulation.World;
using TalesOfTheBrave.Graphics;
using TalesOfTheBrave.Simulation.Entities;

namespace TalesOfTheBrave.Unity.UI
{
    public sealed class MapEntitySceneController : MonoBehaviour
    {
        private readonly Dictionary<string, MapEntityView> views = new Dictionary<string, MapEntityView>();
        private readonly Dictionary<string, LineRenderer> routeRenderers = new Dictionary<string, LineRenderer>();
        private readonly Dictionary<string, SpriteRenderer> nodeRenderers = new Dictionary<string, SpriteRenderer>();
        private readonly Dictionary<string, SpriteRenderer> nodeHighlights = new Dictionary<string, SpriteRenderer>();
        private readonly Dictionary<string, Transform> nodeAnchors = new Dictionary<string, Transform>();
        private GameContext context;
        private Camera mapCamera;
        private Transform layoutDivider;
        private ExternalGraphicsCatalog graphics;
        private float iconScale;
        private Transform entityRoot;
        private Transform mapContentRoot;
        private bool mapVisible = true;
        private Material routeMaterial;
        private Shader roundedMapShader;
        private float mapWidth;
        private float mapHeight;
        private Vector2 mapOffset;
        private Vector2 targetMapOffset;
        private Vector2 mapPanVelocity;
        private bool isCenteringMap;
        private bool isDraggingMap;
        private bool mapDragMoved;
        private Vector2 dragStartPosition;
        private Vector2 lastDragPosition;
        private string pressedNodeId;
        private string lastClickedNodeId;
        private float lastNodeClickTime = -1f;
        private float suppressHoverUntil;
        private bool touchInputDetected;
        private Texture2D highlightTexture;
        private Sprite highlightSprite;
        private Texture2D mapMaskTexture;
        private Sprite mapMaskSprite;
        private SpriteMask mapMask;
        private SpriteRenderer sceneBackgroundRenderer;
        private SpriteRenderer mapBackgroundRenderer;
        private string hoveredNodeId;

        public string HoveredNodeId => hoveredNodeId;
        public string HoveredTooltip { get; private set; }
        public string SelectedNodeId { get; private set; }

        public void Initialize(
            GameContext gameContext,
            Camera camera,
            Transform mapLayoutDivider,
            ExternalGraphicsCatalog graphicsCatalog,
            string mapBackgroundSprite,
            string sceneBackgroundSprite,
            float configuredMapWidth,
            float configuredMapHeight,
            Shader mapPanelShader,
            float mapIconScale)
        {
            context = gameContext ?? throw new ArgumentNullException(nameof(gameContext));
            mapCamera = camera;
            layoutDivider = mapLayoutDivider;
            graphics = graphicsCatalog ?? throw new ArgumentNullException(nameof(graphicsCatalog));
            roundedMapShader = mapPanelShader;
            mapWidth = configuredMapWidth;
            mapHeight = configuredMapHeight;
            iconScale = Mathf.Max(0.01f, mapIconScale);
            CenterOnNode(context.PlayerShip.Travel.CurrentNodeId, true);

            entityRoot = new GameObject("Runtime Map Entities").transform;
            entityRoot.SetParent(transform, false);
            mapContentRoot = new GameObject("Map Content").transform;
            mapContentRoot.SetParent(entityRoot, false);
            CreateSceneBackground(graphics.GetSprite(sceneBackgroundSprite));
            CreateMapMask();
            CreateMapBackground(graphics.GetSprite(mapBackgroundSprite));
            CreateRouteViews();
            CreateViews();
            RefreshViews();
        }

        public void SetMapVisible(bool visible)
        {
            mapVisible = visible;
            if (mapContentRoot != null && mapContentRoot.gameObject.activeSelf != visible)
                mapContentRoot.gameObject.SetActive(visible);
            if (!visible)
            {
                hoveredNodeId = null;
                HoveredTooltip = null;
            }
        }

        public void ClearSelection() => SelectedNodeId = null;

        private void CreateMapBackground(Sprite sprite)
        {
            if (sprite == null || layoutDivider == null) return;

            var background = new GameObject("Map Background");
            background.transform.SetParent(mapContentRoot, false);
            mapBackgroundRenderer = background.AddComponent<SpriteRenderer>();
            mapBackgroundRenderer.sprite = sprite;
            mapBackgroundRenderer.sortingOrder = 0;
            mapBackgroundRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            RefreshMapBackground();
        }

        private void CreateMapMask()
        {
            const int size = 64;
            const float radius = 4f;
            mapMaskTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Runtime Rounded Map Mask",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var nearestX = Mathf.Clamp(x + 0.5f, radius, size - radius);
                var nearestY = Mathf.Clamp(y + 0.5f, radius, size - radius);
                var dx = x + 0.5f - nearestX;
                var dy = y + 0.5f - nearestY;
                mapMaskTexture.SetPixel(
                    x,
                    y,
                    dx * dx + dy * dy <= radius * radius ? Color.white : Color.clear);
            }
            mapMaskTexture.Apply();
            mapMaskSprite = Sprite.Create(
                mapMaskTexture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            var maskObject = new GameObject("Map Viewport Mask");
            maskObject.transform.SetParent(mapContentRoot, false);
            mapMask = maskObject.AddComponent<SpriteMask>();
            mapMask.sprite = mapMaskSprite;
            mapMask.isCustomRangeActive = true;
            mapMask.backSortingOrder = -1;
            mapMask.frontSortingOrder = 30;
            mapMask.alphaCutoff = 0.1f;
        }

        private void Update()
        {
            if (context == null) return;
            UpdateMapCenterAnimation();
            RefreshSceneBackground();
            RefreshMapBackground();
            RefreshViews();
        }

        private void RefreshMapBackground()
        {
            if (mapBackgroundRenderer == null || mapBackgroundRenderer.sprite == null ||
                !TryGetMapWorldBounds(out var lower, out var upper))
                return;

            var width = Mathf.Abs(upper.x - lower.x);
            var height = Mathf.Abs(upper.y - lower.y);
            var background = mapBackgroundRenderer.transform;
            background.position = new Vector3(
                (lower.x + upper.x) * 0.5f,
                (lower.y + upper.y) * 0.5f,
                Mathf.Max(lower.z, upper.z) + 0.1f);
            background.localScale = new Vector3(
                width / mapBackgroundRenderer.sprite.bounds.size.x,
                height / mapBackgroundRenderer.sprite.bounds.size.y,
                1f);
            RefreshMapMask();
        }

        private void RefreshMapMask()
        {
            if (mapMask == null ||
                !TryGetMapViewportBounds(out var lower, out var upper)) return;
            var maskTransform = mapMask.transform;
            maskTransform.position = new Vector3(
                (lower.x + upper.x) * 0.5f,
                (lower.y + upper.y) * 0.5f,
                lower.z);
            maskTransform.localScale = new Vector3(
                Mathf.Abs(upper.x - lower.x),
                Mathf.Abs(upper.y - lower.y),
                1f);
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
            var camera = mapCamera;
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
            if (context == null || !mapVisible) return;

            var currentEvent = Event.current;
            // The encounter overlay owns input in the map viewport while an
            // interaction is pending. Do not consume its button events as map
            // dragging or node selection.
            if (context.GetPendingInteractionEntity() != null)
            {
                hoveredNodeId = null;
                HoveredTooltip = null;
                RefreshNodeHighlights();
                return;
            }
            var mousePosition = currentEvent.mousePosition;
            if (Input.touchCount > 0)
            {
                touchInputDetected = true;
                suppressHoverUntil = UnityEngine.Time.unscaledTime + 0.5f;
            }
            var insideViewport = IsInsideMapViewport(mousePosition);
            hoveredNodeId = insideViewport &&
                            !isDraggingMap &&
                            !touchInputDetected &&
                            UnityEngine.Time.unscaledTime >= suppressHoverUntil
                ? FindNodeAtGuiPosition(mousePosition)
                : null;
            HoveredTooltip = string.IsNullOrWhiteSpace(hoveredNodeId)
                ? null
                : context.World.GetNode(hoveredNodeId).DisplayName;
            if (currentEvent.type == EventType.MouseDown)
            {
                if (currentEvent.button == 1)
                {
                    SelectedNodeId = null;
                    currentEvent.Use();
                }
                else if (currentEvent.button == 0 && insideViewport)
                {
                    isDraggingMap = true;
                    mapDragMoved = false;
                    dragStartPosition = mousePosition;
                    lastDragPosition = mousePosition;
                    pressedNodeId = FindNodeAtGuiPosition(mousePosition);
                    currentEvent.Use();
                }
            }
            else if (currentEvent.type == EventType.MouseDrag &&
                     currentEvent.button == 0 &&
                     isDraggingMap)
            {
                var delta = mousePosition - lastDragPosition;
                if ((mousePosition - dragStartPosition).sqrMagnitude > 36f)
                    mapDragMoved = true;
                PanMap(delta);
                lastDragPosition = mousePosition;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseUp &&
                     currentEvent.button == 0 &&
                     isDraggingMap)
            {
                if (!mapDragMoved && pressedNodeId != null)
                {
                    SelectedNodeId = pressedNodeId;
                    var now = UnityEngine.Time.unscaledTime;
                    if (lastClickedNodeId == pressedNodeId &&
                        now - lastNodeClickTime <= 0.4f)
                    {
                        CenterOnNode(pressedNodeId);
                        lastClickedNodeId = null;
                        lastNodeClickTime = -1f;
                    }
                    else
                    {
                        lastClickedNodeId = pressedNodeId;
                        lastNodeClickTime = now;
                    }
                }
                isDraggingMap = false;
                pressedNodeId = null;
                currentEvent.Use();
            }
            RefreshNodeHighlights();
        }

        private bool IsInsideMapViewport(Vector2 guiPosition)
        {
            ScreenLayout.GetRects(
                mapCamera,
                layoutDivider,
                out _,
                out var mainZone,
                out _);
            return mainZone.Contains(guiPosition);
        }

        private void PanMap(Vector2 guiDelta)
        {
            if (mapCamera == null) return;
            var worldHeight = mapCamera.orthographicSize * 2f;
            var worldWidth = worldHeight * mapCamera.aspect;
            mapOffset += new Vector2(
                guiDelta.x / Screen.width * worldWidth,
                -guiDelta.y / Screen.height * worldHeight);
            ClampMapOffset();
            targetMapOffset = mapOffset;
            mapPanVelocity = Vector2.zero;
            isCenteringMap = false;
        }

        private void CenterOnNode(string nodeId, bool immediate = false)
        {
            if (context == null || string.IsNullOrWhiteSpace(nodeId)) return;
            var node = context.World.GetNode(nodeId);
            targetMapOffset = ClampOffset(new Vector2(
                -(node.MapX - 0.5f) * mapWidth,
                -(node.MapY - 0.5f) * mapHeight));
            if (immediate)
            {
                mapOffset = targetMapOffset;
                mapPanVelocity = Vector2.zero;
                isCenteringMap = false;
            }
            else
            {
                isCenteringMap = true;
            }
        }

        private void ClampMapOffset()
        {
            mapOffset = ClampOffset(mapOffset);
        }

        private Vector2 ClampOffset(Vector2 offset)
        {
            if (!TryGetMapViewportBounds(out var lower, out var upper))
            {
                return Vector2.zero;
            }
            var maximumX = Mathf.Max(0f, (mapWidth - Mathf.Abs(upper.x - lower.x)) * 0.5f);
            var maximumY = Mathf.Max(0f, (mapHeight - Mathf.Abs(upper.y - lower.y)) * 0.5f);
            return new Vector2(
                Mathf.Clamp(offset.x, -maximumX, maximumX),
                Mathf.Clamp(offset.y, -maximumY, maximumY));
        }

        private void UpdateMapCenterAnimation()
        {
            mapOffset = ClampOffset(mapOffset);
            targetMapOffset = ClampOffset(targetMapOffset);
            if (!isCenteringMap) return;
            mapOffset = Vector2.SmoothDamp(
                mapOffset,
                targetMapOffset,
                ref mapPanVelocity,
                0.22f,
                Mathf.Infinity,
                UnityEngine.Time.unscaledDeltaTime);
            if ((mapOffset - targetMapOffset).sqrMagnitude <= 0.000001f)
            {
                mapOffset = targetMapOffset;
                mapPanVelocity = Vector2.zero;
                isCenteringMap = false;
            }
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
                anchor.SetParent(mapContentRoot, false);
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
                routeObject.transform.SetParent(mapContentRoot, false);
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
            entityObject.transform.SetParent(mapContentRoot, false);
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
            // Runtime SpriteMask interaction can intermittently render sliced
            // sprites as white silhouettes in Unity 2021. Viewport clipping for
            // entity sprites is handled explicitly in RefreshViews instead.
            renderer.maskInteraction = SpriteMaskInteraction.None;
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
            renderer.maskInteraction = SpriteMaskInteraction.None;
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
            var camera = mapCamera;
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
                pair.Value.enabled =
                    (selected || hovered) &&
                    IsInsideMapViewport(pair.Value.bounds.center);
                if (selected) pair.Value.color = new Color(1f, 0.82f, 0.15f, 0.85f);
                else if (hovered) pair.Value.color = new Color(0.45f, 0.45f, 0.45f, 0.8f);
            }
        }

        private void RefreshViews()
        {
            if (!TryGetMapWorldBounds(out _, out _)) return;
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
                var routePoints = new List<Vector3>
                {
                    MapToWorld(nodeA.MapX, nodeA.MapY, 0f)
                };
                for (var i = 0; i < edge.MapWaypoints.Count; i++)
                {
                    var waypoint = edge.MapWaypoints[i];
                    routePoints.Add(MapToWorld(waypoint.X, waypoint.Y, 0f));
                }
                routePoints.Add(MapToWorld(nodeB.MapX, nodeB.MapY, 0f));
                var clippedPoints = ClipPolylineToViewport(routePoints);
                line.enabled = clippedPoints.Count >= 2;
                if (line.enabled)
                {
                    line.positionCount = clippedPoints.Count;
                    line.SetPositions(clippedPoints.ToArray());
                }
            }

            foreach (var entity in context.Entities)
            {
                if (entity.HasBehavior<PlayerControlledBehavior>()) continue;
                var node = context.World.GetNode(entity.GetBehavior<WorldEntityBehavior>().StartingNodeId);
                var view = views[entity.Id];
                view.transform.position = MapToWorld(node.MapX, node.MapY);
                SetViewVisibleInsideViewport(view);
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
            SetViewVisibleInsideViewport(shipView);
        }

        private void SetViewVisibleInsideViewport(MapEntityView view)
        {
            var renderer = view.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
                renderer.enabled = IsInsideMapViewport(renderer.bounds.center);
        }

        private bool IsInsideMapViewport(Vector3 worldPosition)
        {
            if (!TryGetMapViewportBounds(out var lower, out var upper)) return false;
            return worldPosition.x >= Mathf.Min(lower.x, upper.x) &&
                   worldPosition.x <= Mathf.Max(lower.x, upper.x) &&
                   worldPosition.y >= Mathf.Min(lower.y, upper.y) &&
                   worldPosition.y <= Mathf.Max(lower.y, upper.y);
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
            if (!TryGetMapWorldBounds(out var lower, out var upper)) return Vector3.zero;
            return new Vector3(
                Mathf.Lerp(lower.x, upper.x, normalizedX),
                Mathf.Lerp(lower.y, upper.y, normalizedY),
                Mathf.Min(lower.z, upper.z) + depthOffset);
        }

        public bool TryGetMapWorldBounds(out Vector3 lower, out Vector3 upper)
        {
            lower = Vector3.zero;
            upper = Vector3.zero;
            if (!TryGetMapViewportBounds(out var viewportLower, out var viewportUpper))
                return false;
            var center = new Vector3(
                (viewportLower.x + viewportUpper.x) * 0.5f + mapOffset.x,
                (viewportLower.y + viewportUpper.y) * 0.5f + mapOffset.y,
                viewportLower.z);
            lower = new Vector3(
                center.x - mapWidth * 0.5f,
                center.y - mapHeight * 0.5f,
                center.z);
            upper = new Vector3(
                center.x + mapWidth * 0.5f,
                center.y + mapHeight * 0.5f,
                center.z);
            return true;
        }

        private bool TryGetMapViewportBounds(out Vector3 lower, out Vector3 upper)
        {
            return ScreenLayout.TryGetMainWorldBounds(
                mapCamera,
                layoutDivider,
                out lower,
                out upper);
        }

        private List<Vector3> ClipPolylineToViewport(IReadOnlyList<Vector3> points)
        {
            var result = new List<Vector3>();
            if (!TryGetMapViewportBounds(out var lower, out var upper)) return result;
            var rect = Rect.MinMaxRect(lower.x, lower.y, upper.x, upper.y);
            for (var i = 1; i < points.Count; i++)
            {
                if (!ClipSegment(rect, points[i - 1], points[i], out var start, out var end))
                    continue;
                if (result.Count == 0 || Vector3.Distance(result[result.Count - 1], start) > 0.001f)
                    result.Add(start);
                result.Add(end);
            }
            return result;
        }

        private static bool ClipSegment(
            Rect rect,
            Vector3 from,
            Vector3 to,
            out Vector3 clippedFrom,
            out Vector3 clippedTo)
        {
            var delta = to - from;
            var start = 0f;
            var end = 1f;
            if (!ClipBoundary(-delta.x, from.x - rect.xMin, ref start, ref end) ||
                !ClipBoundary(delta.x, rect.xMax - from.x, ref start, ref end) ||
                !ClipBoundary(-delta.y, from.y - rect.yMin, ref start, ref end) ||
                !ClipBoundary(delta.y, rect.yMax - from.y, ref start, ref end))
            {
                clippedFrom = Vector3.zero;
                clippedTo = Vector3.zero;
                return false;
            }
            clippedFrom = Vector3.Lerp(from, to, start);
            clippedTo = Vector3.Lerp(from, to, end);
            return true;
        }

        private static bool ClipBoundary(
            float direction,
            float distance,
            ref float start,
            ref float end)
        {
            if (Mathf.Approximately(direction, 0f)) return distance >= 0f;
            var ratio = distance / direction;
            if (direction < 0f)
            {
                if (ratio > end) return false;
                if (ratio > start) start = ratio;
            }
            else
            {
                if (ratio < start) return false;
                if (ratio < end) end = ratio;
            }
            return true;
        }

        private void OnDestroy()
        {
            if (routeMaterial != null) Destroy(routeMaterial);
            if (highlightSprite != null) Destroy(highlightSprite);
            if (highlightTexture != null) Destroy(highlightTexture);
            if (mapMaskSprite != null) Destroy(mapMaskSprite);
            if (mapMaskTexture != null) Destroy(mapMaskTexture);
        }
    }
}
