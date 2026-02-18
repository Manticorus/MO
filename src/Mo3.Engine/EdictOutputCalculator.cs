namespace Mo3.Engine;

public static class EdictOutputCalculator
{
    private const int BaseInternalOutput = 4;
    private const int StrongResourceBonus = 2;

    public static int GetInternalEdictOutputPerExecution(string factionId, ResourceType outputResource)
    {
        var output = BaseInternalOutput;

        if (DefaultData.StrongResourcesByFaction.TryGetValue(factionId, out var strongResources) &&
            strongResources.Contains(outputResource))
        {
            output += StrongResourceBonus;
        }

        if (factionId == "mar-rhazun" &&
            DefaultData.StrongResourcesByFaction.TryGetValue(factionId, out var marStrongResources) &&
            marStrongResources.Contains(outputResource))
        {
            output += 1;
        }

        if (factionId == "qal-asar" && outputResource == ResourceType.MagicItems)
        {
            output += 2;
        }

        return output;
    }
}
