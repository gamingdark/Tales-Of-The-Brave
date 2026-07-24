using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TalesOfTheBrave.Graphics;
using TalesOfTheBrave.Simulation.Core;
using TalesOfTheBrave.Simulation.Entities;
using TalesOfTheBrave.Simulation.Rulesets;
using TalesOfTheBrave.Simulation.World;
using TalesOfTheBrave.Simulation.Time;
using TalesOfTheBrave.Simulation.Economy;

public sealed class WorldDefinitionTests
{
    [Test]
    public void DefaultDefinitionContainsRequiredEntitiesWithoutRequiringExactCounts()
    {
        var definition = WorldDefinition.CreateDefault();
        var game = GameFactory.Create(definition);

        AssertNode(game.World.GetNode("node_klaipeda"), "Klaipėda", 0.62f, 0.2f);
        AssertNode(game.World.GetNode("node_riga"), "Riga", 0.73f, 0.33f);
        AssertNode(game.World.GetNode("node_helsinki"), "Helsinki", 0.70f, 0.63f);
        Assert.That(game.World.GetEdge("route_klaipeda_courland").Distance, Is.EqualTo(25f));
        Assert.That(game.World.GetEdge("route_courland_irbe").Distance, Is.EqualTo(25f));
        Assert.That(game.World.GetEdge("route_irbe_riga").Distance, Is.EqualTo(25f));
        Assert.That(game.World.GetEdge("route_courland_saaremaa").Distance, Is.EqualTo(40f));
        Assert.That(game.World.GetEdge("route_irbe_saaremaa").Distance, Is.EqualTo(25f));
        Assert.That(game.World.GetEdge("route_saaremaa_helsinki").Distance, Is.EqualTo(40f));

        AssertLocation(game, "location_klaipeda", "node_klaipeda", "icons.4", "img-klaipeda");
        AssertLocation(game, "location_riga", "node_riga", "icons.3", "img-riga");
        AssertLocation(game, "location_helsinki", "node_helsinki", "icons.5", "img-helsinki");
        Assert.That(game.PlayerShip.Id, Is.EqualTo("player_ship"));
        Assert.That(game.PlayerShip.DisplayName, Is.EqualTo("The Unsinkable"));
        Assert.That(game.PlayerShip.SpeedPerDay, Is.EqualTo(25f));
        Assert.That(game.PlayerShip.Entity.HasBehavior<PlayerControlledBehavior>(), Is.True);
        Assert.That(game.PlayerShip.Entity.GetBehavior<TransportBehavior>().SpeedPerDay, Is.EqualTo(25f));
        Assert.That(game.PlayerShip.MaxCargoAmount, Is.EqualTo(200));
        Assert.That(game.PlayerShip.CurrentCargo, Is.Empty);
        Assert.That(game.PlayerShip.CurrentCargoAmount, Is.Zero);
        Assert.That(game.PlayerShip.CurrentGold, Is.EqualTo(1000));
        Assert.That(game.PlayerShip.Travel.CurrentNodeId, Is.EqualTo("node_klaipeda"));
        Assert.That(game.PlayerShip.Entity.GetBehavior<DrawableBehavior>().MapIconSprite, Is.EqualTo("icons.8"));
        Assert.That(game.Time.SecondsPerDay, Is.EqualTo(7.5f));
        Assert.That(game.Time.CurrentDate.DaysPerMonth, Is.EqualTo(30));
        Assert.That(game.Time.HoursPerDay, Is.EqualTo(24));
        Assert.That(game.Time.DayStartHourOffset, Is.EqualTo(7));
        Assert.That(game.Time.AllowedSpeeds,
            Is.EqualTo(new[] { TimeSpeed.Normal, TimeSpeed.Fast, TimeSpeed.VeryFast }));
        Assert.That(definition.MapBackgroundSprite, Is.EqualTo("map"));
        Assert.That(definition.SceneBackgroundSprite, Is.EqualTo("wooden-background"));
        Assert.That(definition.MapWidth, Is.EqualTo(11.2f));
        Assert.That(definition.MapHeight, Is.EqualTo(7.9f));
        Assert.That(definition.UI.Menus.Background.a, Is.EqualTo(0.88f));
        Assert.That(definition.UI.Menus.BorderWidth, Is.EqualTo(2f));
        Assert.That(definition.UI.Tooltips.Background.r,
            Is.GreaterThan(definition.UI.Menus.Background.r));
        Assert.That(definition.UI.Tooltips.Font.r,
            Is.GreaterThan(definition.UI.Menus.Font.r));
        Assert.That(game.Commodities.Keys,
            Is.EquivalentTo(new[] { "Timber", "Grain", "Tar", "Iron" }));
        foreach (var commodity in game.Commodities.Values)
        {
            Assert.That(commodity.DefaultPrice, Is.EqualTo(100));
            Assert.That(commodity.UnitName, Is.EqualTo("tonnes"));
            Assert.That(commodity.UnitAbbreviation, Is.EqualTo("t"));
        }
        AssertMarket(game, "location_klaipeda", "Grain", "Timber");
        AssertMarket(game, "location_riga", "Tar", "Grain", "Timber");
        AssertMarket(game, "location_helsinki", "Tar", "Timber", "Iron");
        AssertMarket(game, "location_stockholm", "Iron", "Timber", "Grain");
    }

