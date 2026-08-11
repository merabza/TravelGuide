using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.Cruders;
using AppCliTools.LibDataInput;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace TravelGuide.Menu.TravelGuideParametersEdit;

public sealed class MinDistanceTimeCruder : Cruder
{
    private readonly IParametersManager _parametersManager;
    private readonly List<TimeSpan> _currentValuesList;

    //ჩანაწერის გასაღები თვითონ დროა hh:mm ფორმატით, ამიტომ ცალკე სახელი არ სჭირდება
    public MinDistanceTimeCruder(IParametersManager parametersManager, List<TimeSpan> currentValuesList) : base(
        "Min Distance Time", "Min Distance Times", false, false)
    {
        _parametersManager = parametersManager;
        _currentValuesList = currentValuesList;
    }

    internal static string GetKey(TimeSpan value)
    {
        return value.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        return _currentValuesList.DistinctBy(GetKey)
            .ToDictionary(GetKey, ItemData (v) => new TextItemData { Text = GetKey(v) });
    }

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new TextItemData();
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        return _currentValuesList.Any(x => GetKey(x) == recordKey);
    }

    protected override async ValueTask RemoveRecordWithKey(string recordKey,
        CancellationToken cancellationToken = default)
    {
        _currentValuesList.RemoveAll(x => GetKey(x) == recordKey);
        await Save($"record {recordKey} Removed", cancellationToken);
    }

    protected override async ValueTask AddRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        if (!TimeSpan.TryParse(recordKey, CultureInfo.InvariantCulture, out TimeSpan value))
        {
            StShared.WriteErrorLine($"{recordKey} is not valid {CrudName}", true);
            return;
        }

        _currentValuesList.Add(value);
        await Save($"record {recordKey} Added", cancellationToken);
    }

    protected override ValueTask<string?> InputNewRecordName(CancellationToken cancellationToken = default)
    {
        try
        {
            TimeSpan value = Inputer.InputTimeSpan($"New {CrudName}", TimeSpan.Zero);
            return ValueTask.FromResult<string?>(GetKey(value));
        }
        catch (DataInputEscapeException)
        {
            return ValueTask.FromResult<string?>(null);
        }
    }

    public override ValueTask<bool> Save(string message, CancellationToken cancellationToken = default)
    {
        return _parametersManager.Save(_parametersManager.Parameters, message, null, cancellationToken);
    }
}
