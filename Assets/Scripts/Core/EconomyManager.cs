using System;
using System.Collections.Generic;
using System.Linq;
using TalesOfTheBrave.Simulation.Entities;
using TalesOfTheBrave.Simulation.Rulesets;

namespace TalesOfTheBrave.Simulation.Economy
{
    public sealed class EconomyManager
    {
        private readonly IReadOnlyList<MarketBehavior> markets;
        private readonly EconomySystemDefinition definition;
        private readonly Random random;

        public EconomyManager(
            IEnumerable<MarketBehavior> markets,
            EconomySystemDefinition definition = null,
            Random random = null)
        {
            this.markets = (markets ?? throw new ArgumentNullException(nameof(markets)))
                .ToArray();
            this.definition = definition ?? new EconomySystemDefinition();
            this.random = random ?? new Random();
        }

        public void ProcessDay()
        {
            foreach (var market in markets)
            foreach (var commodity in market.Commodities)
            {
                var maximumAmount = (int)Math.Floor(
                    commodity.TargetAmount *
                    commodity.MaxAmountPercentage / 100f);
                commodity.CurrentAmount = Math.Max(
                    0,
                    Math.Min(
                        maximumAmount,
                        commodity.CurrentAmount -
                        commodity.Consumption +
                        commodity.Production));
                var desiredPrice = CalculatePrice(commodity);
                var gap = desiredPrice - commodity.CurrentPrice;
                var adjustedPrice = commodity.CurrentPrice;
                if (gap != 0)
                {
                    var step = Math.Max(
                        definition.MinimumDailyPriceAdjustment,
                        (int)Math.Ceiling(
                            Math.Abs(gap) * definition.DailyPriceAdjustmentRate));
                    adjustedPrice +=
                        Math.Sign(gap) * Math.Min(Math.Abs(gap), step);
                }
                var fluctuationRange = (int)Math.Round(
                    adjustedPrice *
                    definition.RandomPriceFluctuationPercentage / 100f,
                    MidpointRounding.AwayFromZero);
                var fluctuation = fluctuationRange == 0
                    ? 0
                    : random.Next(-fluctuationRange, fluctuationRange + 1);
                commodity.CurrentPrice = Math.Max(0, adjustedPrice + fluctuation);
            }
        }

        public static int CalculatePrice(MarketCommodity commodity)
        {
            if (commodity == null) throw new ArgumentNullException(nameof(commodity));
            var basePrice = commodity.Commodity.DefaultPrice *
                            commodity.NormalPriceCoefficient;
            var percentage = commodity.TargetAmount <= 0
                ? 100f
                : commodity.CurrentAmount * 100f / commodity.TargetAmount;
            float priceMultiplier;
            if (percentage <= commodity.MinAmountPercentage)
            {
                priceMultiplier = 2f;
            }
            else if (percentage < 90f)
            {
                var deviation = (90f - percentage) /
                                (90f - commodity.MinAmountPercentage);
                priceMultiplier = 1f + deviation * deviation;
            }
            else if (percentage <= 110f)
            {
                priceMultiplier = 1f;
            }
            else if (percentage < commodity.MaxAmountPercentage)
            {
                var deviation = (percentage - 110f) /
                                (commodity.MaxAmountPercentage - 110f);
                priceMultiplier = 1f - 0.5f * deviation * deviation;
            }
            else
            {
                priceMultiplier = 0.5f;
            }

            return Math.Max(
                0,
                (int)Math.Round(
                    basePrice * priceMultiplier,
                    MidpointRounding.AwayFromZero));
        }
    }
}
