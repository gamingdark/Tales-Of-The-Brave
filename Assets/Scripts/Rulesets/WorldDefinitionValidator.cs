using System;
using System.Collections.Generic;
using TalesOfTheBrave.Graphics;
using TalesOfTheBrave.Simulation.Time;
using UnityEngine;

namespace TalesOfTheBrave.Simulation.Rulesets
{
    public static class WorldDefinitionValidator
    {
        public static void Validate(WorldDefinition definition, ISpriteNameLookup spriteLookup = null)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition.Nodes == null) throw new InvalidOperationException("World node definitions are required.");
            if (definition.Edges == null) throw new InvalidOperationException("World edge definitions are required.");
            if (definition.Entities == null) throw new InvalidOperationException("Entity definitions are required.");
            if (definition.Commodities == null)
                throw new InvalidOperationException("Commodity definitions are required.");
            if (definition.MapWidth <= 0f || definition.MapHeight <= 0f ||
                float.IsNaN(definition.MapWidth) || float.IsNaN(definition.MapHeight) ||
                float.IsInfinity(definition.MapWidth) || float.IsInfinity(definition.MapHeight))
                throw new InvalidOperationException("Map width and height must be positive.");
            if (string.IsNullOrWhiteSpace(definition.MapBackgroundSprite))
                throw new InvalidOperationException("A map background sprite is required.");
            if (spriteLookup != null && !spriteLookup.ContainsSprite(definition.MapBackgroundSprite))
                throw new InvalidOperationException(
                    $"World definition references missing map background sprite '{definition.MapBackgroundSprite}'.");
            if (string.IsNullOrWhiteSpace(definition.SceneBackgroundSprite))
                throw new InvalidOperationException("A scene background sprite is required.");
            if (spriteLookup != null && !spriteLookup.ContainsSprite(definition.SceneBackgroundSprite))
                throw new InvalidOperationException(
                    $"World definition references missing scene background sprite '{definition.SceneBackgroundSprite}'.");
            ValidateTimeSystem(definition.TimeSystem);
            ValidateEconomySystem(definition.Economy);
            ValidateUiSystem(definition.UI);
            var commodityNames = ValidateCommodities(
                definition.Commodities,
                spriteLookup);

