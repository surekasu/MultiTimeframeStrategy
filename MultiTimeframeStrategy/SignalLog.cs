using System;

namespace cAlgo.Robots;

public class SignalLog
{
    public string Event { get; set; }

    public string SignalId { get; set; }

    public long TradeId { get; set; } 

    public string BotName { get; set; }

    public string StrategyVersion { get; set; }

    public DateTime TimestampUtc { get; set; }

    public DateTime LocalTimestamp { get; set; }

    public string Symbol { get; set; }

    public string Timeframe { get; set; }

    public string Direction { get; set; }

    public string Session { get; set; }

    public string Regime { get; set; }

    public string H1Trend { get; set; }

    public string H4Trend { get; set; }

    public bool Countertrend { get; set; }

    public string PullbackType { get; set; }

    public string Grade { get; set; }

    public int ConfirmationScore { get; set; }

    public int RequiredScore { get; set; }

    public bool MomentumConfirmed { get; set; }

    public bool ObConfirmed { get; set; }

    public bool FvgConfirmed { get; set; }

    public double EmaExtension { get; set; }

    public int ContinuationLegs { get; set; }

    public bool Ema21Alignment { get; set; }

    public double EntryPrice { get; set; }

    public double StopLoss { get; set; }

    public double TakeProfit { get; set; }

    public double SlPips { get; set; }

    public double TpPips { get; set; }

    public double PlannedRR { get; set; }

    public double Volume { get; set; }

    public double Spread { get; set; }

    public double Atr { get; set; }

    public bool TelegramSent { get; set; }

    public bool TradeExecuted { get; set; }

    public string ExecutionMode { get; set; }

    public string SignalStatus { get; set; }

    public string SignalQuality { get; set; }

    public string CreatedAt { get; set; }
}
