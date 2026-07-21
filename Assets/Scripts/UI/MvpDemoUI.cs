using System;
using UnityEngine;
using TalesOfVoyages.Simulation.Core;
using TalesOfVoyages.Simulation.Time;
using TalesOfVoyages.Simulation.World;
using TalesOfVoyages.Graphics;
using TalesOfVoyages.Simulation.Entities;

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
        private Camera mapCamera;
        private ExternalGraphicsCatalog graphics;
        private Sprite portWindowFrame;
        private GUIStyle encounterTitleStyle;
        private Material circularImageMaterial;
        private const float PortPortraitInsetScale = 0.9f;

        public void Initialize(
            GameContext gameContext,
            Transform bottomLeft,
            Transform topRight,
            ExternalGraphicsCatalog graphicsCatalog,
            Sprite windowFrame,
            Material portraitMaterial)
        {
            context = gameContext ?? throw new ArgumentNullException(nameof(gameContext));
            mapBottomLeft = bottomLeft;
            mapTopRight = topRight;
            graphics = graphicsCatalog ?? throw new ArgumentNullException(nameof(graphicsCatalog));
            portWindowFrame = windowFrame;
            circularImageMaterial = portraitMaterial;
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
        }

        private void OnGUI()
        {
            if (context == null) return;
            EnsureStyles();
            var margin = 20f;
            var controls = new Rect(margin, margin, 320f, Screen.height - margin * 2f);
            var map = GetMapRect(controls, margin);
            GUI.Box(controls, GUIContent.none);
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
            DrawMap(map);
            DrawEnteringPortOverlay(map);
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
                GUILayout.Label($"In port: {context.GetPortAtNode(location.Id).DisplayName}");
                if (ship.Travel.HasPlannedAction)
                {
                    var plannedPort = context.GetPortAtNode(ship.Travel.PlannedDestinationNodeId);
                    GUILayout.Label($"Planned for tomorrow: sail to {plannedPort.DisplayName}");
                    if (GUILayout.Button("Cancel planned voyage")) context.Movement.CancelPlannedDestination(ship.Id);
                }
                else
                {
                    GUILayout.Label("Available destinations:");
                    foreach (var destination in context.World.GetNeighbors(location.Id))
                        if (GUILayout.Button($"Plan voyage to {context.GetPortAtNode(destination.Id).DisplayName}"))
                            context.Movement.PlanDestination(ship.Id, destination.Id);
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
            if (mapCamera == null) mapCamera = Camera.main;
            if (mapBottomLeft == null || mapTopRight == null || mapCamera == null)
                return new Rect(controls.xMax + margin, margin, Math.Max(300f, Screen.width - controls.width - margin * 3f), Screen.height - margin * 2f);

            var a = mapCamera.WorldToScreenPoint(mapBottomLeft.position);
            var b = mapCamera.WorldToScreenPoint(mapTopRight.position);
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

    }
}
