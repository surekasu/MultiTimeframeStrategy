using System;

namespace cAlgo.Robots;

public class TradeCloseLog
{
    public string Event { get; set; }

    public string TradeId { get; set; }

    public string SignalId { get; set; }

    public string BotName { get; set; }

    public string StrategyVersion { get; set; }

    public DateTime TimestampUtc { get; set; }

    public DateTime LocalTimestamp { get; set; }

    public string Symbol { get; set; }

    public String Direction { get; set; }

    public double EntryPrice { get; set; }

    public double ClosePrice { get; set; }

    public double GrossProfit { get; set; }

    public double NetProfit { get; set; }

    public double Pips { get; set; }

    public double ActualRR { get; set; }

    public string ExitReason { get; set; }

    public bool IsWin { get; set; }

    public double Volume { get; set; }

    public double Commission { get; set; }

    public double Swap { get; set; }

    public int DurationMinutes { get; set; }

    public DateTime CreatedAt { get; set; }
}
