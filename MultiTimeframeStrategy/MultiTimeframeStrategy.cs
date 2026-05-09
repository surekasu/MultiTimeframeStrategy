using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using System;
using System.Linq;
using System.Collections.Generic;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class MTF_Pro_Bot_V4 : Robot    {
        private const string BotLabel = "MTF V4";

        private class RejectedTrade
        {
            public string Reason;
            public string Trend;
            public double Entry;
            public double TP;
            public double SL;
            public int BarsLeft;
        }
        private List<RejectedTrade> rejectedTrades = new List<RejectedTrade>();

        private int pullbackRejectWins = 0;
        private int pullbackRejectLoss = 0;

        private int confirmRejectWins = 0;
        private int confirmRejectLoss = 0;

        private int emaRejectWins = 0;
        private int emaRejectLoss = 0;

        private int dailyLossCount = 0;
        private DateTime currentDay;

        private int blockedChoppy = 0;
        private int blockedTrend = 0;
        private int blockedPullback = 0;
        private int blockedConfirmation = 0;
        private int blockedEMA = 0;
        private int blockedSR = 0;
        private int blockedExistingTrade = 0;
        private int totalBarsChecked = 0;
        private int blockedSLTooSmall = 0;
        private int blockedSLTooLarge = 0;
        private HashSet<long> partialClosed = new HashSet<long>();

        private Bars h4Bars;
        private Bars h1Bars;
        private Bars m15Bars;

        private ExponentialMovingAverage ema21_H1;
        private ExponentialMovingAverage ema50_H1;
        private ExponentialMovingAverage ema200_H1;
        private ExponentialMovingAverage ema200_H4;
        private ExponentialMovingAverage ema50_H4;
        private ExponentialMovingAverage ema21_M15;
        private ExponentialMovingAverage ema50_M15;

        private AverageTrueRange atr_M15;

        private bool readyToBuy = false;
        private bool readyToSell = false;
        private double triggerHigh = 0;
        private double triggerLow = 0;
        private int armedBars = 0;
        
        public enum PullbackResult
        {
            Valid_Strong,
            Valid_Weak,
            Valid_Momentum,
            TooShallow,
            NoRetrace
        }

        int pbTooShallow = 0;
        int pbStrong = 0;
        int pbWeak = 0;
        int pbMomentum = 0;
        int pbNoRetrace = 0;
        int pbTotal = 0;

        int confirmFailOB = 0;
        int confirmFailFVG = 0;
        int confirmFailMomentum = 0;
        int confirmPass = 0;
        string currentTradeGrade = "";
        string currentTradeTrend ="";
        string currentPullbackResult = "";
        int currentConfirmScore = 0;
        int currentRequiredScore = 0;
        private int blockedWeakPullbacks = 0;
        //Wins and Losses
        private int totalTrades = 0;
        private int totalWins = 0;
        private int totalLosses = 0;

        private int aWins = 0;
        private int aLosses = 0;

        private int bWins = 0;
        private int bLosses = 0;

        private int cWins = 0;
        private int cLosses = 0;

        //Regime counters
        private int trendWins = 0;
        private int trendLosses = 0;

        private int chopWins = 0;
        private int chopLosses = 0;

        private int transitionWins = 0;
        private int transitionLosses = 0;
        string currentRegime = "Unknown";

        private int totalBreakevens = 0;

        //Session stats
        // Asian
        private int asianWins = 0;
        private int asianLosses = 0;
        private int asianBE = 0;

        // London
        private int londonWins = 0;
        private int londonLosses = 0;
        private int londonBE = 0;

        // New York
        private int nyWins = 0;
        private int nyLosses = 0;
        private int nyBE = 0;

        private const string SessionAsian = "Asian";
        private const string SessionLondon = "London";
        private const string SessionNY = "NewYork";

        private Dictionary<long, string> tradeSessions =
        new Dictionary<long, string>();

        private Dictionary<long, string> tradeGrades =
        new Dictionary<long, string>();

        private Dictionary<long, string> tradeRegimes =
        new Dictionary<long, string>();

        [Parameter("Entry Mode", DefaultValue = 2)]
        public int EntryMode { get; set; }

        [Parameter("Min Confirmations", DefaultValue = 2)]
        public int MinConfirmations { get; set; }

        [Parameter("Min Distance to S/R (pips)", DefaultValue = 15)]
        public int MinSRDistance { get; set; }

        [Parameter("Volume (Lots)", DefaultValue = 0.1)]
        public double Volume { get; set; }

        [Parameter("Min SL (pips)", DefaultValue = 35)]
        public int MinSLPips { get; set; }

        [Parameter("Max SL (pips)", DefaultValue = 120)]
        public int MaxSLPips { get; set; }

        [Parameter("Partial TP (pips)", DefaultValue = 35)]
        public int PartialTP { get; set; }

        [Parameter("Trailing Stop (pips)", DefaultValue = 30)]
        public int TrailingStop { get; set; }

        [Parameter("Breakout Buffer (pips)", DefaultValue = 5)]
        public int BreakoutBuffer { get; set; }

        [Parameter("Enable Partial TP", DefaultValue = false)]
        public bool EnablePartialTP { get; set; }

        [Parameter("Risk % Per Trade", DefaultValue = 2.0)]
        public double RiskPercent { get; set; }

        protected override void OnStart()
        {
            h4Bars = MarketData.GetBars(TimeFrame.Hour4);
            h1Bars = MarketData.GetBars(TimeFrame.Hour);
            m15Bars = MarketData.GetBars(TimeFrame.Minute15);

            ema21_H1 = Indicators.ExponentialMovingAverage(h1Bars.ClosePrices, 21);
            ema50_H1 = Indicators.ExponentialMovingAverage(h1Bars.ClosePrices, 50);
            ema200_H1 = Indicators.ExponentialMovingAverage(h1Bars.ClosePrices, 200);
            ema50_H4 = Indicators.ExponentialMovingAverage(h4Bars.ClosePrices, 50);
            ema200_H4 = Indicators.ExponentialMovingAverage(h4Bars.ClosePrices, 200);
            ema21_M15 = Indicators.ExponentialMovingAverage(m15Bars.ClosePrices, 21);
            ema50_M15 = Indicators.ExponentialMovingAverage(m15Bars.ClosePrices, 50);

            atr_M15 = Indicators.AverageTrueRange(m15Bars, 14, MovingAverageType.Simple);

            currentDay = Server.Time.Date;
            Positions.Closed += OnPositionClosed;

            Print("MTF Pro V4 (SMC) Started");
            Print(
                "Server Time: {0} | UTC Time: {1}",
                Server.Time,
                Server.TimeInUtc
            );
        }

        private void TrackRejected(string reason, string trend)
        {
            double entry = trend == "bullish" ? Symbol.Ask : Symbol.Bid;

            double tp = trend == "bullish"
                ? entry + (80 * Symbol.PipSize)
                : entry - (80 * Symbol.PipSize);

            double sl = trend == "bullish"
                ? entry - (50 * Symbol.PipSize)
                : entry + (50 * Symbol.PipSize);

            rejectedTrades.Add(new RejectedTrade
            {
                Reason = reason,
                Trend = trend,
                Entry = entry,
                TP = tp,
                SL = sl,
                BarsLeft = 20
            });
        }

        private void UpdateRejectStats(string reason, bool win)
        {
            if (reason == "Pullback")
            {
                if (win) pullbackRejectWins++;
                else pullbackRejectLoss++;
            }

            if (reason == "Confirm")
            {
                if (win) confirmRejectWins++;
                else confirmRejectLoss++;
            }

            if (reason == "EMA")
            {
                if (win) emaRejectWins++;
                else emaRejectLoss++;
            }
        }

        private void EvaluateRejectedTrades()
        {
            foreach (var t in rejectedTrades.ToList())
            {
                double high = m15Bars.HighPrices.Last(1);
                double low = m15Bars.LowPrices.Last(1);

                bool win = false;
                bool lose = false;

                if (t.Trend == "bullish")
                {
                    if (low <= t.SL)
                        lose = true;
                    else if (high >= t.TP)
                        win = true;
                }
                else
                {
                    if (high >= t.SL)
                        lose = true;
                    else if (low <= t.TP)
                        win = true;
                }

                if (win || lose || t.BarsLeft <= 0)
                {
                    UpdateRejectStats(t.Reason, win);
                    rejectedTrades.Remove(t);
                }
                else
                {
                    t.BarsLeft--;
                }
            }
        }

        private void OnPositionClosed(PositionClosedEventArgs args)
        {
            var position = args.Position;

                Print(
                    "TRADE CLOSED | Type: {0} | Gross: {1:F2} | Net: {2:F2} | Pips: {3:F1}",
                    position.TradeType,
                    position.GrossProfit,
                    position.NetProfit,
                    position.Pips
                );


            if (position.SymbolName != SymbolName || position.Label != BotLabel)
                return;

            partialClosed.Remove(position.Id);


            double breakevenThreshold = 1.0;

            bool isBreakeven =
            Math.Abs(position.NetProfit) <= breakevenThreshold;

            bool isWin = position.NetProfit > breakevenThreshold;

            if (isBreakeven)
            {
                totalBreakevens++;

                Print("Breakeven trade recorded");

            }
            else if (isWin)
            {
                totalWins++;

                if (tradeGrades[position.Id] == "A")
                    aWins++;

                else if (tradeGrades[position.Id] == "B")
                    bWins++;

                else
                    cWins++;
            }
            else
            {
                totalLosses++;
                
                dailyLossCount++;
                Print("Loss recorded. Total losses today: ", dailyLossCount);

                if (tradeGrades[position.Id] == "A")
                    aLosses++;

                else if (tradeGrades[position.Id] == "B")
                    bLosses++;

                else
                    cLosses++;
            }
            

            // Regime
            if (tradeRegimes.ContainsKey(position.Id))
            {
                string regime = tradeRegimes[position.Id];

                if (regime == "Trending")
                {
                    if (isWin)
                        trendWins++;
                    else if (!isBreakeven)
                        trendLosses++;
                }
                else if (regime == "Choppy")
                {
                    if (isWin)
                        chopWins++;
                    else if (!isBreakeven)
                        chopLosses++;
                }
                else
                {
                    if (isWin)
                        transitionWins++;
                    else if (!isBreakeven)
                        transitionLosses++;
                }
            }

            if (tradeSessions.ContainsKey(position.Id))
            {

                string session = tradeSessions[position.Id];

                switch (session)
                {
                    case SessionAsian:

                        if (isBreakeven)
                            asianBE++;
                        else if (isWin)
                            asianWins++;
                        else
                            asianLosses++;

                        break;

                    case SessionLondon:

                        if (isBreakeven)
                            londonBE++;
                        else if (isWin)
                            londonWins++;
                        else
                            londonLosses++;

                        break;

                    case SessionNY:

                        if (isBreakeven)
                            nyBE++;
                        else if (isWin)
                            nyWins++;
                        else
                            nyLosses++;

                        break;
                }
            }
            tradeGrades.Remove(position.Id);
            tradeRegimes.Remove(position.Id);
            tradeSessions.Remove(position.Id);

        }

        protected override void OnBar()
        {
            totalBarsChecked++;

            // ===============================
            // BAR SAFETY CHECK
            // ===============================
            if (m15Bars.Count < 10)
                return;

            string status = "";
            string trend = "";
            int confirmationScore = 0;

            EvaluateRejectedTrades();
            // ===============================
            // FILTER STATS EVERY 5 BARS
            // ===============================
            if (totalBarsChecked % 10 == 0)
            {
                double pbValidPercent = pbTotal > 0
                    ? (pbStrong + pbWeak + pbMomentum) * 100.0 / pbTotal
                    : 0;

                double pbfailPercent = pbTotal > 0
                    ? (pbTooShallow + pbNoRetrace) * 100.0 / pbTotal
                    : 0;
                
                Print(
                    "***FILTER STATS*** Bars: {0} | Existing: {1} | Choppy: {2} | Trend: {3} | EMA: {4} | S/R: {5} | " +
                    "Confirm Fail ({6}) | OB ({7}) FVG ({8}) Momentum ({9}) | " +
                    "SL Small ({10}) SL Large ({11}) | Passed ({12}) | " +
                    "Pullback Total ({13}) | Strong ({14}) Weak ({15}) Momentum ({16}) | " +
                    "Fail: Shallow ({17}) NoRetrace ({18}) | blockedWeakPullbacks ({19}) | Valid Pullback %: {20:F1} | failed Pullback %: {21:F1}",

                    totalBarsChecked,
                    blockedExistingTrade,
                    blockedChoppy,
                    blockedTrend,
                    blockedEMA,
                    blockedSR,

                    blockedConfirmation,
                    confirmFailOB,
                    confirmFailFVG,
                    confirmFailMomentum,

                    blockedSLTooSmall,
                    blockedSLTooLarge,

                    confirmPass,

                    pbTotal,
                    pbStrong,
                    pbWeak,
                    pbMomentum,
                    pbTooShallow,
                    pbNoRetrace,
                    blockedWeakPullbacks,
                    pbValidPercent,
                    pbfailPercent
                );
            
         
         // TRADE STATS
                double winRate = totalTrades > 0
                ? (double)totalWins / totalTrades * 100
                : 0;

                Print(
                    "***TRADE STATS*** Total: {0} | Wins: {1} | Losses: {2} | BE: {3} | Win Rate: {4:F1}% | " +
                    "A W/L: {5}/{6} | " +
                    "B W/L: {7}/{8} | " +
                    "C W/L: {9}/{10}",

                    totalTrades,
                    totalWins,
                    totalLosses,
                    totalBreakevens,
                    winRate,
                    aWins,
                    aLosses,

                    bWins,
                    bLosses,

                    cWins,
                    cLosses
                );

                //Regime Stats
                Print(
                    "***REGIME STATS*** " +
                    "Trend W/L: {0}/{1} | " +
                    "Chop W/L: {2}/{3} | " +
                    "Transition W/L: {4}/{5}",

                    trendWins,
                    trendLosses,

                    chopWins,
                    chopLosses,

                    transitionWins,
                    transitionLosses
                );

        //  REJECT STATS
                Print("***REJECT STATS*** Pullback Reject Wins: ", pullbackRejectWins,
                      " | Loss: ", pullbackRejectLoss,
                      " | Confirm Reject Wins: ", confirmRejectWins,
                      " | Loss: ", confirmRejectLoss,
                      " | EMA Reject Wins: ", emaRejectWins,
                     " | Loss: ", emaRejectLoss);  
            
                Print(
                    "***SESSION STATS*** Asian W/L/BE: {0}/{1}/{2} | London: {3}/{4}/{5} | NY: {6}/{7}/{8}",
                    asianWins,
                    asianLosses,
                    asianBE,
                    londonWins,
                    londonLosses,
                    londonBE,
                    nyWins,
                    nyLosses,
                    nyBE
                );

            }

            // ===============================
            // RESET DAILY LOSS
            // ===============================
            if (Server.Time.Date > currentDay)
            {
                currentDay = Server.Time.Date;
                dailyLossCount = 0;
            }

            //if (!IsTradingSession())
            //{
            //    PrintSummary("Blocked: Session", trend, confirmationScore);
            //    return;
            //}


            // ===============================
            // DAILY LOSS LIMIT
            // ===============================
            if (dailyLossCount >= 3)
            {
                status = "Blocked: Daily Loss Limit";
                PrintSummary(status, trend, confirmationScore);
                return;
            }

            // ===============================
            // TIMEFRAME CHECK
            // ===============================
            if (Bars.TimeFrame != TimeFrame.Minute15)
                return;

            armedBars++;

            if (armedBars >= 5)
            {
                readyToBuy = false;
                readyToSell = false;
            }

            // ===============================
            // EXISTING TRADE CHECK
            // ===============================
            if (Positions.Any(p => p.SymbolName == SymbolName && p.Label == BotLabel))
            {
                blockedExistingTrade++;
                status = "Blocked: Existing Trade";
                PrintSummary(status, trend, confirmationScore);
                return;
            }

            // ===============================    
            // CHOPPY MARKET CHECK
            // ===============================
            if (IsChoppyMarket())
            {
                blockedChoppy++;
                PrintSummary("Blocked: Choppy", trend, confirmationScore);
                return;
            }

            
            // ===============================
            // TREND CHECK
            // ===============================
            string h4Trend = GetH4Trend();
            string h1Trend = GetH1Trend();
            string executionTrend = h1Trend;

            // If H4 unclear, use H1 operationally
            if (h4Trend == "transitional" && h1Trend == "transitional")
            {
                blockedTrend++;
                status = "Blocked: Transitional Market";

                PrintSummary(status, executionTrend, confirmationScore);
                return;
            }
            
            // -----------------------------------
            // REGIME DETECTION
            // -----------------------------------
            currentRegime = "Unknown";

            if (IsChoppyMarket())
                currentRegime = "Choppy";
            else if (h1Trend == "bullish" || h1Trend == "bearish")
                currentRegime = "Trending";
            else
                currentRegime = "Transition";

            //Print("Current Regime: ", currentRegime);

            
            // ===============================
            // EMA CHECK
            // ===============================
            if (!IsEMAAligned(executionTrend))
            {
                blockedEMA++;
                status = "EMA Weak";
                //TrackRejected("EMA", trend);
                PrintSummary(status, executionTrend, confirmationScore);
                return;
            }

            int emaScore = GetEMAScore(executionTrend);
            confirmationScore += emaScore;

            //Print("EMA Score: ", emaScore);
            // ===============================
            
            // PULLBACK CHECK
            // ===============================
            PullbackResult pullbackResult;
            bool isPullback = IsM15Pullback(executionTrend, out pullbackResult);
            pbTotal++;
            if (!isPullback)
            {
                blockedPullback++;

                switch (pullbackResult)
                {
                    case PullbackResult.TooShallow:
                        pbTooShallow++;
                        status = "Fail: Too shallow";
                        break;

                    case PullbackResult.NoRetrace:
                        pbNoRetrace++;
                        status = "Fail: No retrace";
                        break;

                    default:
                        status = $"Fail: Unknown {pullbackResult}";
                        break;
                }

                TrackRejected("Pullback", executionTrend);
                PrintSummary(status, executionTrend, confirmationScore);
                return;
            }
            else
            {
                // ✅ VALID pullbacks — classify them properly
                switch (pullbackResult)
                {
                    case PullbackResult.Valid_Strong:
                        pbStrong++;
                        break;

                    case PullbackResult.Valid_Weak:
                        pbWeak++;
                        blockedWeakPullbacks++;
                        //Print("Weak pullback skipped");
                        TrackRejected("WeakPullback", executionTrend);
                        PrintSummary("Blocked: Weak Pullback", executionTrend, confirmationScore);
                        return;

                    case PullbackResult.Valid_Momentum:
                        pbMomentum++;
                        break;
                }

                Print($"Pullback VALID | Type: {pullbackResult} | Trend: {executionTrend}");
            }


            bool m15Aligned = IsM15TrendAligned(executionTrend);

            if (!m15Aligned && pullbackResult == PullbackResult.Valid_Momentum)
            {
                confirmationScore -= 1; // only small penalty
            }

            

            // ===============================
            // SUPPORT / RESISTANCE CHECK
            // ===============================
            //if (IsNearSupportResistance(trend))
            //{
            //    blockedSR++;
            //    status = "Blocked: Near S/R";
            //    PrintSummary(status, trend, confirmationScore);
            //    return;
            //}

            // ===============================
            // REQUIRED SCORE (DYNAMIC)
            // ===============================
            
            int requiredScore = MinConfirmations;

            switch (pullbackResult)
            {
                case PullbackResult.Valid_Strong:
                    requiredScore = Math.Max(requiredScore, 2);
                    break;

                case PullbackResult.Valid_Weak:
                    requiredScore = Math.Max(requiredScore, 2);
                    break;

                case PullbackResult.Valid_Momentum:
                    requiredScore = Math.Max(requiredScore, 3); // stricter
                    break;
            }
            // ===============================
            // CONFIRMATION SCORE
            // ===============================
            
            // Existing confirmations
            bool momentumCondition = IsMomentumStrong();
            bool obCondition = IsOrderBlockValid(executionTrend);
            bool fvgCondition = IsFVGPresent(executionTrend);
            
            // confirmations
            int confirmScore = 0;

            if (momentumCondition) confirmScore += 1;
            if (obCondition) confirmScore += 1;
            if (fvgCondition) confirmScore += 1;

           

            // boost weak setups slightly
            if (pullbackResult == PullbackResult.Valid_Weak && momentumCondition && m15Aligned)
            {
                confirmScore += 1;
            }
            
            confirmScore = Math.Min(confirmScore, 4);
            confirmationScore += confirmScore;
            
            
            if (confirmationScore >= requiredScore)
            {
                confirmPass++;
                //Print($"Confirm PASS | Trend: {executionTrend} | Score: {confirmationScore} | Required: {requiredScore} | OB: {obCondition} | FVG: {fvgCondition} | Momentum: {momentumCondition}");
            }
            else
            {
                blockedConfirmation++;

                if (!obCondition) confirmFailOB++;
                if (!fvgCondition) confirmFailFVG++;
                if (!momentumCondition) confirmFailMomentum++;

                string failReasons = "";

                if (!obCondition) failReasons += "OB ";
                if (!fvgCondition) failReasons += "FVG ";
                if (!momentumCondition) failReasons += "Momentum ";

                status = $"Fail: Confirm [{failReasons.Trim()}]";

                TrackRejected("Confirm", executionTrend);
                PrintSummary(status, executionTrend, confirmationScore);
                return;
            }
            
            // ===============================
            // TRADE GRADE
            // ===============================
            string tradeGrade = "C";
            bool htfAligned = h1Trend == h4Trend;
            int confirmCount =
                (momentumCondition ? 1 : 0) +
                (obCondition ? 1 : 0) +
                (fvgCondition ? 1 : 0);

            if (
                pullbackResult == PullbackResult.Valid_Strong &&
                momentumCondition &&
                (obCondition || fvgCondition) &&
                htfAligned &&
                IsEMAAligned(h1Trend)
            )
            {
                tradeGrade = "A";
            }
            else if (
                (pullbackResult == PullbackResult.Valid_Strong ||
                 pullbackResult == PullbackResult.Valid_Momentum) &&
                confirmCount >= 1
            )
            {
                tradeGrade = "B";
            }
            else
            {
                tradeGrade = "C";
            }

            // Filter weak C trades
           /* if (tradeGrade == "C" && confirmationScore < 3)
            {
                Print($"Blocked weak C trade | Score: {confirmationScore}");
                return;
            }*/

            // ===============================
            // HTF CONTEXT RISK FILTER
            // ===============================
            bool counterTrendTrade =
                h4Trend != "transitional" &&
                h1Trend != "transitional" &&
                h4Trend != h1Trend;


            if (counterTrendTrade)
            {
                requiredScore = Math.Max(requiredScore, 3);

                Print("Countertrend H1 move vs H4 context -> extra confirmation required");

                // Block weak pullbacks
                if (pullbackResult == PullbackResult.Valid_Weak)
                {
                    Print("Blocked weak countertrend pullback");
                    return;
                }

                // Block weak C-grade trades
                if (tradeGrade == "C")
                {
                    Print("Blocked C-grade countertrend trade");
                    return;
                }
            }

            
            // BUY & SELL ARM Logic
            int i = m15Bars.Count - 2;

            double swingHigh = m15Bars.HighPrices.Skip(i - 6).Take(5).Max();
            double swingLow  = m15Bars.LowPrices.Skip(i - 6).Take(5).Min();

            double prevHigh = m15Bars.HighPrices[i];
            double prevLow  = m15Bars.LowPrices[i];

            readyToBuy = false;
            readyToSell = false;

            // ===============================
            // BUY ARM
            // ===============================
            bool bullishTrigger =
                m15Bars.ClosePrices[i] > m15Bars.OpenPrices[i] &&
                m15Bars.ClosePrices[i] > m15Bars.HighPrices[i - 1];

            if (executionTrend == "bullish" && bullishTrigger)
            {
                readyToBuy = true;
                readyToSell = false;   // important
                armedBars = 0;

                if (EntryMode == 1)
                    triggerHigh = swingHigh;
                else if (EntryMode == 2)
                    triggerHigh = prevHigh;
                else if (EntryMode == 3)
                    triggerHigh = Symbol.Ask;

                status = "BUY Armed @ " + triggerHigh;
            }

            // ===============================
            // SELL ARM
            // ===============================
            bool bearishTrigger =
                m15Bars.ClosePrices[i] < m15Bars.OpenPrices[i] &&
                m15Bars.ClosePrices[i] < m15Bars.LowPrices[i - 1];

            if (executionTrend == "bearish" && bearishTrigger)
            {
                readyToSell = true;
                readyToBuy = false;   // important
                armedBars = 0;

                if (EntryMode == 1)
                    triggerLow = swingLow;
                else if (EntryMode == 2)
                    triggerLow = prevLow;
                else if (EntryMode == 3)
                    triggerLow = Symbol.Bid;

                status = "SELL Armed @ " + triggerLow;
            }

            // ===============================
            // FINAL SUMMARY PRINT
            // ===============================
            currentTradeGrade = tradeGrade;
            currentTradeTrend = executionTrend;
            currentConfirmScore = confirmationScore;
            currentRequiredScore = requiredScore;
            currentPullbackResult = pullbackResult.ToString();

            PrintSummary(status, executionTrend, confirmationScore);
        }

        // ===================================
        // CLEAN SUMMARY LOG METHOD
        // ===================================
        private void PrintSummary(string status, string trend, int score)
        {
            Print("[Bar ",
                  totalBarsChecked,
                  "] ",
                  SymbolName,
                  " | Trend: ",
                  trend,
                  " | Score: ",
                  score,
                  " | ",
                  status);
        }

        // -------------------------
        // SESSION
        // -------------------------
        private bool IsTradingSession()
        {
            //var hour = Server.Time.Hour;
            int hour = Server.TimeInUtc.Hour;
            return (hour >= 7 && hour <= 22);
        }

        // -------------------------
        // TREND (STRUCTURE)
        // -------------------------
        private string GetH4Trend()
        {
            int i = h4Bars.Count - 2;

            int bullishScore = 0;
            int bearishScore = 0;

            // 1. Price vs EMA200
            if (h4Bars.ClosePrices[i] > ema200_H4.Result[i])
                bullishScore++;
            else
                bearishScore++;

            // 2. EMA50 vs EMA200
            if (ema50_H4.Result[i] > ema200_H4.Result[i])
                bullishScore++;
            else
                bearishScore++;

            // 3. Structure
            if (h4Bars.HighPrices[i] > h4Bars.HighPrices[i - 2] &&
                h4Bars.LowPrices[i] > h4Bars.LowPrices[i - 2])
            {
                bullishScore++;
            }

            if (h4Bars.HighPrices[i] < h4Bars.HighPrices[i - 2] &&
                h4Bars.LowPrices[i] < h4Bars.LowPrices[i - 2])
            {
                bearishScore++;
            }

            //Print("H4 Bull Score: ", bullishScore," | H4 Bear Score: ", bearishScore);

            if (bullishScore >= 2 && bullishScore > bearishScore)
                return "bullish";

            if (bearishScore >= 2 && bearishScore > bullishScore)
                return "bearish";

            return "transitional";
        }


        private string GetH1Trend()
        {
            int i = h1Bars.Count - 2;

            int bullishScore = 0;
            int bearishScore = 0;

            // 1. EMA Alignment
            if (ema21_H1.Result[i] > ema50_H1.Result[i])
                bullishScore++;
            else
                bearishScore++;

            // 2. Price above/below EMA21
            if (h1Bars.ClosePrices[i] > ema21_H1.Result[i])
                bullishScore++;
            else
                bearishScore++;

            // 3. Structure
            if (h1Bars.HighPrices[i] > h1Bars.HighPrices[i - 2] &&
                h1Bars.LowPrices[i] > h1Bars.LowPrices[i - 2])
            {
                bullishScore++;
            }

            if (h1Bars.HighPrices[i] < h1Bars.HighPrices[i - 2] &&
                h1Bars.LowPrices[i] < h1Bars.LowPrices[i - 2])
            {
                bearishScore++;
            }

            //Print("H1 Bull Score: ", bullishScore, " | H1 Bear Score: ", bearishScore);

            if (bullishScore >= 2 && bullishScore > bearishScore)
                return "bullish";

            if (bearishScore >= 2 && bearishScore > bullishScore)
                return "bearish";

            return "transitional";
        }


        private bool IsM15TrendAligned(string trend)
        {
            int i = m15Bars.Count - 2;

            if (trend == "bullish")
                return m15Bars.ClosePrices[i] > ema21_M15.Result[i];

            if (trend == "bearish")
                return m15Bars.ClosePrices[i] < ema21_M15.Result[i];

            return false;
        }

        // -------------------------
        // EMA ALIGNMENT
        // -------------------------
        private bool IsEMAAligned(string trend)
        {
            int i = h1Bars.Count - 2;

            if (trend == "bullish")
                return h1Bars.ClosePrices[i] > ema200_H1.Result[i] &&
                       ema21_H1.Result[i] >= ema50_H1.Result[i];

            if (trend == "bearish")
                return h1Bars.ClosePrices[i] < ema200_H1.Result[i] &&
                       ema21_H1.Result[i] <= ema50_H1.Result[i];

            return false;
        }

        private int GetEMAScore(string trend)
        {
            int i = h1Bars.Count - 2;
            double price = h1Bars.ClosePrices[i];

            int score = 0;

            // Price vs EMA200
            if (trend == "bullish" && price > ema200_H1.Result[i])
                score++;
            else if (trend == "bearish" && price < ema200_H1.Result[i])
                score++;

            // EMA 21 vs EMA 50
            if (trend == "bullish" && ema21_H1.Result[i] >= ema50_H1.Result[i])
                score++;
            else if (trend == "bearish" && ema21_H1.Result[i] <= ema50_H1.Result[i])
                score++;

            return score; // 0 to 2
        }
        // -------------------------
        // CHOPPY FILTER
        // -------------------------
        private bool IsChoppyMarket()
        {
            int lookback = 10;
            int start = m15Bars.Count - lookback;

            double highest = m15Bars.HighPrices
                .Skip(start)
                .Take(lookback)
                .Max();

            double lowest = m15Bars.LowPrices
                .Skip(start)
                .Take(lookback)
                .Min();

            double range = highest - lowest;

            // Average candle size
            double avgCandle = 0;

            for (int i = start; i < m15Bars.Count; i++)
            {
                avgCandle +=
                    (m15Bars.HighPrices[i] - m15Bars.LowPrices[i]);
            }

            avgCandle /= lookback;

            // EMA separation
            int iLast = m15Bars.Count - 2;

            double emaDistance =
                Math.Abs(
                    ema21_M15.Result[iLast] -
                    ema50_M15.Result[iLast]);

            // CONDITIONS

            bool lowExpansion =
                range < avgCandle * 2.5;

            bool emaCompressed =
                emaDistance < (avgCandle * 0.3);

            bool strongMomentum = IsMomentumStrong();

            if (strongMomentum)
                return false;
            
            return lowExpansion && emaCompressed;
        }


        //Pullback CheckM15Pullback
        private PullbackResult CheckM15Pullback(string trend)
        {
            int start = m15Bars.Count - 6;
            int end   = m15Bars.Count - 2;

            if (m15Bars.Count < 10)
                return PullbackResult.NoRetrace;

            int retraceBars = 0;
            int consecutive = 0;
            int maxConsecutive = 0;

            bool smallPullback = false;

            for (int i = start; i <= end; i++)
            {
                double open = m15Bars.OpenPrices[i];
                double close = m15Bars.ClosePrices[i];
                double high = m15Bars.HighPrices[i];
                double low = m15Bars.LowPrices[i];

                double body = Math.Abs(close - open);
                double range = high - low;

                bool strong = range > 0 && (body / range >= 0.3); // 🔥 relaxed

                bool isRetrace = false;

                if (trend == "bearish" && close > open)
                    isRetrace = true;

                if (trend == "bullish" && close < open)
                    isRetrace = true;

                if (isRetrace)
                {
                    retraceBars++;
                    consecutive++;

                    if (consecutive > maxConsecutive)
                        maxConsecutive = consecutive;

                    // detect weak pullback
                    if (!strong)
                        smallPullback = true;
                }
                else
                {
                    consecutive = 0;
                }
            }

            // =========================
            // SMART DECISION LOGIC
            // =========================
            double lastClose = m15Bars.ClosePrices[end];
            double prevClose = m15Bars.ClosePrices[start];

            double retraceSize = Math.Abs(lastClose - prevClose);
            double atr = atr_M15.Result[end];

            bool deepEnough = retraceSize >= atr * 0.5;
            // 🔥 Strong pullback
            if (maxConsecutive >= 3 && retraceBars >= 3 && deepEnough)
                return PullbackResult.Valid_Strong;

            // 🔥 Weak pullback (ALLOW IT)
            if (retraceBars >= 2 && smallPullback)
                return PullbackResult.Valid_Weak;

            if (retraceBars == 0)
                return PullbackResult.NoRetrace;

            // 🔥 Momentum pullback (VERY IMPORTANT)
            if (retraceBars <= 1)
                return PullbackResult.Valid_Momentum;

            return PullbackResult.TooShallow;
        }

        // ===============================
        // PULLBACK WRAPPER (BOOLEAN)
        // ===============================
        private bool IsM15Pullback(string trend, out PullbackResult result)
        {
            result = CheckM15Pullback(trend);

            return result.ToString().StartsWith("Valid");
        }

        // -------------------------
        // ORDER BLOCK
        // -------------------------
        private bool IsOrderBlockValid(string trend)
        {
            int i = m15Bars.Count - 3;

            double open = m15Bars.OpenPrices[i];
            double close = m15Bars.ClosePrices[i];

            double high = m15Bars.HighPrices[i];
            double low = m15Bars.LowPrices[i];

            // Bullish OB
            if (trend == "bullish")
            {
                return close < open &&
                       m15Bars.ClosePrices[i + 1] > high;
            }

            // Bearish OB
            if (trend == "bearish")
            {
                return close > open &&
                       m15Bars.ClosePrices[i + 1] < low;
            }

            return false;
        }

        // -------------------------
        // FAIR VALUE GAP (FVG)
        // -------------------------
        private bool IsFVGPresent(string trend)
        {
            int i = m15Bars.Count - 2;

            double high1 = m15Bars.HighPrices[i - 2];
            double low3 = m15Bars.LowPrices[i];

            double low1 = m15Bars.LowPrices[i - 2];
            double high3 = m15Bars.HighPrices[i];

            double gapBull = low3 - high1;
            double gapBear = low1 - high3;

            double minGap = 20 * Symbol.PipSize;

            if (trend == "bullish")
                return gapBull > minGap;

            if (trend == "bearish")
                return gapBear > minGap;

            return false;
        }

        // -------------------------
        // MOMENTUM + VOLUME
        // -------------------------
        private bool IsMomentumStrong()
        {
            int i = m15Bars.Count - 2;

            double body = Math.Abs(m15Bars.ClosePrices[i] - m15Bars.OpenPrices[i]);
            double range = m15Bars.HighPrices[i] - m15Bars.LowPrices[i];

            double volume = m15Bars.TickVolumes[i];
            double avgVolume = m15Bars.TickVolumes.Skip(i - 10).Take(10).Average();

            return body > (range * 0.35) && volume > avgVolume;
        }

        // -------------------------
        // SWING BOS
        // -------------------------
        private string GetSwingBoS(string trend)
        {
            int i = m15Bars.Count - 2;

            double swingHigh = m15Bars.HighPrices.Skip(i - 6).Take(5).Max();
            double swingLow  = m15Bars.LowPrices.Skip(i - 6).Take(5).Min();

            double high = m15Bars.HighPrices[i];
            double low = m15Bars.LowPrices[i];
            double close = m15Bars.ClosePrices[i];
            double open = m15Bars.OpenPrices[i];

            if (trend == "bullish" && high > swingHigh && close > open)
                return "buy";

            if (trend == "bearish" && low < swingLow && close < open)
                return "sell";

            return "none";
        }

        private bool IsNearSupportResistance(string trend)
        {
            int i = m15Bars.Count - 1;

            double swingHigh = m15Bars.HighPrices.Skip(i - 10).Take(10).Max();
            double swingLow = m15Bars.LowPrices.Skip(i - 10).Take(10).Min();

            double price = m15Bars.ClosePrices[i];

            double distanceToHigh = (swingHigh - price) / Symbol.PipSize;
            double distanceToLow = (price - swingLow) / Symbol.PipSize;

            if (trend == "bullish")
            {
                // Too close to resistance
                return distanceToHigh < MinSRDistance;
            }

            if (trend == "bearish")
            {
                // Too close to support
                return distanceToLow < MinSRDistance;
            }

            return false;
        }

        // ======================================================
        // SMART 3-STAGE TRAILING STOP
        // Stage 1 = Breakeven
        // Stage 2 = Lock Profit
        // Stage 3 = Trail Runner
        // ======================================================

        private void ManageSmartTrailingStop(Position position)
        {
            if (position == null)
                return;

            double pip = Symbol.PipSize;
            double? currentSL = position.StopLoss;

            // ----------------------------------
            // USER PARAMETERS (example defaults)
            // ----------------------------------
            double breakEvenTrigger = 100 * pip;
            double lockProfitTrigger = 180 * pip;
            double trailStart = 250 * pip;

            double lockProfitAmount = 80 * pip;
            double trailDistance = TrailingStop  * pip;
            double minStep = 30 * pip;

            // ==================================================
            // BUY POSITION
            // ==================================================
            if (position.TradeType == TradeType.Buy)
            {
                double profitNow = Symbol.Bid - position.EntryPrice;

                // ---------------------------
                // Stage 1 : Move to Breakeven
                // ---------------------------
                if (profitNow >= breakEvenTrigger)
                {
                    double newSL = position.EntryPrice;

                    if (currentSL == null || newSL > currentSL + minStep)
                    {
                        ModifyPosition(position, newSL, position.TakeProfit, ProtectionType.Absolute);
                        //Print("BUY BE moved to Entry");
                    }
                }

                // ---------------------------
                // Stage 2 : Lock Profit
                // ---------------------------
                if (profitNow >= lockProfitTrigger)
                {
                    double newSL = position.EntryPrice + lockProfitAmount;

                    if (currentSL == null || newSL > currentSL + minStep)
                    {
                        ModifyPosition(position, newSL, position.TakeProfit, ProtectionType.Absolute);
                        //Print("BUY locked profit");
                    }
                }

                // ---------------------------
                // Stage 3 : Trail Runner
                // ---------------------------
                if (profitNow >= trailStart)
                {
                    double newSL = Symbol.Bid - trailDistance;

                    if (newSL < position.EntryPrice)
                        newSL = position.EntryPrice;

                    if (currentSL == null || newSL > currentSL + minStep)
                    {
                        ModifyPosition(position, newSL, position.TakeProfit, ProtectionType.Absolute);
                        //Print("BUY trailing active");
                    }
                }
            }

            // ==================================================
            // SELL POSITION
            // ==================================================
            if (position.TradeType == TradeType.Sell)
            {
                double profitNow = position.EntryPrice - Symbol.Ask;

                // ---------------------------
                // Stage 1 : Move to Breakeven
                // ---------------------------
                if (profitNow >= breakEvenTrigger)
                {
                    double newSL = position.EntryPrice;

                    if (currentSL == null || newSL < currentSL - minStep)
                    {
                        ModifyPosition(position, newSL, position.TakeProfit, ProtectionType.Absolute);
                        //Print("SELL BE moved to Entry");
                    }
                }

                // ---------------------------
                // Stage 2 : Lock Profit
                // ---------------------------
                if (profitNow >= lockProfitTrigger)
                {
                    double newSL = position.EntryPrice - lockProfitAmount;

                    if (currentSL == null || newSL < currentSL - minStep)
                    {
                        ModifyPosition(position, newSL, position.TakeProfit, ProtectionType.Absolute);
                        //Print("SELL locked profit");
                    }
                }

                // ---------------------------
                // Stage 3 : Trail Runner
                // ---------------------------
                if (profitNow >= trailStart)
                {
                    double newSL = Symbol.Ask + trailDistance;

                    if (newSL > position.EntryPrice)
                        newSL = position.EntryPrice;

                    if (currentSL == null || newSL < currentSL - minStep)
                    {
                        ModifyPosition(position, newSL, position.TakeProfit, ProtectionType.Absolute);
                        //Print("SELL trailing active");
                    }
                }
            }
        }

        private (double? sl, double? tp, double? volumeInUnits) GetDynamicSLTP(TradeType type)
        {
            int i = m15Bars.Count - 1;

            if (i < 15)
                return (null, null, null);

            // Use REALISTIC entry price
            double entry = type == TradeType.Buy ? Symbol.Ask : Symbol.Bid;


            // -----------------------------------
            // SETTINGS
            // -----------------------------------
            int lookback = 0;
            if (currentPullbackResult == "Valid_Momentum")
                lookback = 3;
            else if (currentPullbackResult == "Valid_Strong")
                lookback = 5;
            else
                lookback = 6;

            double bufferPips = SymbolName == "XAUUSD" ? 10 : 5;
            double minSLPips = MinSLPips;
            double maxSLPips = SymbolName == "XAUUSD" ? 600 : MaxSLPips;     

            double rr = 1.8;   // Reward ratio

            // -----------------------------------
            // SWING LEVELS
            // -----------------------------------
            double swingHigh = m15Bars.HighPrices.Skip(i - lookback).Take(lookback).Max();
            double swingLow = m15Bars.LowPrices.Skip(i - lookback).Take(lookback).Min();

            double sl = 0;
            double tp = 0;
            double risk = 0;

            // -----------------------------------
            // BUY
            // -----------------------------------
            if (type == TradeType.Buy)
            {
                sl = swingLow - (bufferPips * Symbol.PipSize);
                risk = entry - sl;

                if (risk <= 0)
                    return (null, null, null);

                tp = entry + (risk * rr);
            }

            // -----------------------------------
            // SELL
            // -----------------------------------
            else
            {
                sl = swingHigh + (bufferPips * Symbol.PipSize);
                risk = sl - entry;

                if (risk <= 0)
                    return (null, null, null);

                tp = entry - (risk * rr);
            }

            // -----------------------------------
            // CHECK STOP SIZE
            // -----------------------------------
            double slPips = Math.Abs(risk / Symbol.PipSize);

            double riskAmount = Account.Balance * (RiskPercent / 100.0);
            double volumeInUnits = riskAmount / (slPips * Symbol.PipValue);
            volumeInUnits =
                Symbol.NormalizeVolumeInUnits(
                    volumeInUnits,
                    RoundingMode.Down
                );
           /* Print(
                "RISK CHECK | Balance: {0:F2} | Risk%: {1:F1} | Target Risk: {2:F2} | Volume: {3:F0} | SL Pips: {4:F1}",
                Account.Balance,
                RiskPercent,
                riskAmount,
                volumeInUnits,
                slPips
            );*/

            Print(
                "SL DEBUG | Type: {0} | Entry: {1:F2} | SwingHigh: {2:F2} | SwingLow: {3:F2} | Risk: {4:F2} | SL Pips: {5:F1}",
                type,
                entry,
                swingHigh,
                swingLow,
                risk,
                slPips
            );

            if (slPips < minSLPips)
            {
                blockedSLTooSmall++;    
                Print($"SL too small, skip trade | Entry: {entry} | SL: {slPips} | Min: {minSLPips}");
                return (null, null, null);
            }

            if (slPips > maxSLPips)
            {
                blockedSLTooLarge++;
                Print($"SL too large, skip trade | Entry: {entry} | SL: {slPips} | Max: {maxSLPips}");
                return (null, null, null);
            }

            return (sl, tp, volumeInUnits);
        }


        // -------------------------
        // EXECUTION
        // -------------------------
        private void ExecuteTrade(TradeType type)
        {
            
            /*if (type == TradeType.Buy)
            {
                Print("Buy trades disabled for testing");
                return;
            }*/
            string session = "Asian";

            //int hour = Server.Time.Hour;
            int hour = Server.TimeInUtc.Hour;

            if (hour >= 7 && hour < 13)
                session = "London";

            else if (hour >= 13 && hour < 21)
                session = "NewYork";


            var (slPrice, tpPrice, volumeInUnits) = GetDynamicSLTP(type);

            if (slPrice == null || tpPrice == null || volumeInUnits == null)
                return;

            double entry = type == TradeType.Buy ? Symbol.Ask : Symbol.Bid;

            double slPips = 0;
            double tpPips = 0;

            if (type == TradeType.Buy)
            {
                slPips = (entry - slPrice.Value) / Symbol.PipSize;
                tpPips = (tpPrice.Value - entry) / Symbol.PipSize;
            }
            else
            {
                slPips = (slPrice.Value - entry) / Symbol.PipSize;
                tpPips = (entry - tpPrice.Value) / Symbol.PipSize;
            }

            if (slPips <= 0 || tpPips <= 0)
            {
                Print("Invalid SL/TP, skipping trade");
                return;
            }

            var result = ExecuteMarketOrder(
                type,
                SymbolName,
                volumeInUnits.Value,
                BotLabel,
                slPips,
                tpPips
            );

            if (result.IsSuccessful)
            {
                totalTrades++;
                tradeGrades[result.Position.Id] = currentTradeGrade;

                //Save Trade Regime     
                tradeRegimes[result.Position.Id] = currentRegime;
                tradeSessions[result.Position.Id] = session;
            }

            

            Print(
                "TRADE EXECUTED | Grade: {0} | Trend: {1} | Pullback: {2} | Type: {3} | Entry: {4} | SL: {5} ({6} pips) | TP: {7} ({8} pips) | Score: {9}/{10}",

                currentTradeGrade,
                currentTradeTrend,
                currentPullbackResult,
                type,
                entry,
                slPrice,
                slPips,
                tpPrice,
                tpPips,
                currentConfirmScore,
                currentRequiredScore
            );
        }
        
        
        // -------------------------
        // MANAGEMENT
        // -------------------------
        protected override void OnTick()
        {
            bool hasOpenBotTrade = Positions.Any(p =>
            p.SymbolName == SymbolName &&
            p.Label == BotLabel);

            double buffer = BreakoutBuffer * Symbol.PipSize;

            if (readyToBuy && !hasOpenBotTrade && Symbol.Bid > triggerHigh)
            {
                double extensionPips = Math.Abs(Symbol.Ask - ema21_M15.Result.LastValue) / Symbol.PipSize;

                if (extensionPips <= 150)
                {
                    ExecuteTrade(TradeType.Buy);
                }
                else
                {
                    Print("Blocked BUY: EMA extension too large | Extension: {0:F1}", extensionPips);
                }

                readyToBuy = false;
                readyToSell = false;
            }

            if (readyToSell && !hasOpenBotTrade && Symbol.Ask < triggerLow)
            {

                double extensionPips = Math.Abs(Symbol.Bid - ema21_M15.Result.LastValue) / Symbol.PipSize;

                if (extensionPips <= 150)
                {
                    ExecuteTrade(TradeType.Sell);
                }
                else
                {
                    Print("Blocked SELL: EMA extension too large | Extension: {0:F1}", extensionPips);
                }
                
                readyToSell = false;
                readyToBuy = false;
            }

            foreach (var position in Positions)
            {
                if (position.SymbolName != SymbolName)
                    continue;

                if (position.Label != BotLabel)
                    continue;

                double profit = position.Pips;
                
                // Partial TP
                double halfVolume =  position.VolumeInUnits / 2.0;
                double currentVolume = position.VolumeInUnits;

                // Partial TP only once
                if (EnablePartialTP &&
                    profit >= PartialTP &&
                    !partialClosed.Contains(position.Id) &&
                    currentVolume > halfVolume)
                {
                    ClosePosition(position, halfVolume);
                    partialClosed.Add(position.Id);

                    Print(
                        "PARTIAL TP | Type: {0} | Current Profit: {1:F2} | Pips: {2:F1}",
                        position.TradeType,
                        position.NetProfit,
                        position.Pips
                    );
                }
                
                // Trailing
                //ManageSmartTrailingStop(position);
            }
        }
    }
}