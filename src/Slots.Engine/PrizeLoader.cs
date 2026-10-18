namespace Slots.Engine;

public class PrizeLoader
{
    public static List<CoinPrize> Load(string path)
    {
        var prizes = new List<CoinPrize>();

        var lines = File.ReadAllLines(path).Skip(1);

        foreach (var line in lines)
        {
            var columns = line.Split(',');

            string kind = columns[0].Trim();
            int value = int.Parse(columns[1].Trim());
            int weight = int.Parse(columns[2].Trim());

            prizes.Add(new CoinPrize(
                kind,
                value,
                weight
            ));
        }

        return prizes;
    }
}