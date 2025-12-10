namespace TestGrand.Core.Services;

public interface IRollerService
{
    public int Roll(CubeTypes cube, int modificator);
}

public class RollerService : IRollerService
{
    private Random _random = new Random();

    public int Roll(CubeTypes cube, int modificator)
    {
        var result = _random.Next(1, (int)cube);

        return result + modificator;
    }
}

public enum CubeTypes
{
    D4 = 4,
    D6 = 6,
    D8 = 8,
    D10 = 10,
    D12 = 12,
    D20 = 20,
    D100 = 100
}