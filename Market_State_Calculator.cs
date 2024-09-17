// Custom Market State Score Indicator on quantower

using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace Market_State_Calculator
{
    public sealed class Market_State_Calculator : Indicator
    {
        // Input parameters for EMAs
        [InputParameter("Mini EMA Length", 10)]
        public int miniEmaLength = 8;

        [InputParameter("Fast EMA Length", 20)]
        public int fastEmaLength = 50;

        [InputParameter("Slow EMA Length", 30)]
        public int slowEmaLength = 200;

        [InputParameter("ATR Length", 40)]
        public int AtrLength = 14;

        // Input parameters for slope periods
        [InputParameter("Slope Period for mini EMA", 50)]
        public int SlopePeriodMini = 8;

        [InputParameter("Slope Period for fast EMA", 60)]
        public int SlopePeriodFast = 20;

        [InputParameter("Slope Period for slow EMA", 70)]
        public int SlopePeriodSlow = 100;

        // Lookback period for calculating dynamic threshold
        [InputParameter("Lookback Period for Slope Range Calculation", 80)]
        public int lookbackPeriod = 2000;

        // Smoothing period for market state score
        [InputParameter("Smoothing Period", 90)]
        public int smoothingPeriod = 5;

        // EMA and ATR Indicators
        private Indicator emaMini;
        private Indicator emaFast;
        private Indicator emaSlow;
        private Indicator atr;

        private double previousSmoothedScore = double.NaN;

        public Market_State_Calculator()
            : base()
        {
            this.Name = "Market State Calculator";
            this.Description = "Calculates the market state score based on EMA slopes";

            // Initialize three different line series with different colors
            this.AddLineSeries("Market State Score Green", Color.Green, 2, LineStyle.Solid);
            this.AddLineSeries("Market State Score Red", Color.Red, 2, LineStyle.Solid);
            this.AddLineSeries("Market State Score Blue", Color.Blue, 2, LineStyle.Solid);

            this.SeparateWindow = true;
        }

        protected override void OnInit()
        {
            // Initialize the EMA indicators
            this.emaMini = Core.Indicators.BuiltIn.EMA(this.miniEmaLength, PriceType.Close);
            this.emaFast = Core.Indicators.BuiltIn.EMA(this.fastEmaLength, PriceType.Close);
            this.emaSlow = Core.Indicators.BuiltIn.EMA(this.slowEmaLength, PriceType.Close);
            this.atr = Core.Indicators.BuiltIn.ATR(this.AtrLength, MaMode.SMA);

            this.AddIndicator(this.emaMini);
            this.AddIndicator(this.emaFast);
            this.AddIndicator(this.emaSlow);
            this.AddIndicator(this.atr);
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (this.Count <= Math.Max(this.SlopePeriodMini, Math.Max(this.SlopePeriodFast, this.SlopePeriodSlow)))
                return;

            double slopeEmaMini = CalculateSlope(this.emaMini, this.SlopePeriodMini);
            double slopeEmaFast = CalculateSlope(this.emaFast, this.SlopePeriodFast);
            double slopeEmaSlow = CalculateSlope(this.emaSlow, this.SlopePeriodSlow);

            // Calculate the range of slopes over the lookback period
            double slopeRangeMini = CalculateSlopeRange(this.emaMini, this.SlopePeriodMini, this.lookbackPeriod);
            double slopeRangeFast = CalculateSlopeRange(this.emaFast, this.SlopePeriodFast, this.lookbackPeriod);
            double slopeRangeSlow = CalculateSlopeRange(this.emaSlow, this.SlopePeriodSlow, this.lookbackPeriod);

            // Define the dynamic thresholds
            double neutralThresholdMini = slopeRangeMini * 0.05;
            double neutralThresholdFast = slopeRangeFast * 0.05;
            double neutralThresholdSlow = slopeRangeSlow * 0.05;
            double doubleThreshold = slopeRangeMini * 0.3;

            // Calculate scores for each EMA slope using dynamic thresholds
            int scoreMiniEMA = slopeEmaMini > doubleThreshold ? 2 : slopeEmaMini < -doubleThreshold ? -2 : slopeEmaMini > neutralThresholdMini ? 1 : slopeEmaMini < -neutralThresholdMini ? -1 : 0;
            int scoreFastEMA = slopeEmaFast > neutralThresholdFast ? 1 : slopeEmaFast < -neutralThresholdFast ? -1 : 0;
            int scoreSlowEMA = slopeEmaSlow > neutralThresholdSlow ? 1 : slopeEmaSlow < -neutralThresholdSlow ? -1 : 0;

            // Calculate the total market state score
            int marketStateScore = scoreMiniEMA + scoreFastEMA + scoreSlowEMA;

            // Apply smoothing to the market state score using custom EMA
            double smoothedMarketStateScore = CalculateCustomEMA(marketStateScore, previousSmoothedScore, this.smoothingPeriod);
            previousSmoothedScore = smoothedMarketStateScore;

            // Determine which line series to update based on the smoothedMarketStateScore
            if (smoothedMarketStateScore >= 2)
            {
                this.SetValue(smoothedMarketStateScore, 0); // Green series
                this.SetValue(double.NaN, 1); // Red series
                this.SetValue(double.NaN, 2); // Blue series
            }
            else if (smoothedMarketStateScore <= -2)
            {
                this.SetValue(double.NaN, 0); // Green series
                this.SetValue(smoothedMarketStateScore, 1); // Red series
                this.SetValue(double.NaN, 2); // Blue series
            }
            else
            {
                this.SetValue(double.NaN, 0); // Green series
                this.SetValue(double.NaN, 1); // Red series
                this.SetValue(smoothedMarketStateScore, 2); // Blue series
            }
        }

        private double CalculateSlope(Indicator ema, int period)
        {
            double currentEmaValue = ema.GetValue(0);
            double previousEmaValue = ema.GetValue(period);

            return (currentEmaValue - previousEmaValue) / period;
        }

        private double CalculateSlopeRange(Indicator ema, int period, int lookback)
        {
            double highestSlope = 0;
            double lowestSlope = 0; 

            for (int i = 0; i <= lookback; i++)
            {
                // Calculate the slope for the EMA at the current lookback position
                double iCurrentEma = ema.GetValue(i); // EMA at current lookback position
                double iPreviousEma = ema.GetValue(i + period); // EMA at (current + period) lookback position
                double slope = (iCurrentEma - iPreviousEma) / period;

                if (slope > highestSlope)
                    highestSlope = slope;
                if (slope < lowestSlope)
                    lowestSlope = slope;
            }

            return highestSlope - lowestSlope;
        }

        private double CalculateCustomEMA(double currentValue, double previousValue, int period)
        {
            if (double.IsNaN(previousValue))
                return currentValue;

            double alpha = 2.0 / (period + 1);
            return alpha * currentValue + (1 - alpha) * previousValue;
        }
    }
}



