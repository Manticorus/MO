namespace Mo3.Engine;

public static class ProductionScaling
{
    public static ProductionScalingResult Calculate(int baseOutputPerEdict, int requestedEdicts, int edictLimit)
    {
        if (baseOutputPerEdict < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseOutputPerEdict));
        }

        if (requestedEdicts < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedEdicts));
        }

        if (edictLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(edictLimit));
        }

        var maxEdicts = edictLimit * 2;
        var executedEdicts = Math.Min(requestedEdicts, maxEdicts);
        var fullOutputEdicts = Math.Min(executedEdicts, edictLimit);
        var reducedOutputEdicts = executedEdicts - fullOutputEdicts;

        var totalOutput = (fullOutputEdicts * baseOutputPerEdict)
            + (reducedOutputEdicts * (baseOutputPerEdict / 2m));

        return new ProductionScalingResult(
            requestedEdicts,
            executedEdicts,
            fullOutputEdicts,
            reducedOutputEdicts,
            totalOutput);
    }
}

public sealed record ProductionScalingResult(
    int RequestedEdicts,
    int ExecutedEdicts,
    int FullOutputEdicts,
    int ReducedOutputEdicts,
    decimal TotalOutput);
