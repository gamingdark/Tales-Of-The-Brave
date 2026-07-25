using System;
using System.Collections.Generic;
using UnityEngine;
using TalesOfTheBrave.Simulation.Core;
using TalesOfTheBrave.Simulation.Time;
using TalesOfTheBrave.Simulation.World;
using TalesOfTheBrave.Graphics;
using TalesOfTheBrave.Simulation.Entities;
using TalesOfTheBrave.Simulation.Rulesets;
using System.Linq;

namespace TalesOfTheBrave.Unity.UI
{
    public static class ScreenLayout
    {
        public const float Padding = 10f;

        public static void GetRects(
            Camera camera,
            Transform divider,
            out Rect leftMenu,
            out Rect mainZone,
            out Rect bottomMenu)
        {
            var splitX = Mathf.Clamp(Screen.width * 0.35f, 240f, Screen.width - 320f);
            var splitY = Mathf.Clamp(Screen.height * 0.82f, 240f, Screen.height - 120f);
            if (divider != null && camera != null)
            {
                var screenPoint = camera.WorldToScreenPoint(divider.position);
                splitX = Mathf.Clamp(screenPoint.x, 240f, Screen.width - 320f);
                splitY = Mathf.Clamp(Screen.height - screenPoint.y, 240f, Screen.height - 120f);
            }

            leftMenu = new Rect(
                Padding,
                Padding,
                Mathf.Max(1f, splitX - Padding * 2f),
                Mathf.Max(1f, Screen.height - Padding * 2f));
            mainZone = new Rect(
                splitX + Padding,
                Padding,
                Mathf.Max(1f, Screen.width - splitX - Padding * 2f),
                Mathf.Max(1f, splitY - Padding * 2f));
            bottomMenu = new Rect(
                splitX + Padding,
                splitY + Padding,
                Mathf.Max(1f, Screen.width - splitX - Padding * 2f),
                Mathf.Max(1f, Screen.height - splitY - Padding * 2f));
        }

        public static bool TryGetMainWorldBounds(
            Camera camera,
            Transform divider,
            out Vector3 lower,
            out Vector3 upper)
        {
            lower = Vector3.zero;
            upper = Vector3.zero;
            if (camera == null || divider == null) return false;

            GetRects(camera, divider, out _, out var mainZone, out _);
            var distanceFromCamera = Mathf.Abs(divider.position.z - camera.transform.position.z);
            lower = camera.ScreenToWorldPoint(new Vector3(
                mainZone.xMin,
                Screen.height - mainZone.yMax,
                distanceFromCamera));
            upper = camera.ScreenToWorldPoint(new Vector3(
                mainZone.xMax,
                Screen.height - mainZone.yMin,
                distanceFromCamera));
            lower.z = divider.position.z;
            upper.z = divider.position.z;
            return true;
        }
    }

    public sealed class GameplayUI : MonoBehaviour
    {
        private GameContext context;
        private GUIStyle titleStyle;
        private GUIStyle logStyle;
        private Transform layoutDivider;
        private Camera mapCamera;
        private ExternalGraphicsCatalog graphics;
        private Sprite locationWindowFrame;
        private GUIStyle encounterTitleStyle;
        private GUIStyle locationTitleStyle;
        private Material circularImageMaterial;
        private MapEntitySceneController mapController;
        private GUIStyle menuPanelStyle;
        private GUIStyle mapPanelStyle;
        private GUIStyle tooltipStyle;
        private GUIStyle locationRowStyle;
        private GUIStyle iconFrameStyle;
        private GUIStyle portraitFrameStyle;
        private Texture2D menuPanelTexture;
        private Texture2D mapPanelTexture;
        private Texture2D tooltipTexture;
        private Texture2D iconFrameTexture;
        private Texture2D portraitFrameTexture;
        private Texture2D nightTintTexture;
        private UiSystemDefinition uiDefinition;
        private TimeSystemDefinition timeDefinition;
        private GUIStyle nightTintStyle;
        private Vector2 chroniclerScroll;
        private Vector2 marketScroll;
        private ILocationAction selectedLocationAction;
        private MarketTradeSelection marketTradeSelection;
        private int tradeQuantity = 10;
        private string openLocationEntityId;
        private float suppressTooltipUntil;
        private bool touchInputDetected;
        private const float PanelContentPadding = 16f;
        private const float LocationPortraitInsetScale = 1.0f;

