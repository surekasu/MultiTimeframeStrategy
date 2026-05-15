using System;

namespace cAlgo.Robots;

public class TradeCloseLog
{
    public string Event { get; set; }

    public string TradeId { get; set; }

    public string SignalId { get; set; }

    public DateTime TimestampUtc { get; set; }

    public double GrossProfit { get; set; }

    public double NetProfit { get; set; }

    public double Pips { get; set; }

    public double ActualRR { get; set; }

    public int DurationMinutes { get; set; }

    public string ExitReason { get; set; }

    public bool IsWin { get; set; }
}
