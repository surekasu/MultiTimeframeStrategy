using System;
using System.IO;
using System.Text.Json;

namespace cAlgo.Robots;

public static class JsonLogger
{
    private static readonly string LogFolder =
        @"C:\TradingSystem\logs\";

    public static void Write(string fileName, object data)
    {
        try
        {
            if (!Directory.Exists(LogFolder))
                Directory.CreateDirectory(LogFolder);

            string fullPath = Path.Combine(LogFolder, fileName);

            string json = JsonSerializer.Serialize(data);

            File.AppendAllText(fullPath, json + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Console.WriteLine("JSON LOG ERROR: " + ex.Message);
        }
    }
}