    [Test]
    public void CreationIsUnaffectedByAdditionalValidNodeAndEntity()
    {
        var definition = WorldDefinition.CreateDefault();
        definition.Nodes.Add(new WorldNodeDefinition("node_extra", "Extra", WorldNodeType.Location, 0.1f, 0.1f));
        definition.Entities.Add(CreateLocation("location_extra", "Extra", "node_extra", "icons.extra"));

        Assert.DoesNotThrow(() => GameFactory.Create(definition));
    }

    [Test]
    public void CreationRejectsDuplicateIds()
    {
        var definition = CreateMinimalDefinition();
        definition.Nodes.Add(new WorldNodeDefinition("node_a", "Duplicate", WorldNodeType.Location, 0f, 0f));

        AssertValidationFailure(definition, "Duplicate definition ID");
    }

    [Test]
    public void CreationRejectsMissingEdgeEndpointNode()
    {
        var definition = CreateMinimalDefinition();
        definition.Edges[0].NodeBId = "node_missing";

        AssertValidationFailure(definition, "missing endpoint node");
    }

    [Test]
    public void CreationRejectsInvalidEdgeDistance()
    {
        var definition = CreateMinimalDefinition();
        definition.Edges[0].Distance = 0f;

        AssertValidationFailure(definition, "positive distance");
    }

    [Test]
    public void CreationRejectsInvalidEntityStartingNode()
    {
        var definition = CreateMinimalDefinition();
        definition.Entities[0].Behaviors.WorldEntityBehavior.StartingNodeId = "node_missing";

        AssertValidationFailure(definition, "does not exist");
    }

    [Test]
    public void CreationRejectsMultiplePlayerControlledEntities()
    {
        var definition = CreateMinimalDefinition();
        definition.Entities.Add(CreatePlayer("other_player", "node_b", "icons.other"));

        AssertValidationFailure(definition, "Exactly one player-controlled entity");
    }

    [Test]
    public void ValidationRejectsPlayerControlledEntityWithoutTransportBehavior()
    {
        var definition = CreateMinimalDefinition();
        definition.Entities[0].Behaviors.TransportBehavior = null;

        AssertValidationFailure(definition, "requires a transport behavior");
    }

    [Test]
    public void ValidationRejectsInvalidTransportSpeed()
    {
        var definition = CreateMinimalDefinition();
        definition.Entities[0].Behaviors.TransportBehavior.SpeedPerDay = 0f;

        AssertValidationFailure(definition, "positive speed");
    }

    [Test]
    public void GraphicsValidationAcceptsAllDefaultSpriteNames()
    {
        var sprites = new SpriteNames(
            "icons.3", "icons.4", "icons.5", "icons.7", "icons.8",
            "img-klaipeda", "img-riga", "img-helsinki", "img-stockholm",
            "commodity_icons.0", "commodity_icons.1",
            "commodity_icons.2", "commodity_icons.3",
            "location-actions.0", "location-actions.1",
            "location-actions.2", "location-actions.3",
            "map", "wooden-background");
        Assert.DoesNotThrow(() => WorldDefinitionValidator.Validate(WorldDefinition.CreateDefault(), sprites));
    }

