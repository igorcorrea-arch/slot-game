namespace Slots.Engine;

public class FeaturePayoutCalculatorFacade
{
    public static long Calculate(IReadOnlyList<CoinPrize?> board, long betCents, int grandPrizeTenths) {
        long cashTenths = 0;
        long multiplier = 1;

        foreach (var coin in board)
        {
            if (coin is null)
                continue;

            if (coin.Kind == "cash")
            {
                cashTenths = checked(
                    cashTenths + coin.ValueTenths);
            }
            else if (coin.Kind == "mult")
            {
                multiplier = checked(
                    multiplier * coin.ValueTenths);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unknown prize kind: {coin.Kind}");
            }
        }

        long unit = betCents / 10;

        long total = checked(
            unit * cashTenths * multiplier);

        bool fullBoard = board.All(coin => coin is not null);

        if (fullBoard)
        {
            long grand = checked(unit * grandPrizeTenths);
            total = checked(total + grand);
        }

        return total;
    }
}