        public void Initialize(
            GameContext gameContext,
            Camera camera,
            Transform divider,
            ExternalGraphicsCatalog graphicsCatalog,
            Sprite windowFrame,
            Material portraitMaterial,
            UiSystemDefinition uiSystemDefinition,
            TimeSystemDefinition timeSystemDefinition,
            MapEntitySceneController entitySceneController)
        {
            context = gameContext ?? throw new ArgumentNullException(nameof(gameContext));
            mapCamera = camera;
            layoutDivider = divider;
            graphics = graphicsCatalog ?? throw new ArgumentNullException(nameof(graphicsCatalog));
            locationWindowFrame = windowFrame;
            circularImageMaterial = portraitMaterial;
            uiDefinition = uiSystemDefinition ?? throw new ArgumentNullException(nameof(uiSystemDefinition));
            timeDefinition = timeSystemDefinition ?? throw new ArgumentNullException(nameof(timeSystemDefinition));
            mapController = entitySceneController;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };

            logStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, alignment = TextAnchor.UpperLeft };
            encounterTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            locationTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            menuPanelTexture = CreateRoundedPanelTexture(
                "Rounded Menu Panel",
                ToColor(uiDefinition.Menus.Background),
                ToColor(uiDefinition.Menus.Border),
                uiDefinition.Menus.BorderWidth);
            mapPanelTexture = CreateRoundedPanelTexture(
                "Rounded Map Border",
                Color.clear,
                ToColor(uiDefinition.Menus.Border),
                uiDefinition.Menus.BorderWidth);
            tooltipTexture = CreateRoundedPanelTexture(
                "Rounded Tooltip",
                ToColor(uiDefinition.Tooltips.Background),
                ToColor(uiDefinition.Tooltips.Border),
                uiDefinition.Tooltips.BorderWidth);
            menuPanelStyle = CreateRoundedPanelStyle(menuPanelTexture);
            mapPanelStyle = CreateRoundedPanelStyle(mapPanelTexture);
            tooltipStyle = CreateRoundedPanelStyle(tooltipTexture);
            tooltipStyle.padding = new RectOffset(10, 10, 7, 7);
            tooltipStyle.wordWrap = true;
            tooltipStyle.richText = true;
            tooltipStyle.normal.textColor = ToColor(uiDefinition.Tooltips.Font);
            locationRowStyle = new GUIStyle(tooltipStyle)
            {
                padding = new RectOffset(8, 8, 6, 6)
            };
            locationRowStyle.hover.background = tooltipTexture;
            locationRowStyle.active.background = tooltipTexture;
            iconFrameTexture = CreateRoundedPanelTexture(
                "Round Location Icon",
                ToColor(uiDefinition.Tooltips.Background),
                ToColor(uiDefinition.Tooltips.Border),
                uiDefinition.Tooltips.BorderWidth,
                15);
            portraitFrameTexture = CreateRoundedPanelTexture(
                "Rounded Location Portrait",
                Color.clear,
                ToColor(uiDefinition.Tooltips.Border),
                uiDefinition.Tooltips.BorderWidth);
            iconFrameStyle = CreateRoundedPanelStyle(iconFrameTexture);
            portraitFrameStyle = CreateRoundedPanelStyle(portraitFrameTexture);
            nightTintTexture = CreateRoundedPanelTexture(
                "Rounded Map Night Tint",
                Color.white,
                Color.white,
                0f);
            nightTintStyle = CreateRoundedPanelStyle(nightTintTexture);
        }

        private void OnGUI()
        {
            if (context == null) return;
            if (Input.touchCount > 0)
            {
                touchInputDetected = true;
                suppressTooltipUntil = UnityEngine.Time.unscaledTime + 0.5f;
            }
            EnsureStyles();
            var insideLocation = context.PlayerShip.Travel.IsInsideLocation;
            mapController?.SetMapVisible(!insideLocation);
            GetLayoutRects(out var leftMenu, out var mainZone, out var bottomMenu);
            var previousContentColor = GUI.contentColor;
            GUI.contentColor = ToColor(uiDefinition.Menus.Font);
            DrawLeftMenu(leftMenu);
            GUI.contentColor = previousContentColor;
            if (insideLocation) GUI.contentColor = ToColor(uiDefinition.Menus.Font);
            DrawMainZone(mainZone);
            GUI.contentColor = previousContentColor;
            GUI.contentColor = ToColor(uiDefinition.Menus.Font);
            DrawBottomPanel(bottomMenu);
            GUI.contentColor = previousContentColor;
            DrawTooltip();
        }

        private void DrawLeftMenu(Rect panel)
        {
            GUI.Box(panel, GUIContent.none, menuPanelStyle);
            var content = Inset(panel, PanelContentPadding);
            GUILayout.BeginArea(content);
            GUILayout.BeginVertical();
            DrawTimeControls();
            GUILayout.Space(14f);
            DrawActionControls();
            GUILayout.Space(14f);
            DrawChronicler();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawTimeControls()
        {
            GUILayout.Label("Tales of the Brave", titleStyle);
            GUILayout.Label($"{context.Time.CurrentDate}  {context.Time.GetFormattedTime()}");
            GUILayout.Space(6f);
            var previousEnabled = GUI.enabled;
            if (context.PlayerShip.Travel.IsInsideLocation) GUI.enabled = false;
            GUILayout.BeginHorizontal();
            SpeedButton("Pause", TimeSpeed.Paused);
            foreach (var speed in context.Time.AllowedSpeeds)
                SpeedButton($"{(int)speed}×", speed);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Developer: advance one day")) context.Time.AdvanceDay();
            GUI.enabled = previousEnabled;
        }

        private void DrawActionControls()
        {
            DrawShipControls();
        }

        private void DrawChronicler()
        {
            GUILayout.Label("Captain's Log", titleStyle);
            chroniclerScroll = GUILayout.BeginScrollView(chroniclerScroll, GUILayout.ExpandHeight(true));
            for (var i = context.Chronicler.Entries.Count - 1; i >= 0; i--)
            {
                var entry = context.Chronicler.Entries[i];
                GUILayout.Label($"{entry.Date}\n{entry.Text}", logStyle);
                GUILayout.Space(4f);
            }
            GUILayout.EndScrollView();
        }

        private void DrawMainZone(Rect panel)
        {
            if (context.PlayerShip.Travel.IsInsideLocation)
            {
                GUI.Box(panel, GUIContent.none, menuPanelStyle);
                DrawLocationPanel(panel);
                return;
            }

            GUI.Box(panel, GUIContent.none, mapPanelStyle);
            // Night tint rendering is retained for possible future reuse, but
            // intentionally disabled because it distracts from map interaction.
            DrawEnteringLocationOverlay(panel);
        }

        private void DrawMapNightTint(Rect panel)
        {
            var strength = CalculateNightTintStrength(
                context.Time.CurrentDisplayedHour,
                context.Time.HoursPerDay,
                timeDefinition.MidnightHour,
                timeDefinition.NightDarkeningDurationHours,
                timeDefinition.NightBrighteningDurationHours);
            if (strength <= 0f) return;

            var previousColor = GUI.color;
            var tint = timeDefinition.NightTint;
            tint.a *= strength;
            GUI.color = tint;
            GUI.Box(
                Inset(panel, Mathf.Max(1f, uiDefinition.Menus.BorderWidth)),
                GUIContent.none,
                nightTintStyle);
            GUI.color = previousColor;
        }

        public static float CalculateNightTintStrength(
            float currentHour,
            float hoursPerDay,
            float midnightHour,
            float darkeningDurationHours,
            float brighteningDurationHours)
        {
            if (hoursPerDay <= 0f) return 0f;
            var hour = Mathf.Repeat(currentHour, hoursPerDay);
            var midnight = Mathf.Repeat(midnightHour, hoursPerDay);
            var untilMidnight = Mathf.Repeat(midnight - hour, hoursPerDay);
            var afterMidnight = Mathf.Repeat(hour - midnight, hoursPerDay);
            var darkening = untilMidnight <= darkeningDurationHours
                ? 1f - untilMidnight / darkeningDurationHours
                : 0f;
            var brightening = afterMidnight <= brighteningDurationHours
                ? 1f - afterMidnight / brighteningDurationHours
                : 0f;
            return Mathf.Clamp01(Mathf.Max(darkening, brightening));
        }

        private static GUIStyle CreateRoundedPanelStyle(Texture2D texture)
        {
            var style = new GUIStyle { normal = { background = texture } };
            style.border = new RectOffset(10, 10, 10, 10);
            return style;
        }

        private static Texture2D CreateRoundedPanelTexture(
            string textureName,
            Color fill,
            Color border,
            float configuredBorderWidth,
            int radius = 9)
        {
            const int size = 32;
            var borderWidth = Mathf.Clamp(configuredBorderWidth, 0f, radius);
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

        private static bool IsInsideRoundedRect(float x, float y, float size, float radius)
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
            GUI.Box(panel, GUIContent.none, menuPanelStyle);
            var content = Inset(panel, PanelContentPadding);
            const int elementCount = 2;
            var elementWidth = content.width / elementCount;
            DrawShipInfo(new Rect(content.x, content.y, elementWidth, content.height));
            DrawNodeInfo(new Rect(content.x + elementWidth, content.y, elementWidth, content.height));
        }

        private void DrawShipInfo(Rect area)
        {
            GUILayout.BeginArea(Inset(area, ScreenLayout.Padding));
            var ship = context.PlayerShip;
            GUILayout.Label(ship.DisplayName, titleStyle);
            var travel = ship.Travel;
            var locationText = travel.IsTravelling
                ? $"Travelling to {context.World.GetNode(travel.GetNextNodeId(context.Time.DayProgress)).DisplayName}"
                : $"At {context.World.GetNode(travel.CurrentNodeId).DisplayName}";
            GUILayout.Label(locationText);
            GUILayout.Label($"Speed per day: {ship.SpeedPerDay:0.##}");
            GUILayout.Label(WithTooltip(
                $"Cargo {ship.CurrentCargoAmount}/{ship.MaxCargoAmount}",
                GetCargoTooltip(ship)));
            GUILayout.Label($"Gold {ship.CurrentGold} G");
            var estimatedDays = GetEstimatedTravelDays(ship);
            if (estimatedDays.HasValue)
                GUILayout.Label($"Estimated duration: {estimatedDays.Value} {(estimatedDays.Value == 1 ? "day" : "days")}");
            GUILayout.EndArea();
        }

        private void DrawNodeInfo(Rect area)
        {
            GUILayout.BeginArea(Inset(area, ScreenLayout.Padding));
            if (mapController != null && !string.IsNullOrWhiteSpace(mapController.SelectedNodeId))
            {
                var selectedNode = context.World.GetNode(mapController.SelectedNodeId);
                GUILayout.Label(selectedNode.DisplayName, titleStyle);
                var locationActions = context.Entities
                    .Where(entity =>
                        entity.HasBehavior<WorldEntityBehavior>() &&
                        entity.GetBehavior<WorldEntityBehavior>().StartingNodeId == selectedNode.Id)
                    .SelectMany(entity => entity.Behaviors.OfType<ILocationAction>())
                    .ToArray();
                foreach (var action in locationActions)
                    GUILayout.Label(WithTooltip(
                        action.Title,
                        GetLocationActionTooltip(action)));
            }
            else
            {
                GUILayout.Label("No node selected", titleStyle);
            }
            GUILayout.EndArea();
        }

        private int? GetEstimatedTravelDays(TalesOfTheBrave.Simulation.Movement.Transport ship)
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
            TalesOfTheBrave.Simulation.Movement.TravelState travel)
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
            if (ship.Travel.IsInsideLocation)
            {
                GUILayout.Label($"Inside {context.GetInsideLocationEntity().DisplayName}");
                return;
            }

            if (!ship.Travel.IsTravelling)
            {
                var location = context.World.GetNode(ship.Travel.CurrentNodeId);
                var locationEntity = context.Entities.SingleOrDefault(entity =>
                    entity.HasBehavior<LocationBehavior>() &&
                    entity.HasBehavior<WorldEntityBehavior>() &&
                    entity.GetBehavior<WorldEntityBehavior>().StartingNodeId == location.Id);
                GUILayout.Label(locationEntity == null
                    ? $"At {location.DisplayName}"
                    : $"At location: {locationEntity.DisplayName}");
                if (locationEntity != null)
                    DrawAvailableActions(locationEntity);
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
                    GUILayout.Label($"Entering location {interaction.DisplayName}");
                    DrawInteractionLayoutActions(interaction);
                }
                else
                {
                    GUILayout.Label($"At sea — {visualProgress:P0} of current leg");
                    if (GUILayout.Button("Abort route"))
                        context.Movement.AbortRoute(ship.Id, context.Time.DayProgress);
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
            DrawAvailableActions(entity);
        }

        private void DrawAvailableActions(Entity entity)
        {
            foreach (var action in entity.Actions.Where(action => action.IsAvailable(context)))
                if (GUILayout.Button(action.Label)) action.Execute(context);
        }

        private void DrawLocationPanel(Rect panel)
        {
            var location = context.GetInsideLocationEntity();
            if (location == null) return;
            if (openLocationEntityId != location.Id)
            {
                openLocationEntityId = location.Id;
                selectedLocationAction = null;
                marketTradeSelection = null;
            }

            var content = Inset(panel, PanelContentPadding);
            var headerHeight = content.height * 0.3f;
            var footerHeight = content.height * 0.1f;
            var header = new Rect(content.x, content.y, content.width, headerHeight);
            var main = new Rect(
                content.x,
                header.yMax,
                content.width,
                Mathf.Max(0f, content.height - headerHeight - footerHeight));
            var footer = new Rect(content.x, main.yMax, content.width, footerHeight);

            var imageArea = new Rect(header.x, header.y, header.width * 0.5f, header.height);
            var titleArea = new Rect(
                imageArea.xMax,
                header.y,
                header.width - imageArea.width,
                header.height);
            var sprite = graphics.GetSprite(
                location.GetBehavior<LocationBehavior>().LocationViewSprite);
            if (sprite != null)
                DrawSprite(Inset(imageArea, 5f), sprite, ScaleMode.ScaleToFit);
            GUI.Box(imageArea, GUIContent.none, portraitFrameStyle);
            var locationBehavior = location.GetBehavior<LocationBehavior>();
            var nameArea = new Rect(
                titleArea.x + 12f,
                titleArea.y,
                titleArea.width - 24f,
                Mathf.Min(52f, titleArea.height * 0.42f));
            var descriptionArea = new Rect(
                nameArea.x,
                nameArea.yMax,
                nameArea.width,
                Mathf.Max(0f, titleArea.yMax - nameArea.yMax));
            GUI.Label(nameArea, location.DisplayName, locationTitleStyle);
            var descriptionStyle = new GUIStyle(logStyle)
            {
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = ToColor(uiDefinition.Menus.Font) }
            };
            GUI.Label(descriptionArea, locationBehavior.Description ?? string.Empty, descriptionStyle);

            DrawLocationMain(main, location);
            DrawLocationFooter(footer, location);
        }

        private void DrawLocationMain(Rect area, Entity location)
        {
            GUILayout.BeginArea(Inset(area, ScreenLayout.Padding));
            if (selectedLocationAction == null)
            {
                foreach (var action in location.Behaviors.OfType<ILocationAction>())
                {
                    var row = GUILayoutUtility.GetRect(
                        GUIContent.none,
                        locationRowStyle,
                        GUILayout.Height(72f),
                        GUILayout.ExpandWidth(true));
                    if (GUI.Button(
                            row,
                            WithTooltip(string.Empty, GetLocationActionTooltip(action)),
                            locationRowStyle))
                    {
                        selectedLocationAction = action;
                        marketScroll = Vector2.zero;
                        marketTradeSelection = action is MarketBehavior
                            ? new MarketTradeSelection()
                            : null;
                    }
                    DrawLocationActionRow(row, action);
                    GUILayout.Space(6f);
                }
            }
            else if (selectedLocationAction is MarketBehavior market)
            {
                DrawMarket(market);
            }
            GUILayout.EndArea();
        }

        private void DrawMarket(MarketBehavior market)
        {
            GUILayout.Label(market.Title, titleStyle);
            marketScroll = GUILayout.BeginScrollView(
                marketScroll,
                false,
                true,
                GUILayout.ExpandHeight(true));
            foreach (var marketCommodity in market.Commodities)
            {
                var commodity = marketCommodity.Commodity;
                var change = marketTradeSelection.GetChange(commodity);
                var cargo = context.PlayerShip.GetCargoAmount(commodity) + change;
                var marketAmount = marketCommodity.CurrentAmount - change;
                var row = GUILayoutUtility.GetRect(
                    GUIContent.none,
                    locationRowStyle,
                    GUILayout.Height(76f),
                    GUILayout.ExpandWidth(true));
                GUI.Box(row, GUIContent.none, locationRowStyle);
                DrawCargoItemIcon(
                    new Rect(row.x + 10f, row.y + 10f, 56f, 56f),
                    commodity);
                var titleRect = new Rect(row.x + 78f, row.y + 7f, 115f, 26f);
                GUI.Label(
                    titleRect,
                    WithTooltip(
                        commodity.Name,
                        $"Target: {marketCommodity.TargetAmount} {commodity.UnitAbbreviation}\n" +
                        $"Consumption: {marketCommodity.Consumption} {commodity.UnitAbbreviation}\n" +
                        $"Production: {marketCommodity.Production} {commodity.UnitAbbreviation}"),
                    new GUIStyle(titleStyle) { fontSize = 18 });
                GUI.Label(
                    new Rect(titleRect.x, titleRect.yMax, 170f, 36f),
                    $"Buy {marketCommodity.BuyPrice} G  Sell {marketCommodity.SellPrice} G\n" +
                    $"Cargo {cargo} {commodity.UnitAbbreviation}",
                    logStyle);
                var controlsX = row.xMax - 250f;
                var cost = change > 0
                    ? -change * marketCommodity.BuyPrice
                    : -change * marketCommodity.SellPrice;
                GUI.Label(
                    new Rect(controlsX - 125f, row.y + 9f, 120f, 24f),
                    FormatSignedGold(cost),
                    logStyle);
                if (GUI.Button(new Rect(controlsX, row.y + 8f, 36f, 27f), "<"))
                    marketTradeSelection.SelectBuy(
                        context.PlayerShip, marketCommodity, GetTradeQuantity());
                if (GUI.Button(new Rect(controlsX + 42f, row.y + 8f, 36f, 27f), ">"))
                    marketTradeSelection.SelectSell(
                        context.PlayerShip, marketCommodity, GetTradeQuantity());
                var previousContentColor = GUI.contentColor;
                GUI.contentColor = GetMarketStockColor(marketCommodity, marketAmount);
                GUI.Label(
                    new Rect(controlsX + 88f, row.y + 9f, 152f, 48f),
                    $"Market\n{marketAmount} {commodity.UnitAbbreviation}",
                    logStyle);
                GUI.contentColor = previousContentColor;
                GUILayout.Space(6f);
            }
            GUILayout.EndScrollView();

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Quantity", GUILayout.Width(70f));
            TradeQuantityButton("x1", 1);
            TradeQuantityButton("x10", 10);
            TradeQuantityButton("x100", 100);
            TradeQuantityButton("MAX", int.MaxValue);
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                $"Total: {FormatSignedGold(marketTradeSelection.GoldChange)}",
                GUILayout.Width(140f));
            if (GUILayout.Button("Cancel")) marketTradeSelection.Clear();
            var previousEnabled = GUI.enabled;
            GUI.enabled = marketTradeSelection.HasChanges;
            if (GUILayout.Button("Accept"))
                marketTradeSelection.Commit(context.PlayerShip, market);
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();
        }

        private void DrawLocationActionRow(Rect row, ILocationAction action)
        {
            DrawRoundIcon(
                new Rect(row.x + 10f, row.y + 10f, 52f, 52f),
                action.IconSprite);
            GUI.Label(
                new Rect(row.x + 76f, row.y + 12f, row.width * 0.38f, row.height - 24f),
                action.Title,
                new GUIStyle(titleStyle)
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleLeft
                });
            GUI.Label(
                new Rect(
                    row.x + row.width * 0.58f,
                    row.y + 8f,
                    row.width * 0.38f,
                    row.height - 16f),
                action.AdditionalInfo ?? string.Empty,
                new GUIStyle(logStyle) { alignment = TextAnchor.MiddleLeft });
        }

        private static string FormatSignedGold(int amount) =>
            amount == 0 ? "0 G" : $"{(amount > 0 ? "+" : "")}{amount} G";

        private void DrawCargoItemIcon(Rect rect, ICargoItem item) =>
            DrawRoundIcon(rect, item?.IconSprite);

        private void DrawRoundIcon(Rect rect, string spriteName)
        {
            GUI.Box(rect, GUIContent.none, iconFrameStyle);
            if (string.IsNullOrWhiteSpace(spriteName)) return;
            var sprite = graphics.GetSprite(spriteName);
            if (sprite != null)
                DrawSprite(Inset(rect, 5f), sprite, ScaleMode.ScaleToFit);
        }

        private static void DrawSprite(Rect rect, Sprite sprite, ScaleMode scaleMode)
        {
            if (sprite == null) return;
            var drawRect = rect;
            if (scaleMode == ScaleMode.ScaleToFit)
            {
                var spriteAspect = sprite.rect.width / sprite.rect.height;
                var rectAspect = rect.width / rect.height;
                if (spriteAspect > rectAspect)
                {
                    var height = rect.width / spriteAspect;
                    drawRect = new Rect(
                        rect.x,
                        rect.center.y - height * 0.5f,
                        rect.width,
                        height);
                }
                else
                {
                    var width = rect.height * spriteAspect;
                    drawRect = new Rect(
                        rect.center.x - width * 0.5f,
                        rect.y,
                        width,
                        rect.height);
                }
            }

            var texture = sprite.texture;
            var textureRect = sprite.textureRect;
            var uv = new Rect(
                textureRect.x / texture.width,
                textureRect.y / texture.height,
                textureRect.width / texture.width,
                textureRect.height / texture.height);
            GUI.DrawTextureWithTexCoords(drawRect, texture, uv, true);
        }

        private Color GetMarketStockColor(
            MarketCommodity commodity,
            int amount)
        {
            var percentage = commodity.TargetAmount <= 0
                ? 100f
                : amount * 100f / commodity.TargetAmount;
            if (percentage <= commodity.MinAmountPercentage)
                return new Color(0.95f, 0.2f, 0.2f);
            if (percentage < 90f)
                return new Color(1f, 0.82f, 0.18f);
            if (percentage <= 110f)
                return uiDefinition.MarketNormalStock;
            if (percentage < commodity.MaxAmountPercentage)
                return new Color(0.25f, 0.9f, 0.35f);
            return new Color(0.2f, 0.9f, 0.95f);
        }

        private void TradeQuantityButton(string label, int quantity)
        {
            var previous = GUI.backgroundColor;
            if (tradeQuantity == quantity)
                GUI.backgroundColor = new Color(0.65f, 0.9f, 0.65f);
            if (GUILayout.Button(label)) tradeQuantity = quantity;
            GUI.backgroundColor = previous;
        }

        private int GetTradeQuantity() => tradeQuantity;

        private void DrawLocationFooter(Rect footer, Entity location)
        {
            var buttonCount = selectedLocationAction == null ? 1 : 2;
            var spacing = 10f;
            var buttonWidth = Mathf.Min(
                260f,
                (footer.width - spacing * (buttonCount - 1)) / buttonCount);
            var buttonHeight = Mathf.Min(36f, footer.height);
            var totalWidth = buttonWidth * buttonCount + spacing * (buttonCount - 1);
            var button = new Rect(
                footer.center.x - totalWidth * 0.5f,
                footer.center.y - buttonHeight * 0.5f,
                buttonWidth,
                buttonHeight);
            if (selectedLocationAction != null)
            {
                if (GUI.Button(button, $"Back to {location.DisplayName}"))
                {
                    selectedLocationAction = null;
                    marketTradeSelection = null;
                }
                button.x += buttonWidth + spacing;
            }
            if (GUI.Button(button, $"Exit {location.DisplayName}"))
            {
                selectedLocationAction = null;
                marketTradeSelection = null;
                openLocationEntityId = null;
                context.ExitLocation();
                mapController?.ClearSelection();
            }
        }

        private string GetLocationActionTooltip(ILocationAction action)
        {
            if (!(action is MarketBehavior market)) return action.Title;
            return string.Join(
                "\n",
                market.Commodities.Select(entry =>
                {
                    var color = ColorUtility.ToHtmlStringRGB(
                        GetMarketStockColor(entry, entry.CurrentAmount));
                    return $"<color=#{color}>{entry.CurrentAmount} " +
                           $"{entry.Commodity.UnitAbbreviation} of " +
                           $"{entry.Commodity.Name} at B {entry.BuyPrice} / " +
                           $"S {entry.SellPrice} G</color>";
                }));
        }

        private static string GetCargoTooltip(
            TalesOfTheBrave.Simulation.Movement.Transport ship)
        {
            return "Wares\n" +
                   FormatCargoSection(ship.Wares) +
                   "\nSupplies\n" +
                   FormatCargoSection(ship.Supplies) +
                   "\nRestricted\n" +
                   FormatCargoSection(ship.Restricted);
        }

        private static string FormatCargoSection<T>(
            IEnumerable<T> cargo)
            where T : CargoItemStack
        {
            var entries = cargo
                .Select(item =>
                    $"{item.Amount} {item.Item.UnitAbbreviation} of {item.Item.Name}")
                .ToArray();
            return entries.Length == 0 ? "Empty" : string.Join("\n", entries);
        }

        private void GetLayoutRects(out Rect leftMenu, out Rect mainZone, out Rect bottomMenu)
        {
            if (mapCamera == null) mapCamera = Camera.main;
            ScreenLayout.GetRects(
                mapCamera,
                layoutDivider,
                out leftMenu,
                out mainZone,
                out bottomMenu);
        }

        private static Rect Inset(Rect rect, float padding)
        {
            return new Rect(
                rect.x + padding,
                rect.y + padding,
                Mathf.Max(0f, rect.width - padding * 2f),
                Mathf.Max(0f, rect.height - padding * 2f));
        }

        public static GUIContent WithTooltip(string label, string tooltip) =>
            new GUIContent(label, tooltip);

        private void DrawTooltip()
        {
            if (Event.current.type != EventType.Repaint ||
                touchInputDetected ||
                UnityEngine.Time.unscaledTime < suppressTooltipUntil) return;

            var text = GUI.tooltip;
            if (string.IsNullOrWhiteSpace(text) &&
                mapController != null)
                text = mapController.HoveredTooltip;
            if (string.IsNullOrWhiteSpace(text)) return;

            const float maximumWidth = 280f;
            var content = new GUIContent(text);
            var naturalSize = tooltipStyle.CalcSize(content);
            var width = Mathf.Min(maximumWidth, naturalSize.x + tooltipStyle.padding.horizontal);
            var height = tooltipStyle.CalcHeight(content, width);
            var position = Event.current.mousePosition + new Vector2(16f, 18f);
            position.x = Mathf.Min(position.x, Screen.width - width - 4f);
            position.y = Mathf.Min(position.y, Screen.height - height - 4f);
            GUI.Label(new Rect(position.x, position.y, width, height), content, tooltipStyle);
        }

        private static Color ToColor(Color color) => color;

        private void DrawEnteringLocationOverlay(Rect mapRect)
        {
            var location = context.GetPendingInteractionEntity();
            if (location == null || locationWindowFrame == null) return;

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
            portraitRect = ScaleRectAroundCenter(portraitRect, LocationPortraitInsetScale);
            var locationSprite = graphics.GetSprite(location.GetBehavior<LocationBehavior>().LocationViewSprite);

            if (Event.current.type == EventType.Repaint)
                DrawCircularTexture(portraitRect, locationSprite.texture);
            GUI.DrawTexture(frameRect, locationWindowFrame.texture, ScaleMode.ScaleToFit, true);

            var labelRect = new Rect(frameRect.x, frameRect.yMax + 2f, size, 38f);
            GUI.Label(labelRect, location.DisplayName, encounterTitleStyle);
            var buttonRect = new Rect(frameRect.center.x - 90f, labelRect.yMax + 6f, 180f, 32f);
            foreach (var action in location.Actions.Where(action => action.IsAvailable(context)))
            {
                if (GUI.Button(buttonRect, action.Label)) action.Execute(context);
                buttonRect.y += 38f;
            }
        }

        private void DrawCircularTexture(Rect rect, Texture texture)
        {
            if (circularImageMaterial == null) return;

            var previousSrgbWrite = GL.sRGBWrite;
            GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
            circularImageMaterial.mainTexture = texture;
            circularImageMaterial.SetPass(0);
            GL.PushMatrix();
            try
            {
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
            }
            finally
            {
                GL.PopMatrix();
                GL.sRGBWrite = previousSrgbWrite;
            }
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
            if (tooltipTexture != null) Destroy(tooltipTexture);
            if (iconFrameTexture != null) Destroy(iconFrameTexture);
            if (portraitFrameTexture != null) Destroy(portraitFrameTexture);
            if (nightTintTexture != null) Destroy(nightTintTexture);
        }

    }
}
