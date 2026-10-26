using System.Text;

namespace KznNotch.Services;

public sealed class FpsCsvParser
{
    private int _pid = -1, _interval = -1, _chain = -1;
    public bool TryRead(string line, int targetPid, out double milliseconds, out string chain)
    {
        milliseconds = 0; chain = "";
        var columns = Split(line);
        if (columns.Length > 0 && columns[0].Equals("Application", StringComparison.OrdinalIgnoreCase))
        {
            _pid = Array.FindIndex(columns, x => x.Equals("ProcessID", StringComparison.OrdinalIgnoreCase));
            _interval = Array.FindIndex(columns, x => x.Equals("msBetweenPresents", StringComparison.OrdinalIgnoreCase));
            _chain = Array.FindIndex(columns, x => x.Equals("SwapChainAddress", StringComparison.OrdinalIgnoreCase));
            return false;
        }
        if (_pid < 0 || _interval < 0 || _chain < 0 || columns.Length <= Math.Max(_chain, Math.Max(_pid, _interval))) return false;
        if (!int.TryParse(columns[_pid], out var pid) || pid != targetPid) return false;
        chain = columns[_chain];
        return double.TryParse(columns[_interval], NumberStyles.Float, CultureInfo.InvariantCulture, out milliseconds) && double.IsFinite(milliseconds) && milliseconds > 0 && milliseconds < 5000;
    }
    private static string[] Split(string line)
    {
        var result = new List<string>(); var cell = new StringBuilder(); var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            if (line[i] == '"') { if (quoted && i + 1 < line.Length && line[i + 1] == '"') { cell.Append('"'); i++; } else quoted = !quoted; }
            else if (line[i] == ',' && !quoted) { result.Add(cell.ToString()); cell.Clear(); }
            else cell.Append(line[i]);
        }
        result.Add(cell.ToString()); return result.ToArray();
    }
}
