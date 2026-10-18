namespace Slots.Engine;

public static class ReelLoader {
    public static List<List<string>> Load(string path) {
        var reels = new List<List<string>>();
        
        for (int i = 0; i < 5; i++)
            reels.Add(new List<string>());
        
        var lines = File.ReadAllLines(path).Skip(1);

        foreach (var line in lines)
        {
            var columns = line.Split(',');

            for (int reel = 0; reel < 5; reel++)
            {
                string symbol = columns[reel + 1].Trim();
                
                if (!string.IsNullOrEmpty(symbol))
                    reels[reel].Add(symbol);
            }
        }

        return reels;
    }
}