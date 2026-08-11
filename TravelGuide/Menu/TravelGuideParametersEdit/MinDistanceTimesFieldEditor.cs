using System;
using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.FieldEditors;
using ParametersManagement.LibParameters;

namespace TravelGuide.Menu.TravelGuideParametersEdit;

public sealed class MinDistanceTimesFieldEditor : FieldEditor<List<TimeSpan>>
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public MinDistanceTimesFieldEditor(string propertyName, IParametersManager parametersManager) : base(propertyName,
        false, null, false, null, true)
    {
        _parametersManager = parametersManager;
    }

    public override CliMenuSet GetSubMenu(object record)
    {
        List<TimeSpan> currentValuesList = GetValue(record) ?? [];
        return new MinDistanceTimeCruder(_parametersManager, currentValuesList).GetListMenu();
    }

    public override string GetValueStatus(object? record)
    {
        List<TimeSpan>? val = GetValue(record);

        if (val is not { Count: > 0 })
        {
            return "No Details";
        }

        if (val.Count > 1)
        {
            return $"{val.Count} Details";
        }

        return MinDistanceTimeCruder.GetKey(val.Single());
    }
}
