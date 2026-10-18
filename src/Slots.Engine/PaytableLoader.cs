namespace Slots.Engine;

public static class PayTableLoader {
    public static Dictionary<string, Dictionary<int, int>> Load(string path) {
        var payTable = new Dictionary<string, Dictionary<int, int>>();

        var lines = File.ReadAllLines(path).Skip(1);
        
        foreach (var line in lines) {
            var columns = line.Split(',');
            
            string symbol = columns[0].Trim();
            int count = int.Parse(columns[1].Trim());
            int payment = int.Parse(columns[2].Trim());
            
            
            if (!payTable.ContainsKey(symbol))
                payTable[symbol] = new Dictionary<int, int>();
            
            payTable[symbol][count] = payment;
        }
        return payTable;
    }
}