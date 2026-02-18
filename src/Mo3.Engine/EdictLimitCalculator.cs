namespace Mo3.Engine;

public static class EdictLimitCalculator
{
    public const int BaseLimit = 2;

    public static Dictionary<ResourceType, int> CalculateLimits(IEnumerable<City> controlledCities)
    {
        ArgumentNullException.ThrowIfNull(controlledCities);

        var limits = Enum.GetValues<ResourceType>()
            .ToDictionary(resource => resource, _ => BaseLimit);

        var factionSize = 0;
        foreach (var city in controlledCities)
        {
            var cityBonus = Math.Max(0, 20 - factionSize);
            foreach (var focusedResource in city.ResourceFocuses.Distinct())
            {
                limits[focusedResource] += cityBonus;
            }

            factionSize++;
        }

        return limits;
    }
}