            var allIds = new HashSet<string>(StringComparer.Ordinal);
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in definition.Nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Id))
                    throw new InvalidOperationException("Every world node requires an ID.");
                if (!nodeIds.Add(node.Id) || !allIds.Add(node.Id))
                    throw new InvalidOperationException($"Duplicate definition ID '{node.Id}'.");
            }

            foreach (var edge in definition.Edges)
            {
                if (edge == null || string.IsNullOrWhiteSpace(edge.Id))
                    throw new InvalidOperationException("Every world edge requires an ID.");
                if (!allIds.Add(edge.Id))
                    throw new InvalidOperationException($"Duplicate definition ID '{edge.Id}'.");
                if (edge.Distance <= 0f)
                    throw new InvalidOperationException($"Edge '{edge.Id}' must have a positive distance.");
                if (!nodeIds.Contains(edge.NodeAId) || !nodeIds.Contains(edge.NodeBId))
                    throw new InvalidOperationException($"Edge '{edge.Id}' references a missing endpoint node.");
            }

            var playerControlledCount = 0;
            foreach (var entity in definition.Entities)
            {
                if (entity == null || string.IsNullOrWhiteSpace(entity.Id))
                    throw new InvalidOperationException("Every entity requires an ID.");
                if (!allIds.Add(entity.Id))
                    throw new InvalidOperationException($"Duplicate definition ID '{entity.Id}'.");
                if (entity.Behaviors == null)
                    throw new InvalidOperationException($"Entity '{entity.Id}' requires a behaviors definition.");

                var behaviors = entity.Behaviors;
                if (behaviors.PlayerControlledBehavior != null)
                {
                    playerControlledCount++;
                }
                if (behaviors.TransportBehavior != null && behaviors.TransportBehavior.SpeedPerDay <= 0f)
                    throw new InvalidOperationException($"Transport entity '{entity.Id}' must have positive speed.");
                if (behaviors.TransportBehavior != null && behaviors.TransportBehavior.MaxCargoAmount <= 0)
                    throw new InvalidOperationException(
                        $"Transport entity '{entity.Id}' must have positive maximum cargo.");
                if (behaviors.TransportBehavior != null && behaviors.TransportBehavior.CurrentGold < 0)
                    throw new InvalidOperationException(
                        $"Transport entity '{entity.Id}' cannot start with negative gold.");
                if (behaviors.PlayerControlledBehavior != null && behaviors.TransportBehavior == null)
                    throw new InvalidOperationException(
                        $"Player-controlled entity '{entity.Id}' requires a transport behavior.");
                if (behaviors.WorldEntityBehavior == null)
                    throw new InvalidOperationException($"Map entity '{entity.Id}' requires a world entity behavior.");
                if (!nodeIds.Contains(behaviors.WorldEntityBehavior.StartingNodeId))
                    throw new InvalidOperationException(
                        $"Entity '{entity.Id}' starting node '{behaviors.WorldEntityBehavior.StartingNodeId}' does not exist.");
                if (behaviors.DrawableBehavior == null)
                    throw new InvalidOperationException($"Map entity '{entity.Id}' requires a drawable behavior.");
                if (spriteLookup != null && !spriteLookup.ContainsSprite(behaviors.DrawableBehavior.MapIconSprite))
                    throw new InvalidOperationException(
                        $"Entity '{entity.Id}' references missing map icon sprite '{behaviors.DrawableBehavior.MapIconSprite}'.");
                if (behaviors.LocationBehavior != null &&
                    (spriteLookup == null ? string.IsNullOrWhiteSpace(behaviors.LocationBehavior.LocationViewSprite)
                        : !spriteLookup.ContainsSprite(behaviors.LocationBehavior.LocationViewSprite)))
                    throw new InvalidOperationException(
                        $"Location entity '{entity.Id}' references missing location view sprite '{behaviors.LocationBehavior.LocationViewSprite}'.");
                ValidateMarket(entity, commodityNames, spriteLookup);
            }
            if (playerControlledCount != 1)
                throw new InvalidOperationException("Exactly one player-controlled entity is required.");
        }

        private static HashSet<string> ValidateCommodities(
            IEnumerable<CommodityDefinition> commodities,
            ISpriteNameLookup spriteLookup)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var commodity in commodities)
            {
                if (commodity == null || string.IsNullOrWhiteSpace(commodity.Name))
                    throw new InvalidOperationException("Every commodity requires a name.");
                if (!names.Add(commodity.Name))
                    throw new InvalidOperationException($"Duplicate commodity name '{commodity.Name}'.");
                if (commodity.DefaultPrice < 0)
                    throw new InvalidOperationException(
                        $"Commodity '{commodity.Name}' cannot have a negative default price.");
                if (commodity.Unit == null ||
                    string.IsNullOrWhiteSpace(commodity.Unit.FullName) ||
                    string.IsNullOrWhiteSpace(commodity.Unit.Abbreviation))
                    throw new InvalidOperationException(
                        $"Commodity '{commodity.Name}' requires a complete unit definition.");
                ValidateOptionalSprite(
                    commodity.IconSprite,
                    spriteLookup,
                    $"Commodity '{commodity.Name}'");
            }
            return names;
        }

        private static void ValidateMarket(
            EntityDefinition entity,
            HashSet<string> commodityNames,
            ISpriteNameLookup spriteLookup)
        {
            var market = entity.Behaviors.MarketBehavior;
            if (market == null) return;
            if (entity.Behaviors.LocationBehavior == null)
                throw new InvalidOperationException(
                    $"Market entity '{entity.Id}' must also be a location.");
            if (string.IsNullOrWhiteSpace(market.Title))
                throw new InvalidOperationException($"Market entity '{entity.Id}' requires a title.");
            ValidateOptionalSprite(
                market.IconSprite,
                spriteLookup,
                $"Market entity '{entity.Id}'");
            if (market.Commodities == null)
                throw new InvalidOperationException(
                    $"Market entity '{entity.Id}' requires a commodity list.");

            var marketCommodityNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var commodity in market.Commodities)
            {
                if (commodity == null ||
                    string.IsNullOrWhiteSpace(commodity.CommodityName) ||
                    !commodityNames.Contains(commodity.CommodityName))
                    throw new InvalidOperationException(
                        $"Market entity '{entity.Id}' references an unknown commodity.");
                if (!marketCommodityNames.Add(commodity.CommodityName))
                    throw new InvalidOperationException(
                        $"Market entity '{entity.Id}' contains duplicate commodity '{commodity.CommodityName}'.");
                if (commodity.TargetAmount <= 0)
                    throw new InvalidOperationException(
                        $"Market commodity '{commodity.CommodityName}' must have a positive target amount.");
                if (commodity.MinAmountPercentage < 0f ||
                    commodity.MaxAmountPercentage < commodity.MinAmountPercentage)
                    throw new InvalidOperationException(
                        $"Market commodity '{commodity.CommodityName}' has invalid amount percentages.");
                if (commodity.MinAmountPercentage >= 90f ||
                    commodity.MaxAmountPercentage <= 110f)
                    throw new InvalidOperationException(
                        $"Market commodity '{commodity.CommodityName}' minimum and maximum percentages " +
                        "must remain outside the 90-110 normal-price range.");
                if (commodity.Consumption < 0 || commodity.Production < 0)
                    throw new InvalidOperationException(
                        $"Market commodity '{commodity.CommodityName}' cannot have negative production or consumption.");
                if (commodity.NormalPriceCoefficient <= 0f ||
                    float.IsNaN(commodity.NormalPriceCoefficient) ||
                    float.IsInfinity(commodity.NormalPriceCoefficient))
                    throw new InvalidOperationException(
                        $"Market commodity '{commodity.CommodityName}' must have a positive normal price coefficient.");
            }
        }

        private static void ValidateOptionalSprite(
            string spriteName,
            ISpriteNameLookup spriteLookup,
            string owner)
        {
            if (string.IsNullOrWhiteSpace(spriteName) || spriteLookup == null) return;
            if (!spriteLookup.ContainsSprite(spriteName))
                throw new InvalidOperationException(
                    $"{owner} references missing icon sprite '{spriteName}'.");
        }

        private static void ValidateTimeSystem(TimeSystemDefinition time)
        {
            if (time == null) throw new InvalidOperationException("A time system definition is required.");
            if (time.SecondsPerDay <= 0f) throw new InvalidOperationException("Time system day length must be positive.");
            if (time.DaysPerMonth <= 0) throw new InvalidOperationException("Time system days per month must be positive.");
            if (time.HoursPerDay <= 0) throw new InvalidOperationException("Time system hours per day must be positive.");
            if (time.DayStartHourOffset < 0 || time.DayStartHourOffset >= time.HoursPerDay)
                throw new InvalidOperationException("Time system day-start offset must be within the configured day.");
            if (time.MidnightHour < 0f || time.MidnightHour >= time.HoursPerDay ||
                float.IsNaN(time.MidnightHour) || float.IsInfinity(time.MidnightHour))
                throw new InvalidOperationException(
                    "Time system midnight hour must be within the configured day.");
            if (time.NightDarkeningDurationHours <= 0f ||
                time.NightDarkeningDurationHours > time.HoursPerDay ||
                float.IsNaN(time.NightDarkeningDurationHours) ||
                float.IsInfinity(time.NightDarkeningDurationHours))
                throw new InvalidOperationException(
                    "Time system night darkening duration must be positive and no longer than a day.");
            if (time.NightBrighteningDurationHours <= 0f ||
                time.NightBrighteningDurationHours > time.HoursPerDay ||
                float.IsNaN(time.NightBrighteningDurationHours) ||
                float.IsInfinity(time.NightBrighteningDurationHours))
                throw new InvalidOperationException(
                    "Time system night brightening duration must be positive and no longer than a day.");
            ValidateColor(time.NightTint, true, "night tint");
            if (time.AllowedSpeeds == null || time.AllowedSpeeds.Count == 0)
                throw new InvalidOperationException("Time system requires at least one allowed running speed.");

            var speeds = new HashSet<TimeSpeed>();
            foreach (var speed in time.AllowedSpeeds)
            {
                if (!Enum.IsDefined(typeof(TimeSpeed), speed) || speed == TimeSpeed.Paused || speed == TimeSpeed.Developer)
                    throw new InvalidOperationException($"Time speed '{speed}' cannot be configured as a running speed.");
                if (!speeds.Add(speed))
                    throw new InvalidOperationException($"Time speed '{speed}' is configured more than once.");
            }
        }

        private static void ValidateEconomySystem(EconomySystemDefinition economy)
        {
            if (economy == null)
                throw new InvalidOperationException("An economy system definition is required.");
            if (economy.DailyPriceAdjustmentRate <= 0f ||
                economy.DailyPriceAdjustmentRate > 1f ||
                float.IsNaN(economy.DailyPriceAdjustmentRate))
                throw new InvalidOperationException(
                    "Economy daily price adjustment rate must be between zero and one.");
            if (economy.MinimumDailyPriceAdjustment < 0)
                throw new InvalidOperationException(
                    "Economy minimum daily price adjustment cannot be negative.");
            if (economy.RandomPriceFluctuationPercentage < 0f ||
                float.IsNaN(economy.RandomPriceFluctuationPercentage) ||
                float.IsInfinity(economy.RandomPriceFluctuationPercentage))
                throw new InvalidOperationException(
                    "Economy random price fluctuation cannot be negative.");
            if (economy.BuySellSpreadPercentage < 0f ||
                economy.BuySellSpreadPercentage >= 100f ||
                float.IsNaN(economy.BuySellSpreadPercentage))
                throw new InvalidOperationException(
                    "Economy buy/sell spread must be between zero and 100 percent.");
        }

        private static void ValidateUiSystem(UiSystemDefinition ui)
        {
            if (ui == null) throw new InvalidOperationException("A UI definition is required.");
            if (ui.Menus == null) throw new InvalidOperationException("A menu UI style is required.");
            if (ui.Tooltips == null) throw new InvalidOperationException("A tooltip UI style is required.");

            ValidateColor(ui.Menus.Background, true, "menu background");
            ValidateColor(ui.Menus.Border, false, "menu border");
            ValidateColor(ui.Menus.Font, false, "menu font");
            ValidateBorderWidth(ui.Menus.BorderWidth, "menu");
            ValidateColor(ui.Tooltips.Background, true, "tooltip background");
            ValidateColor(ui.Tooltips.Border, false, "tooltip border");
            ValidateColor(ui.Tooltips.Font, false, "tooltip font");
            ValidateColor(ui.MarketNormalStock, false, "market normal stock");
            ValidateBorderWidth(ui.Tooltips.BorderWidth, "tooltip");
        }

        private static void ValidateColor(Color color, bool requiresAlpha, string name)
        {
            if (!IsUnit(color.r) || !IsUnit(color.g) || !IsUnit(color.b) ||
                requiresAlpha && !IsUnit(color.a))
                throw new InvalidOperationException($"The {name} color components must be between 0 and 1.");
        }

        private static void ValidateBorderWidth(float width, string name)
        {
            if (float.IsNaN(width) || float.IsInfinity(width) || width < 0f)
                throw new InvalidOperationException($"The {name} border width cannot be negative.");
        }

        private static bool IsUnit(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f;
    }
}
