using System;
using UnityEngine;
using TalesOfVoyages.Simulation.Core;
using TalesOfVoyages.Simulation.Time;
using TalesOfVoyages.Simulation.World;

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

        public void Initialize(GameContext gameContext, Transform bottomLeft, Transform topRight)
        {
            context = gameContext ?? throw new ArgumentNullException(nameof(gameContext));
            mapBottomLeft = bottomLeft;
            mapTopRight = topRight;
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
            SpeedButton("1×", TimeSpeed.Normal);
            SpeedButton("2×", TimeSpeed.Fast);
            SpeedButton("4×", TimeSpeed.VeryFast);
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
                GUILayout.Label($"In port: {location.DisplayName}");
                if (ship.Travel.HasPlannedAction)
                {
                    var plannedPort = context.World.GetNode(ship.Travel.PlannedDestinationNodeId);
                    GUILayout.Label($"Planned for tomorrow: sail to {plannedPort.DisplayName}");
                    if (GUILayout.Button("Cancel planned voyage")) context.Movement.CancelPlannedDestination(ship.Id);
                }
                else
                {
                    GUILayout.Label("Available destinations:");
                    foreach (var destination in context.World.GetNeighbors(location.Id))
                        if (GUILayout.Button($"Plan voyage to {destination.DisplayName}"))
                            context.Movement.PlanDestination(ship.Id, destination.Id);
                }
            }
            else
            {
                var visualProgress = ship.Travel.GetVisualEdgeProgress(context.Time.DayProgress);
                if (ship.Travel.IsEnteringPort(context.Time.DayProgress))
                    GUILayout.Label($"Entering port {context.World.GetNode(ship.Travel.DestinationNodeId).DisplayName}");
                else
                    GUILayout.Label($"At sea — {visualProgress:P0} of current leg");
                var progressRect = GUILayoutUtility.GetRect(100f, 18f);
                GUI.Box(progressRect, GUIContent.none);
                GUI.Box(new Rect(progressRect.x + 2f, progressRect.y + 2f, (progressRect.width - 4f) * visualProgress, progressRect.height - 4f), GUIContent.none);
            }
        }

        private void DrawMap(Rect rect)
        {
            foreach (var node in context.World.Nodes)
                if (node.Type == WorldNodeType.Port)
                    DrawPortLabel(MapToGui(rect, node.MapX, node.MapY), node.DisplayName);
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

    }
}
