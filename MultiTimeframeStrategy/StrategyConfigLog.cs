using System;

namespace cAlgo.Robots
{
    public class StrategyConfigLog
    {
        public string Event { get; set; }

        public string StrategyVersion { get; set; }

        public DateTime CreatedAt { get; set; }

        public int EntryMode { get; set; }

        public int MinConfirmations { get; set; }

        public int MinDistance { get; set; }

        public double RiskPercent { get; set; }

        public double MinSL { get; set; }

        public double MaxSL { get; set; }

        public double PartialTP { get; set; }

        public bool UseTrailingStop { get; set; }

         public double BreakoutBuffer { get; set; }

        public double MaxEMAExtension { get; set; }

        public int MaxContinuationLegs { get; set; }
    }
}