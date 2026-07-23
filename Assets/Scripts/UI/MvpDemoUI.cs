using System;
using UnityEngine;
using TalesOfVoyages.Simulation.Core;
using TalesOfVoyages.Simulation.Time;
using TalesOfVoyages.Simulation.World;
using TalesOfVoyages.Graphics;
using TalesOfVoyages.Simulation.Entities;
using System.Linq;

namespace TalesOfVoyages.Unity.UI
{
    public sealed class MvpDemoUI : MonoBehaviour
    {
        private GameContext context;
        private GUIStyle titleStyle;
        private GUIStyle centeredStyle;
        private GUIStyle logStyle;
        private Transform mapBottomLeft;
        private Transform mapTopRight;
        private Transform leftMenuBottomLeft;
        private Transform leftMenuTopRight;
        private Transform bottomMenuBottomLeft;
        private Transform bottomMenuTopRight;
        private Camera mapCamera;
        private ExternalGraphicsCatalog graphics;
        private Sprite portWindowFrame;
        private GUIStyle encounterTitleStyle;
        private Material circularImageMaterial;
        private MapEntitySceneController mapController;
        private GUIStyle menuPanelStyle;
        private GUIStyle mapPanelStyle;
        private Texture2D menuPanelTexture;
        private Texture2D mapPanelTexture;
        private const float PortPortraitInsetScale = 1.0f;

        public void Initialize(
            GameContext gameContext,
            Transform mapBoundsBottomLeft,
            Transform mapBoundsTopRight,
            Transform leftMenuBoundsBottomLeft,
            Transform leftMenuBoundsTopRight,
            Transform bottomMenuBoundsBottomLeft,
            Transform bottomMenuBoundsTopRight,
            ExternalGraphicsCatalog graphicsCatalog,
            Sprite windowFrame,
            Material portraitMaterial,
            MapEntitySceneController entitySceneController)
        {
            context = gameContext ?? throw new ArgumentNullException(nameof(gameContext));
            mapBottomLeft = mapBoundsBottomLeft;
            mapTopRight = mapBoundsTopRight;
            leftMenuBottomLeft = leftMenuBoundsBottomLeft;
            leftMenuTopRight = leftMenuBoundsTopRight;
            bottomMenuBottomLeft = bottomMenuBoundsBottomLeft;
            bottomMenuTopRight = bottomMenuBoundsTopRight;
            graphics = graphicsCatalog ?? throw new ArgumentNullException(nameof(graphicsCatalog));
            portWindowFrame = windowFrame;
            circularImageMaterial = portraitMaterial;
            mapController = entitySceneController;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };

            centeredStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            centeredStyle.normal.textColor = new Color(0.43f, 0.15f, 0.10f, 1f);
            centeredStyle.normal.background = Texture2D.whiteTexture;
            centeredStyle.hover.textColor = new Color(0.0f, 0.0f, 0.0f, 1f);
            centeredStyle.hover.background = Texture2D.whiteTexture;

            logStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, alignment = TextAnchor.UpperLeft };
            encounterTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            menuPanelTexture = CreateRoundedPanelTexture(
                "Rounded Menu Panel",
                new Color(0.055f, 0.07f, 0.1f, 0.88f),
                new Color(0.015f, 0.015f, 0.015f, 0.95f));
            mapPanelTexture = CreateRoundedPanelTexture(
                "Rounded Map Border",
                Color.clear,
                new Color(0.015f, 0.015f, 0.015f, 0.95f));
            menuPanelStyle = CreateRoundedPanelStyle(menuPanelTexture);
            mapPanelStyle = CreateRoundedPanelStyle(mapPanelTexture);
        }

        private void OnGUI()
        {
            if (context == null) return;
            EnsureStyles();
            var margin = 20f;
            var controls = GetScreenRect(
                leftMenuBottomLeft,
                leftMenuTopRight,
                new Rect(margin, margin, 320f, Screen.height - margin * 2f));
            var map = GetMapRect(controls, margin);
            var bottomMenu = GetScreenRect(
                bottomMenuBottomLeft,
                bottomMenuTopRight,
                new Rect(map.x, Mathf.Max(map.y, Screen.height - 160f), map.width, 160f));
            GUI.Box(bottomMenu, GUIContent.none, menuPanelStyle);
            DrawBottomPanel(bottomMenu);
            GUI.Box(controls, GUIContent.none, menuPanelStyle);
            GUILayout.BeginArea(new Rect(controls.x + 16f, controls.y + 12f, controls.width - 32f, controls.height - 24f));
            GUILayout.Label("Tales of the Brave", titleStyle);
            GUILayout.Label($"{context.Time.CurrentDate}  {context.Time.GetFormattedTime()}");
            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            SpeedButton("Pause", TimeSpeed.Paused);
            foreach (var speed in context.Time.AllowedSpeeds)
                SpeedButton($"{(int)speed}×", speed);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Developer: advance one day")) context.Time.AdvanceDay();
            GUILayout.Space(14f);
            DrawShipControls();
            GUILayout.Space(14f);
            GUILayout.Label("Captain's Log", titleStyle);
            for (var i = context.Chronicler.Entries.Count - 1; i >= 0; i--)
            {
                var entry = context.Chronicler.Entries[i];
                GUILayout.Label($"{entry.Date}\n{entry.Text}", logStyle);
                GUILayout.Space(4f);
            }
            GUILayout.EndArea();
            GUI.Box(map, GUIContent.none, mapPanelStyle);
            DrawMap(map);
            DrawEnteringPortOverlay(map);
        }

        private static GUIStyle CreateRoundedPanelStyle(Texture2D texture)
        {
            var style = new GUIStyle { normal = { background = texture } };
            style.border = new RectOffset(10, 10, 10, 10);
            return style;
        }

        private static Texture2D CreateRoundedPanelTexture(string textureName, Color fill, Color border)
        {
            const int size = 32;
            const int radius = 9;
            const int borderWidth = 2;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = textureName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var outer = IsInsideRoundedRect(x + 0.5f, y + 0.5f, size, radius);
                var inner = IsInsideRoundedRect(
                    x + 0.5f - borderWidth,
                    y + 0.5f - borderWidth,
                    size - borderWidth * 2,
                    radius - borderWidth);
                texture.SetPixel(x, y, !outer ? Color.clear : inner ? fill : border);
            }
            texture.Apply();
            return texture;
        }

        private static bool IsInsideRoundedRect(float x, float y, int size, float radius)
        {
            if (x < 0f || y < 0f || x > size || y > size) return false;
            var nearestX = Mathf.Clamp(x, radius, size - radius);
            var nearestY = Mathf.Clamp(y, radius, size - radius);
            var deltaX = x - nearestX;
            var deltaY = y - nearestY;
            return deltaX * deltaX + deltaY * deltaY <= radius * radius;
        }

        private void DrawBottomPanel(Rect panel)
        {
            const float padding = 16f;
            const float columnGap = 20f;
            var content = new Rect(
                panel.x + padding,
                panel.y + padding,
                Mathf.Max(0f, panel.width - padding * 2f),
                Mathf.Max(0f, panel.height - padding * 2f));
            var columnWidth = Mathf.Max(0f, (content.width - columnGap) * 0.5f);

            GUILayout.BeginArea(new Rect(content.x, content.y, columnWidth, content.height));
            var ship = context.PlayerShip;
            GUILayout.Label(ship.DisplayName, titleStyle);
            var travel = ship.Travel;
            var locationText = travel.IsTravelling
                ? $"Travelling to {context.World.GetNode(travel.GetNextNodeId(context.Time.DayProgress)).DisplayName}"
                : $"At {context.World.GetNode(travel.CurrentNodeId).DisplayName}";
            GUILayout.Label(locationText);
            GUILayout.Label($"Speed per day: {ship.SpeedPerDay:0.##}");
            var estimatedDays = GetEstimatedTravelDays(ship);
            if (estimatedDays.HasValue)
                GUILayout.Label($"Estimated duration: {estimatedDays.Value} {(estimatedDays.Value == 1 ? "day" : "days")}");
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(
                content.x + columnWidth + columnGap,
                content.y,
                columnWidth,
                content.height));
            if (mapController != null && !string.IsNullOrWhiteSpace(mapController.SelectedNodeId))
            {
                var selectedNode = context.World.GetNode(mapController.SelectedNodeId);
                GUILayout.Label(selectedNode.DisplayName, titleStyle);
                var isPort = context.Entities.Any(entity =>
                    entity.HasBehavior<PortBehavior>() &&
                    entity.HasBehavior<WorldEntityBehavior>() &&
                    entity.GetBehavior<WorldEntityBehavior>().StartingNodeId == selectedNode.Id);
                if (isPort) GUILayout.Label("Port");
            }
            GUILayout.EndArea();
        }

        private int? GetEstimatedTravelDays(TalesOfVoyages.Simulation.Movement.Transport ship)
        {
            var travel = ship.Travel;
            float distance;
            if (travel.IsTravelling)
            {
                distance = GetRemainingTravelDistance(travel);
            }
            else
            {
                var destinationNodeId = travel.HasPlannedAction
                    ? travel.PlannedDestinationNodeId
                    : mapController?.SelectedNodeId;
                if (string.IsNullOrWhiteSpace(destinationNodeId) ||
                    destinationNodeId == travel.CurrentNodeId)
                    return null;
                distance = context.World.GetRouteDistance(
                    context.World.FindRoute(travel.CurrentNodeId, destinationNodeId));
            }
            return Mathf.CeilToInt(distance / ship.SpeedPerDay);
        }

        private float GetRemainingTravelDistance(
            TalesOfVoyages.Simulation.Movement.TravelState travel)
        {
            var visualSegment = travel.GetVisualSegment(context.Time.DayProgress);
            var currentEdgeId = visualSegment?.EdgeId ?? travel.CurrentEdgeId;
            if (currentEdgeId == null) return 0f;

            var currentProgress = travel.GetVisualEdgeProgress(context.Time.DayProgress);
            var distance = context.World.GetEdge(currentEdgeId).Distance * (1f - currentProgress);
            var nextNodeId = visualSegment?.ToNodeId ?? travel.NextNodeId;
            var nextNodeIndex = travel.RemainingRoute.IndexOf(nextNodeId);
            if (nextNodeIndex < 0) return distance;

            for (var i = nextNodeIndex + 1; i < travel.RemainingRoute.Count; i++)
            {
                var edge = context.World.GetConnectingEdge(
                    travel.RemainingRoute[i - 1],
                    travel.RemainingRoute[i]);
                if (edge != null) distance += edge.Distance;
            }
            return distance;
        }

        private void SpeedButton(string label, TimeSpeed speed)
        {
            var previous = GUI.backgroundColor;
            if (context.Time.Speed == speed) GUI.backgroundColor = new Color(0.65f, 0.9f, 0.65f);
            if (GUILayout.Button(label)) context.Time.SetSpeed(speed);
            GUI.backgroundColor = previous;
        }

        private void DrawShipControls()
        {
            var ship = context.PlayerShip;
            GUILayout.Label(ship.DisplayName, titleStyle);
            if (!ship.Travel.IsTravelling)
            {
                var location = context.World.GetNode(ship.Travel.CurrentNodeId);
                var locationPort = context.Entities.SingleOrDefault(entity =>
                    entity.HasBehavior<PortBehavior>() &&
                    entity.HasBehavior<WorldEntityBehavior>() &&
                    entity.GetBehavior<WorldEntityBehavior>().StartingNodeId == location.Id);
                GUILayout.Label(locationPort == null
                    ? $"At {location.DisplayName}"
                    : $"In port: {locationPort.DisplayName}");
                if (ship.Travel.HasPlannedAction)
                {
                    var plannedNode = context.World.GetNode(ship.Travel.PlannedDestinationNodeId);
                    GUILayout.Label($"Planned for tomorrow: sail to {plannedNode.DisplayName}");
                    if (GUILayout.Button("Cancel planned voyage")) context.Movement.CancelPlannedDestination(ship.Id);
                    if (GUILayout.Button("Forward to departure")) context.Time.SkipToNextDayStart();
                }
                else
                {
                    var selectedNodeId = mapController == null ? null : mapController.SelectedNodeId;
                    if (string.IsNullOrWhiteSpace(selectedNodeId))
                    {
                        GUILayout.Label("Select a map node to plan a voyage.");
                    }
                    else if (selectedNodeId == location.Id)
                    {
                        GUILayout.Label("The ship is already at the selected node.");
                    }
                    else
                    {
                        var selectedNode = context.World.GetNode(selectedNodeId);
                        if (GUILayout.Button($"Set sail to {selectedNode.DisplayName}"))
                            context.Movement.PlanDestination(ship.Id, selectedNode.Id);
                    }
                }
            }
            else
            {
                var visualProgress = ship.Travel.GetVisualEdgeProgress(context.Time.DayProgress);
                var interaction = context.GetPendingInteractionEntity();
                if (interaction != null)
                {
                    GUILayout.Label($"Entering port {interaction.DisplayName}");
                    DrawInteractionLayoutActions(interaction);
                }
                else
                {
                    GUILayout.Label($"At sea — {visualProgress:P0} of current leg");
                    var progressRect = GUILayoutUtility.GetRect(100f, 18f);
                    GUI.Box(progressRect, GUIContent.none);
                    GUI.Box(new Rect(
                        progressRect.x + 2f,
                        progressRect.y + 2f,
                        (progressRect.width - 4f) * visualProgress,
                        progressRect.height - 4f), GUIContent.none);
                }
            }
        }

        private void DrawInteractionLayoutActions(Entity entity)
        {
            foreach (var action in entity.Actions)
                if (GUILayout.Button(action.Label)) action.Execute(context);
        }

        private void DrawMap(Rect rect)
        {
            foreach (var entity in context.Entities)
                if (entity.HasBehavior<TalesOfVoyages.Simulation.Entities.PortBehavior>())
                {
                    var node = context.World.GetNode(
                        entity.GetBehavior<TalesOfVoyages.Simulation.Entities.WorldEntityBehavior>().StartingNodeId);
                    DrawPortLabel(MapToGui(rect, node.MapX, node.MapY), entity.DisplayName);
                }
        }

        private Rect GetMapRect(Rect controls, float margin)
        {
            return GetScreenRect(
                mapBottomLeft,
                mapTopRight,
                new Rect(
                    controls.xMax + margin,
                    margin,
                    Math.Max(300f, Screen.width - controls.width - margin * 3f),
                    Screen.height - margin * 2f));
        }

        private Rect GetScreenRect(Transform bottomLeft, Transform topRight, Rect fallback)
        {
            if (mapCamera == null) mapCamera = Camera.main;
            if (bottomLeft == null || topRight == null || mapCamera == null) return fallback;

            var a = mapCamera.WorldToScreenPoint(bottomLeft.position);
            var b = mapCamera.WorldToScreenPoint(topRight.position);
            var left = Mathf.Min(a.x, b.x);
            var right = Mathf.Max(a.x, b.x);
            var bottom = Mathf.Min(a.y, b.y);
            var top = Mathf.Max(a.y, b.y);
            return new Rect(left, Screen.height - top, right - left, top - bottom);
        }

        private static Vector2 MapToGui(Rect rect, float normalizedX, float normalizedY)
        {
            return new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalizedX),
                Mathf.Lerp(rect.yMax, rect.yMin, normalizedY));
        }

        private void DrawPortLabel(Vector2 point, string label) =>
            GUI.Label(new Rect(point.x - 70f, point.y + 34f, 140f, 24f), label, centeredStyle);

        private void DrawEnteringPortOverlay(Rect mapRect)
        {
            var port = context.GetPendingInteractionEntity();
            if (port == null || portWindowFrame == null) return;

            var size = Mathf.Min(380f, Mathf.Min(mapRect.width * 0.55f, mapRect.height * 0.62f));
            var frameRect = new Rect(
                mapRect.center.x - size * 0.5f,
                mapRect.center.y - size * 0.58f,
                size,
                size);
            var portraitRect = new Rect(
                frameRect.x + size * 0.16f,
                frameRect.y + size * 0.16f,
                size * 0.68f,
                size * 0.68f);
            portraitRect = ScaleRectAroundCenter(portraitRect, PortPortraitInsetScale);
            var portSprite = graphics.GetSprite(port.GetBehavior<PortBehavior>().PortViewSprite);

            if (Event.current.type == EventType.Repaint)
                DrawCircularTexture(portraitRect, portSprite.texture);
            GUI.DrawTexture(frameRect, portWindowFrame.texture, ScaleMode.ScaleToFit, true);

            var labelRect = new Rect(frameRect.x, frameRect.yMax + 2f, size, 38f);
            GUI.Label(labelRect, port.DisplayName, encounterTitleStyle);
            var buttonRect = new Rect(frameRect.center.x - 90f, labelRect.yMax + 6f, 180f, 32f);
            foreach (var action in port.Actions)
            {
                if (GUI.Button(buttonRect, action.Label)) action.Execute(context);
                buttonRect.y += 38f;
            }
        }

        private void DrawCircularTexture(Rect rect, Texture texture)
        {
            if (circularImageMaterial == null) return;

            circularImageMaterial.mainTexture = texture;
            circularImageMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0f, Screen.width, Screen.height, 0f);
            GL.Begin(GL.TRIANGLES);
            GL.Color(Color.white);
            const int segments = 64;
            for (var i = 0; i < segments; i++)
            {
                var angleA = i * Mathf.PI * 2f / segments;
                var angleB = (i + 1) * Mathf.PI * 2f / segments;
                GL.TexCoord2(0.5f, 0.5f);
                GL.Vertex3(rect.center.x, rect.center.y, 0f);
                AddCircularVertex(rect, angleA);
                AddCircularVertex(rect, angleB);
            }
            GL.End();
            GL.PopMatrix();
        }

        private static void AddCircularVertex(Rect rect, float angle)
        {
            var x = Mathf.Cos(angle);
            var y = Mathf.Sin(angle);
            // GUI/GL screen Y grows downward, while texture V grows upward.
            GL.TexCoord2(x * 0.5f + 0.5f, 0.5f - y * 0.5f);
            GL.Vertex3(
                rect.center.x + x * rect.width * 0.5f,
                rect.center.y + y * rect.height * 0.5f,
                0f);
        }

        private static Rect ScaleRectAroundCenter(Rect rect, float scale)
        {
            var width = rect.width * scale;
            var height = rect.height * scale;
            return new Rect(
                rect.center.x - width * 0.5f,
                rect.center.y - height * 0.5f,
                width,
                height);
        }

        private void OnDestroy()
        {
            if (menuPanelTexture != null) Destroy(menuPanelTexture);
            if (mapPanelTexture != null) Destroy(mapPanelTexture);
        }

    }
}
