using System;

namespace cAlgo.Robots
{
    public class BacktestSummaryLog
    {
        public string Event { get; set; }

        public string BacktestRunId { get; set; }

        public string BotName { get; set; }

        public string StrategyVersion { get; set; }

        public DateTime TimestampUtc { get; set; }

        public DateTime LocalTimestamp { get; set; }

        // =====================================
        // OVERALL TRADE STATS
        // =====================================

        public int TotalTrades { get; set; }

        public int Wins { get; set; }

        public int Losses { get; set; }

        public double WinRate { get; set; }

        public double GrossProfit { get; set; }

        public double NetProfit { get; set; }

        public double ProfitFactor { get; set; }

        // =====================================
        // GRADE STATS
        // =====================================

        public int GradeAWins { get; set; }

        public int GradeALosses { get; set; }

        public int GradeBWins { get; set; }

        public int GradeBLosses { get; set; }

        public int GradeCWins { get; set; }

        public int GradeCLosses { get; set; }

        // =====================================
        // REGIME STATS
        // =====================================

        public int TrendWins { get; set; }

        public int TrendLosses { get; set; }

        public int ChopWins { get; set; }

        public int ChopLosses { get; set; }

        public int TransitionWins { get; set; }

        public int TransitionLosses { get; set; }

        // =====================================
        // FILTER STATS
        // =====================================

        public int TotalBars { get; set; }

        public int ExistingTradesBlocked { get; set; }

        public int ChoppyBlocked { get; set; }

        public int TrendBlocked { get; set; }

        public int EMABlocked { get; set; }

        public int ConfirmRejects { get; set; }

        public int OBRejects { get; set; }

        public int FVGRejects { get; set; }

        public int MomentumRejects { get; set; }

        public int SLTooSmallRejects { get; set; }

        public int SLTooLargeRejects { get; set; }

        public int PassedFilters { get; set; }

        // =====================================
        // PULLBACK STATS
        // =====================================

        public int PullbackTotal { get; set; }

        public int StrongPullbacks { get; set; }

        public int WeakPullbacks { get; set; }

        public int MomentumPullbacks { get; set; }

        public int ShallowPullbackFails { get; set; }

        public int NoRetraceFails { get; set; }

        public int BlockedWeakPullbacks { get; set; }

        public double ValidPullbackPercent { get; set; }

        public double FailedPullbackPercent { get; set; }

        // =====================================
        // REJECT STATS
        // =====================================

        public int PullbackRejectWins { get; set; }

        public int PullbackRejectLosses { get; set; }

        public int ConfirmRejectWins { get; set; }

        public int ConfirmRejectLosses { get; set; }

        public int EMARejectWins { get; set; }

        public int EMARejectLosses { get; set; }

        // =====================================
        // CREATED
        // =====================================

        public DateTime CreatedAt { get; set; }
    }
}