    [Test]
    public void DefaultLocationActionsAndCommoditiesHaveTemporaryIcons()
    {
        var definition = WorldDefinition.CreateDefault();

        Assert.That(
            definition.Commodities.All(commodity =>
                !string.IsNullOrWhiteSpace(commodity.IconSprite)),
            Is.True);
        Assert.That(
            definition.Entities
                .Where(entity => entity.Behaviors.LocationBehavior != null)
                .All(entity =>
                    !string.IsNullOrWhiteSpace(
                        entity.Behaviors.LocationBehavior.Description) &&
                    !string.IsNullOrWhiteSpace(
                        entity.Behaviors.MarketBehavior.IconSprite)),
            Is.True);
    }

    [Test]
    public void GraphicsValidationRejectsMissingMapBackgroundSprite()
    {
        var definition = CreateMinimalDefinition();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            WorldDefinitionValidator.Validate(
                definition, new SpriteNames("icons.ship", "icons.location", "img-location", "wooden-background")));

        StringAssert.Contains("map", exception.Message);
    }

    [Test]
    public void GraphicsValidationRejectsMissingSceneBackgroundSprite()
    {
        var definition = CreateMinimalDefinition();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            WorldDefinitionValidator.Validate(
                definition, new SpriteNames("map", "icons.ship", "icons.location", "img-location")));

        StringAssert.Contains("wooden-background", exception.Message);
    }

    [Test]
    public void GraphicsValidationRejectsMissingEntitySprite()
    {
        var definition = CreateMinimalDefinition();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            WorldDefinitionValidator.Validate(
                definition, new SpriteNames("map", "wooden-background", "icons.location")));

        StringAssert.Contains("player_ship", exception.Message);
        StringAssert.Contains("icons.ship", exception.Message);
    }

    [Test]
    public void GraphicsValidationRejectsMissingLocationViewSprite()
    {
        var definition = CreateMinimalDefinition();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            WorldDefinitionValidator.Validate(
                definition, new SpriteNames("map", "wooden-background", "icons.ship", "icons.location")));

        StringAssert.Contains("location_b", exception.Message);
        StringAssert.Contains("img-location", exception.Message);
    }

    [Test]
    public void EnteringLocationQueryReturnsDestinationLocationEntity()
    {
        var game = GameFactory.Create();
        game.Movement.PlanDestination(game.PlayerShip.Id, "node_helsinki");
        game.Time.AdvanceDays(5);
        game.Time.Tick(game.Time.SecondsPerDay * 0.9f);

        var location = game.GetPendingInteractionEntity();

        Assert.That(location.Id, Is.EqualTo("location_helsinki"));
        Assert.That(location.GetBehavior<LocationBehavior>().LocationViewSprite, Is.EqualTo("img-helsinki"));
        Assert.That(location.Actions.Select(action => action.Label),
            Is.EqualTo(new[] { "Enter location", "Go into Helsinki" }));
    }

    [Test]
    public void FactoryUsesCustomTimeSystemDefinition()
    {
        var definition = CreateMinimalDefinition();
        definition.TimeSystem = new TimeSystemDefinition
        {
            SecondsPerDay = 42f,
            DaysPerMonth = 18,
            HoursPerDay = 12,
            DayStartHourOffset = 4,
            AllowedSpeeds = new List<TimeSpeed> { TimeSpeed.Fast }
        };

        var game = GameFactory.Create(definition);

        Assert.That(game.Time.SecondsPerDay, Is.EqualTo(42f));
        Assert.That(game.Time.CurrentDate.DaysPerMonth, Is.EqualTo(18));
        Assert.That(game.Time.HoursPerDay, Is.EqualTo(12));
        Assert.That(game.Time.GetFormattedTime(), Is.EqualTo("04:00"));
        Assert.That(game.Time.Speed, Is.EqualTo(TimeSpeed.Fast));
    }

    [TestCase(0, 24, 7, "day length")]
    [TestCase(7.5f, 0, 7, "days per month")]
    [TestCase(7.5f, 30, -1, "day-start offset")]
    public void ValidationRejectsInvalidTimeSettings(
        float secondsPerDay, int daysPerMonth, int dayStartOffset, string expectedMessage)
    {
        var definition = CreateMinimalDefinition();
        definition.TimeSystem.SecondsPerDay = secondsPerDay;
        definition.TimeSystem.DaysPerMonth = daysPerMonth;
        definition.TimeSystem.DayStartHourOffset = dayStartOffset;

        AssertValidationFailure(definition, expectedMessage);
    }

    [Test]
    public void ValidationRejectsPauseAsConfiguredRunningSpeed()
    {
        var definition = CreateMinimalDefinition();
        definition.TimeSystem.AllowedSpeeds = new List<TimeSpeed> { TimeSpeed.Paused };

        AssertValidationFailure(definition, "cannot be configured");
    }

    [Test]
    public void ValidationRejectsInvalidHoursPerDay()
    {
        var definition = CreateMinimalDefinition();
        definition.TimeSystem.HoursPerDay = 0;

        AssertValidationFailure(definition, "hours per day");
    }

    [Test]
    public void ValidationRejectsDuplicateRunningSpeeds()
    {
        var definition = CreateMinimalDefinition();
        definition.TimeSystem.AllowedSpeeds = new List<TimeSpeed> { TimeSpeed.Normal, TimeSpeed.Normal };

        AssertValidationFailure(definition, "more than once");
    }

    [Test]
    public void ValidationRejectsMissingUiDefinition()
    {
        var definition = CreateMinimalDefinition();
        definition.UI = null;

        AssertValidationFailure(definition, "UI definition");
    }

    [Test]
    public void ValidationRejectsOutOfRangeUiColor()
    {
        var definition = CreateMinimalDefinition();
        definition.UI.Tooltips.Background.a = 1.1f;

        AssertValidationFailure(definition, "tooltip background color components");
    }

    [Test]
    public void ValidationRejectsNegativeUiBorderWidth()
    {
        var definition = CreateMinimalDefinition();
        definition.UI.Menus.BorderWidth = -1f;

        AssertValidationFailure(definition, "menu border width");
    }

    [TestCase(0f, 7.9f)]
    [TestCase(11.2f, 0f)]
    public void ValidationRejectsInvalidMapSize(float width, float height)
    {
        var definition = CreateMinimalDefinition();
        definition.MapWidth = width;
        definition.MapHeight = height;

        AssertValidationFailure(definition, "Map width and height");
    }

    [Test]
    public void ValidationRejectsDuplicateCommodityNames()
    {
        var definition = CreateMinimalDefinition();
        definition.Commodities.Add(new CommodityDefinition("Grain", 100));
        definition.Commodities.Add(new CommodityDefinition("Grain", 120));

        AssertValidationFailure(definition, "Duplicate commodity name");
    }

    [Test]
    public void ValidationRejectsUnknownMarketCommodity()
    {
        var definition = CreateMinimalDefinition();
        definition.Entities[1].Behaviors.MarketBehavior =
            new MarketBehaviorDefinition(new[] { "Unknown" });

        AssertValidationFailure(definition, "unknown commodity");
    }

    [Test]
    public void ValidationRejectsDuplicateCommodityWithinMarket()
    {
        var definition = CreateMinimalDefinition();
        definition.Commodities.Add(new CommodityDefinition("Grain", 100));
        definition.Entities[1].Behaviors.MarketBehavior =
            new MarketBehaviorDefinition(new[] { "Grain", "Grain" });

        AssertValidationFailure(definition, "duplicate commodity");
    }

    [Test]
    public void ValidationRejectsInvalidCargoCapacity()
    {
        var definition = CreateMinimalDefinition();
        definition.Entities[0].Behaviors.TransportBehavior.MaxCargoAmount = 0;

        AssertValidationFailure(definition, "positive maximum cargo");
    }

    [Test]
    public void ValidationRejectsNegativeStartingGold()
    {
        var definition = CreateMinimalDefinition();
        definition.Entities[0].Behaviors.TransportBehavior.CurrentGold = -1;

        AssertValidationFailure(definition, "negative gold");
    }

    [Test]
    public void MarketPurchaseIsPendingUntilCommitted()
    {
        var definition = WorldDefinition.CreateDefault();
        definition.Economy.BuySellSpreadPercentage = 0f;
        var game = GameFactory.Create(definition);
        var market = game.Entities
            .Single(entity => entity.Id == "location_klaipeda")
            .GetBehavior<MarketBehavior>();
        var grain = market.Commodities.Single(entry => entry.Commodity.Name == "Grain");
        var selection = new MarketTradeSelection();

        var selected = selection.SelectBuy(game.PlayerShip, grain, 10);

        Assert.That(selected, Is.EqualTo(10));
        Assert.That(selection.GoldChange, Is.EqualTo(-1000));
        Assert.That(game.PlayerShip.CurrentCargoAmount, Is.Zero);
        Assert.That(game.PlayerShip.CurrentGold, Is.EqualTo(1000));
        Assert.That(grain.CurrentAmount, Is.EqualTo(100));

        selection.Commit(game.PlayerShip, market);

        Assert.That(game.PlayerShip.GetCargoAmount(grain.Commodity), Is.EqualTo(10));
        Assert.That(game.PlayerShip.CurrentGold, Is.Zero);
        Assert.That(grain.CurrentAmount, Is.EqualTo(90));
    }

    [Test]
    public void MarketPurchaseCapsAtMinimumStock()
    {
        var definition = WorldDefinition.CreateDefault();
        definition.Entities
            .Single(entity => entity.Behaviors.PlayerControlledBehavior != null)
            .Behaviors.TransportBehavior.CurrentGold = 100000;
        var game = GameFactory.Create(definition);
        var market = game.Entities
            .Single(entity => entity.Id == "location_klaipeda")
            .GetBehavior<MarketBehavior>();
        var grain = market.Commodities.Single(entry => entry.Commodity.Name == "Grain");
        var selection = new MarketTradeSelection();

        selection.SelectBuy(game.PlayerShip, grain, int.MaxValue);
        selection.Commit(game.PlayerShip, market);

        Assert.That(game.PlayerShip.GetCargoAmount(grain.Commodity), Is.EqualTo(50));
        Assert.That(grain.CurrentAmount, Is.EqualTo(50));
    }

    [Test]
    public void MarketSaleCapsAtCargoAndMaximumStock()
    {
        var definition = WorldDefinition.CreateDefault();
        definition.Economy.BuySellSpreadPercentage = 0f;
        var game = GameFactory.Create(definition);
        var market = game.Entities
            .Single(entity => entity.Id == "location_klaipeda")
            .GetBehavior<MarketBehavior>();
        var grain = market.Commodities.Single(entry => entry.Commodity.Name == "Grain");
        game.PlayerShip.ChangeCargo(grain.Commodity, 150);
        var selection = new MarketTradeSelection();

        selection.SelectSell(game.PlayerShip, grain, int.MaxValue);
        selection.Commit(game.PlayerShip, market);

        Assert.That(game.PlayerShip.GetCargoAmount(grain.Commodity), Is.EqualTo(50));
        Assert.That(grain.CurrentAmount, Is.EqualTo(200));
        Assert.That(game.PlayerShip.CurrentGold, Is.EqualTo(11000));
    }

    [Test]
    public void EconomyUpdatesStockAndPriceBeforeTheNewDaySimulation()
    {
        var definition = WorldDefinition.CreateDefault();
        ConfigureImmediateDeterministicPrices(definition);
        var grainDefinition = definition.Entities
            .Single(entity => entity.Id == "location_klaipeda")
            .Behaviors.MarketBehavior.Commodities
            .Single(commodity => commodity.CommodityName == "Grain");
        grainDefinition.Consumption = 75;
        grainDefinition.Production = 0;
        var game = GameFactory.Create(definition);
        var grain = game.Entities
            .Single(entity => entity.Id == "location_klaipeda")
            .GetBehavior<MarketBehavior>().Commodities
            .Single(commodity => commodity.Commodity.Name == "Grain");

        game.Time.AdvanceDay();

        Assert.That(grain.CurrentAmount, Is.EqualTo(25));
        Assert.That(grain.CurrentPrice, Is.EqualTo(200));
    }

    [Test]
    public void EconomyCapsMaximumStockAndUsesDesperateSellingPrice()
    {
        var definition = WorldDefinition.CreateDefault();
        ConfigureImmediateDeterministicPrices(definition);
        var grainDefinition = definition.Entities
            .Single(entity => entity.Id == "location_klaipeda")
            .Behaviors.MarketBehavior.Commodities
            .Single(commodity => commodity.CommodityName == "Grain");
        grainDefinition.Consumption = 0;
        grainDefinition.Production = 500;
        var game = GameFactory.Create(definition);
        var grain = game.Entities
            .Single(entity => entity.Id == "location_klaipeda")
            .GetBehavior<MarketBehavior>().Commodities
            .Single(commodity => commodity.Commodity.Name == "Grain");

        game.Time.AdvanceDay();

        Assert.That(grain.CurrentAmount, Is.EqualTo(200));
        Assert.That(grain.CurrentPrice, Is.EqualTo(50));
    }

    [Test]
    public void IntradayTradeDoesNotRecalculatePriceUntilNextDay()
    {
        var definition = WorldDefinition.CreateDefault();
        ConfigureImmediateDeterministicPrices(definition);
        definition.Entities
            .Single(entity => entity.Behaviors.PlayerControlledBehavior != null)
            .Behaviors.TransportBehavior.CurrentGold = 100000;
        var game = GameFactory.Create(definition);
        var market = game.Entities
            .Single(entity => entity.Id == "location_klaipeda")
            .GetBehavior<MarketBehavior>();
        var grain = market.Commodities.Single(
            commodity => commodity.Commodity.Name == "Grain");
        var selection = new MarketTradeSelection();

        selection.SelectBuy(game.PlayerShip, grain, 50);
        selection.Commit(game.PlayerShip, market);

        Assert.That(grain.CurrentAmount, Is.EqualTo(50));
        Assert.That(grain.CurrentPrice, Is.EqualTo(100));

        game.Time.AdvanceDay();

        Assert.That(grain.CurrentPrice, Is.EqualTo(200));
    }

    [Test]
    public void EconomyPriceCurveGetsSteeperTowardMinimum()
    {
        var commodity = new Commodity("Test", 100, "tonnes", "t");
        var marketCommodity = new MarketCommodity(
            commodity, 100, 200f, 50f, 0, 0, 1f);
        marketCommodity.CurrentAmount = 75;

        Assert.That(EconomyManager.CalculatePrice(marketCommodity), Is.EqualTo(114));
    }

    [Test]
    public void EconomyMovesGraduallyTowardDesiredPrice()
    {
        var definition = WorldDefinition.CreateDefault();
        definition.Economy.DailyPriceAdjustmentRate = 0.2f;
        definition.Economy.MinimumDailyPriceAdjustment = 1;
        definition.Economy.RandomPriceFluctuationPercentage = 0f;
        var grainDefinition = definition.Entities
            .Single(entity => entity.Id == "location_klaipeda")
            .Behaviors.MarketBehavior.Commodities
            .Single(commodity => commodity.CommodityName == "Grain");
        grainDefinition.Consumption = 75;
        grainDefinition.Production = 0;
        var game = GameFactory.Create(definition);
        var grain = game.Entities
            .Single(entity => entity.Id == "location_klaipeda")
            .GetBehavior<MarketBehavior>().Commodities
            .Single(commodity => commodity.Commodity.Name == "Grain");

        game.Time.AdvanceDay();

        Assert.That(grain.CurrentPrice, Is.EqualTo(120));
    }

    [Test]
    public void EconomyRandomFluctuationStaysWithinConfiguredRange()
    {
        var commodity = new Commodity("Test", 100, "tonnes", "t");
        var marketCommodity = new MarketCommodity(
            commodity, 100, 200f, 50f, 0, 0, 1f);
        var market = new MarketBehavior("Market", new[] { marketCommodity });
        var economyDefinition = new EconomySystemDefinition
        {
            DailyPriceAdjustmentRate = 1f,
            MinimumDailyPriceAdjustment = 0,
            RandomPriceFluctuationPercentage = 5f
        };
        var economy = new EconomyManager(
            new[] { market }, economyDefinition, new Random(1234));

        economy.ProcessDay();

        Assert.That(marketCommodity.CurrentPrice, Is.InRange(95, 105));
    }

    [Test]
    public void MarketUsesConfiguredBuySellSpread()
    {
        var definition = WorldDefinition.CreateDefault();
        definition.Economy.BuySellSpreadPercentage = 5f;
        var game = GameFactory.Create(definition);
        var market = game.Entities
            .Single(entity => entity.Id == "location_klaipeda")
            .GetBehavior<MarketBehavior>();
        var grain = market.Commodities.Single(
            commodity => commodity.Commodity.Name == "Grain");

        Assert.That(grain.BuyPrice, Is.EqualTo(105));
        Assert.That(grain.SellPrice, Is.EqualTo(95));
    }

    [Test]
    public void CargoSectionsShareOneCapacityAndMarketsUseWares()
    {
        var game = GameFactory.Create();
        var grain = game.Entities
            .Single(entity => entity.Id == "location_klaipeda")
            .GetBehavior<MarketBehavior>().Commodities
            .Single(commodity => commodity.Commodity.Name == "Grain")
            .Commodity;
        game.PlayerShip.Supplies.Add(
            new CargoItemStack(new TestCargoItem("Water", "barrels", "bbl"), 30));
        game.PlayerShip.Restricted.Add(
            new CargoItemStack(grain, 20));

        game.PlayerShip.ChangeCargo(grain, 150);

        Assert.That(game.PlayerShip.CurrentCargoAmount, Is.EqualTo(200));
        Assert.That(game.PlayerShip.Wares.Single().Amount, Is.EqualTo(150));
        Assert.That(game.PlayerShip.Supplies.Single().Amount, Is.EqualTo(30));
        Assert.That(game.PlayerShip.Restricted.Single().Amount, Is.EqualTo(20));
        Assert.Throws<InvalidOperationException>(
            () => game.PlayerShip.ChangeCargo(grain, 1));
    }

    [TestCase(-0.1f, 5f, 5f)]
    [TestCase(0.2f, -1f, 5f)]
    [TestCase(0.2f, 5f, 100f)]
    public void ValidationRejectsInvalidEconomyPricingSettings(
        float adjustmentRate,
        float fluctuation,
        float spread)
    {
        var definition = WorldDefinition.CreateDefault();
        definition.Economy.DailyPriceAdjustmentRate = adjustmentRate;
        definition.Economy.RandomPriceFluctuationPercentage = fluctuation;
        definition.Economy.BuySellSpreadPercentage = spread;

        Assert.Throws<InvalidOperationException>(
            () => GameFactory.Create(definition));
    }

    [TestCase(90f, 200f)]
    [TestCase(50f, 110f)]
    public void ValidationRejectsMarketLimitsInsideNormalPriceRange(
        float minimum,
        float maximum)
    {
        var definition = WorldDefinition.CreateDefault();
        var commodity = definition.Entities
            .Single(entity => entity.Id == "location_klaipeda")
            .Behaviors.MarketBehavior.Commodities[0];
        commodity.MinAmountPercentage = minimum;
        commodity.MaxAmountPercentage = maximum;

        AssertValidationFailure(definition, "outside the 90-110");
    }

    private static WorldDefinition CreateMinimalDefinition()
    {
        return new WorldDefinition
        {
            Nodes = new List<WorldNodeDefinition>
            {
                new WorldNodeDefinition("node_a", "A", WorldNodeType.Location, 0f, 0f),
                new WorldNodeDefinition("node_b", "B", WorldNodeType.Location, 1f, 1f)
            },
            Edges = new List<WorldEdgeDefinition>
            {
                new WorldEdgeDefinition("edge_ab", "node_a", "node_b", 10f)
            },
            Entities = new List<EntityDefinition>
            {
                CreatePlayer("player_ship", "node_a", "icons.ship"),
                CreateLocation("location_b", "B", "node_b", "icons.location")
            }
        };
    }

    private static void ConfigureImmediateDeterministicPrices(
        WorldDefinition definition)
    {
        definition.Economy.DailyPriceAdjustmentRate = 1f;
        definition.Economy.MinimumDailyPriceAdjustment = 0;
        definition.Economy.RandomPriceFluctuationPercentage = 0f;
        definition.Economy.BuySellSpreadPercentage = 0f;
    }

    private sealed class TestCargoItem : ICargoItem
    {
        public string Name { get; }
        public string UnitName { get; }
        public string UnitAbbreviation { get; }
        public string IconSprite => null;

        public TestCargoItem(
            string name,
            string unitName,
            string unitAbbreviation)
        {
            Name = name;
            UnitName = unitName;
            UnitAbbreviation = unitAbbreviation;
        }
    }

    private static EntityDefinition CreatePlayer(string id, string nodeId, string sprite) =>
        new PlayerShipDefinition(id, "Ship", 25f, nodeId, sprite);

    private static EntityDefinition CreateLocation(string id, string name, string nodeId, string sprite) =>
        new EntityDefinition(id, name, new EntityBehaviorsDefinition
        {
            LocationBehavior = new LocationBehaviorDefinition("img-location"),
            DrawableBehavior = new DrawableBehaviorDefinition(sprite),
            WorldEntityBehavior = new WorldEntityBehaviorDefinition(nodeId)
        });

    private static void AssertValidationFailure(WorldDefinition definition, string message)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => GameFactory.Create(definition));
        StringAssert.Contains(message, exception.Message);
    }

    private static void AssertLocation(
        GameContext game, string id, string nodeId, string sprite, string locationViewSprite)
    {
        var location = game.Entities.Single(entity => entity.Id == id);
        Assert.That(location.HasBehavior<LocationBehavior>(), Is.True);
        Assert.That(location.GetBehavior<WorldEntityBehavior>().StartingNodeId, Is.EqualTo(nodeId));
        Assert.That(location.GetBehavior<DrawableBehavior>().MapIconSprite, Is.EqualTo(sprite));
        Assert.That(location.GetBehavior<LocationBehavior>().LocationViewSprite, Is.EqualTo(locationViewSprite));
    }

    private static void AssertMarket(GameContext game, string entityId, params string[] commodities)
    {
        var market = game.Entities
            .Single(entity => entity.Id == entityId)
            .GetBehavior<MarketBehavior>();
        Assert.That(market.Title, Is.EqualTo("Market"));
        Assert.That(market.Commodities.Select(entry => entry.Commodity.Name),
            Is.EqualTo(commodities));
        foreach (var entry in market.Commodities)
        {
            Assert.That(entry.TargetAmount, Is.EqualTo(100));
            Assert.That(entry.MaxAmountPercentage, Is.EqualTo(200f));
            Assert.That(entry.MinAmountPercentage, Is.EqualTo(50f));
            Assert.That(entry.Consumption, Is.EqualTo(25));
            Assert.That(entry.Production, Is.EqualTo(25));
            Assert.That(entry.NormalPriceCoefficient, Is.EqualTo(1f));
        }
    }

    private static void AssertNode(WorldNode node, string displayName, float mapX, float mapY)
    {
        Assert.That(node.DisplayName, Is.EqualTo(displayName));
        Assert.That(node.Type, Is.EqualTo(WorldNodeType.Location));
        Assert.That(node.MapX, Is.EqualTo(mapX));
        Assert.That(node.MapY, Is.EqualTo(mapY));
        Assert.That(node.IsDiscovered, Is.True);
    }

    private sealed class SpriteNames : ISpriteNameLookup
    {
        private readonly HashSet<string> names;
        public SpriteNames(params string[] names) => this.names = new HashSet<string>(names, StringComparer.Ordinal);
        public bool ContainsSprite(string name) => names.Contains(name);
    }
}
