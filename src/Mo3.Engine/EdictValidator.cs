namespace Mo3.Engine;

public sealed record EdictValidationError
{
    public required string Message { get; init; }
}

public static class EdictValidator
{
    public static IReadOnlyList<EdictValidationError> Validate(EdictBase edict, FactionState factionState)
    {
        var errors = new List<EdictValidationError>();

        if (string.IsNullOrWhiteSpace(edict.IssuingFactionId))
        {
            errors.Add(new EdictValidationError { Message = "Issuing faction is required." });
        }

        if (factionState.IsInEconomicCollapse && !IsAllowedDuringCollapse(edict))
        {
            errors.Add(new EdictValidationError
            {
                Message = "Faction in economic collapse may only issue trade contracts or cancellation/downscale edicts."
            });
        }

        switch (edict)
        {
            case InternalProductionEdict internalEdict:
                ValidateInternal(internalEdict, errors);
                break;
            case ExternalEdict externalEdict:
                ValidateExternal(externalEdict, errors);
                break;
            case MilitaryEdict militaryEdict:
                ValidateMilitary(militaryEdict, errors);
                break;
            default:
                errors.Add(new EdictValidationError { Message = "Unknown edict type." });
                break;
        }

        return errors;
    }

    private static bool IsAllowedDuringCollapse(EdictBase edict)
    {
        if (edict.IsCancellation)
        {
            return true;
        }

        return edict is ExternalEdict { Type: ExternalEdictType.TradeContract };
    }

    private static void ValidateInternal(InternalProductionEdict edict, List<EdictValidationError> errors)
    {
        if (edict.Section == EdictSection.Military)
        {
            errors.Add(new EdictValidationError { Message = "Internal edict cannot be in military section." });
        }

        if (string.IsNullOrWhiteSpace(edict.EdictName))
        {
            errors.Add(new EdictValidationError { Message = "Internal edict name is required." });
        }

        if (edict.ExecutionCount <= 0)
        {
            errors.Add(new EdictValidationError { Message = "Internal edict execution count must be positive." });
        }

        if (edict.InputRequirements.Count == 0)
        {
            errors.Add(new EdictValidationError { Message = "Internal edict must define at least one input requirement." });
        }

        if (edict.Outputs.Count == 0)
        {
            errors.Add(new EdictValidationError { Message = "Internal edict must define at least one output." });
        }

        ValidateAmounts(edict.InputRequirements.Select(x => (x.Resource, x.Amount)), "input", errors);
        ValidateAmounts(edict.Outputs.Select(x => (x.Resource, x.Amount)), "output", errors);
    }

    private static void ValidateExternal(ExternalEdict edict, List<EdictValidationError> errors)
    {
        if (edict.Section == EdictSection.Military)
        {
            errors.Add(new EdictValidationError { Message = "External edict cannot be in military section." });
        }

        if (string.IsNullOrWhiteSpace(edict.TargetFactionId))
        {
            errors.Add(new EdictValidationError { Message = "External edict target faction is required." });
        }

        if (edict.Amount < 0)
        {
            errors.Add(new EdictValidationError { Message = "External edict amount cannot be negative." });
        }

        if (edict.Type == ExternalEdictType.TradeContract)
        {
            if (edict.Resource is null)
            {
                errors.Add(new EdictValidationError { Message = "Trade contract requires a resource." });
            }

            if (edict.Amount <= 0)
            {
                errors.Add(new EdictValidationError { Message = "Trade contract amount must be positive." });
            }
        }
    }

    private static void ValidateMilitary(MilitaryEdict edict, List<EdictValidationError> errors)
    {
        if (edict.Section != EdictSection.Military)
        {
            errors.Add(new EdictValidationError { Message = "Military edict must be in military section." });
        }

        switch (edict.Type)
        {
            case MilitaryEdictType.Attack:
            case MilitaryEdictType.Takeover:
            case MilitaryEdictType.Liberation:
                if (string.IsNullOrWhiteSpace(edict.TargetCityId))
                {
                    errors.Add(new EdictValidationError { Message = "Military operation requires target city." });
                }

                break;
            case MilitaryEdictType.SupportAttack:
                if (string.IsNullOrWhiteSpace(edict.SupportedFactionId))
                {
                    errors.Add(new EdictValidationError { Message = "Support attack requires supported faction." });
                }

                if (string.IsNullOrWhiteSpace(edict.TargetCityId))
                {
                    errors.Add(new EdictValidationError { Message = "Support attack requires target city." });
                }

                break;
            case MilitaryEdictType.EndOccupation:
                if (string.IsNullOrWhiteSpace(edict.TargetCityId))
                {
                    errors.Add(new EdictValidationError { Message = "End occupation requires target city." });
                }

                break;
        }
    }

    private static void ValidateAmounts(
        IEnumerable<(ResourceType Resource, int Amount)> amounts,
        string label,
        List<EdictValidationError> errors)
    {
        foreach (var (_, amount) in amounts)
        {
            if (amount <= 0)
            {
                errors.Add(new EdictValidationError
                {
                    Message = $"Internal edict {label} amount must be positive."
                });
            }
        }
    }
}
