using System.Text;

namespace Slots.Simulator;

/// <summary>Draws a [reel][row] grid on the console, with automatic cell width.</summary>
internal static class GridRenderer
{
    public static string Render(IReadOnlyList<IReadOnlyList<string>> window)
    {
        int reels = window.Count;
        int rows = window[0].Count;
        int width = Math.Max(window.SelectMany(reel => reel).Max(cell => cell.Length), 2) + 2;

        string bar = string.Join('┬', Enumerable.Repeat(new string('─', width + 2), reels));
        var sb = new StringBuilder();
        sb.AppendLine("  ┌" + bar + "┐");
        for (int row = 0; row < rows; row++)
        {
            sb.Append("  │");
            for (int reel = 0; reel < reels; reel++)
            {
                string cell = window[reel][row];
                sb.Append($" {cell.PadLeft((width + cell.Length) / 2).PadRight(width)} │");
            }
            sb.AppendLine();
        }
        sb.Append("  └" + bar.Replace('┬', '┴') + "┘");
        return sb.ToString();
    }
}
