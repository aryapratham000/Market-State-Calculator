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

        [InputParameter("Neutral Threshold", 80)]
        private const double NeutralThreshold = 0.08;

        [InputParameter("MiniEMA Double Threshold", 80)]
        private const double DoubleThreshold = 0.3;

        // EMA and ATR Indicators
        private Indicator ema8;
        private Indicator ema50;
        private Indicator ema200;
        private Indicator atr;

        public Market_State_Calculator()
            : base()
        {
            this.Name = "Market State Calculator";
            this.Description = "Calculates the market state score based on EMA slopes";

            this.AddLineSeries("Market State Score", Color.Blue, 2, LineStyle.Solid);

            this.SeparateWindow = true;
        }

        protected override void OnInit()
        {
            // Initialize the EMA indicators
            this.ema8 = Core.Indicators.BuiltIn.EMA(this.miniEmaLength, PriceType.Close);
            this.ema50 = Core.Indicators.BuiltIn.EMA(this.fastEmaLength, PriceType.Close);
            this.ema200 = Core.Indicators.BuiltIn.EMA(this.slowEmaLength, PriceType.Close);
            this.atr = Core.Indicators.BuiltIn.ATR(this.AtrLength, MaMode.SMA);

            this.AddIndicator(this.ema8);
            this.AddIndicator(this.ema50);
            this.AddIndicator(this.ema200);
            this.AddIndicator(this.atr);
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (this.Count <= Math.Max(this.SlopePeriodMini, Math.Max(this.SlopePeriodFast, this.SlopePeriodSlow)))
                return;

            double slope8EMA = CalculateSlope(this.ema8, this.SlopePeriodMini);
            double slope50EMA = CalculateSlope(this.ema50, this.SlopePeriodFast);
            double slope200EMA = CalculateSlope(this.ema200, this.SlopePeriodSlow);

            // Calculate scores for each EMA slope
            int scoreMiniEMA = slope8EMA > DoubleThreshold ? 2 : slope8EMA < -DoubleThreshold ? -2 : slope8EMA > NeutralThreshold ? 1 : slope8EMA < -NeutralThreshold ? -1 : 0;
            int scoreFastEMA = slope50EMA > NeutralThreshold ? 1 : slope50EMA < -NeutralThreshold ? -1 : 0;
            int scoreSlowEMA = slope200EMA > NeutralThreshold ? 1 : slope200EMA < -NeutralThreshold ? -1 : 0;

            // Calculate the total market state score
            int marketStateScore = scoreMiniEMA + scoreFastEMA + scoreSlowEMA;

            // Plot the market state score
            this.SetValue(marketStateScore, 0);
        }

        private double CalculateSlope(Indicator ema, int period)
        {
            double currentEmaValue = ema.GetValue(0);
            double previousEmaValue = ema.GetValue(period);

            return (currentEmaValue - previousEmaValue) / period;
        }
    }
}
