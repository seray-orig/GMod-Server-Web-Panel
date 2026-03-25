using ASP_API.Models;
using System.Globalization;

public class SystemStatsService
{
    public SystemStats GetStats()
    {
        return new SystemStats
        {
            MemoryUsage = GetMemoryUsage(),
            CpuUsage = GetCpuUsage(),
            DiskUsage = GetDiskUsage()
        };
    }

    private double GetMemoryUsage()
    {
        var lines = File.ReadAllLines("/proc/meminfo");

        double total = 0;
        double available = 0;

        foreach (var line in lines)
        {
            if (line.StartsWith("MemTotal"))
                total = ParseKb(line);

            if (line.StartsWith("MemAvailable"))
                available = ParseKb(line);
        }

        return (1 - (available / total)) * 100;
    }

    private double GetCpuUsage()
    {
        var line = File.ReadLines("/proc/stat").First();
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        double user = double.Parse(parts[1], CultureInfo.InvariantCulture);
        double system = double.Parse(parts[3], CultureInfo.InvariantCulture);
        double idle = double.Parse(parts[4], CultureInfo.InvariantCulture);

        double total = user + system + idle;

        return ((total - idle) / total) * 100;
    }

    private double GetDiskUsage()
    {
        // Упростим: просто возвращаем 0 пока
        return 0;
    }

    private double ParseKb(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        // parts[1] содержит число с "kB" (например, "8172344kB" или "8172344 kB")
        var numberStr = new string(parts[1].TakeWhile(char.IsDigit).ToArray());
        return double.Parse(numberStr, CultureInfo.InvariantCulture);
    }
}