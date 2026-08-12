using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.LibDataInput;
using SystemTools.SystemToolsShared;

namespace TravelGuide.FieldEditors;

//თარიღის ველის რედაქტორი (ApAgent-ის ანალოგიური)
public sealed class DateFieldEditor : FieldEditor<DateTime>
{
    private readonly DateTime _defaultValue;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DateFieldEditor(string propertyName, DateTime defaultValue, bool enterFieldDataOnCreate = false) : base(
        propertyName, enterFieldDataOnCreate)
    {
        _defaultValue = defaultValue;
    }

    public override ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        SetValue(recordForUpdate, Inputer.InputDate(FieldName, GetValue(recordForUpdate, _defaultValue)));
        return ValueTask.CompletedTask;
    }

    public override string GetValueStatus(object? record)
    {
        DateTime val = GetValue(record);
        return val.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public override void SetDefault(ItemData currentItem)
    {
        SetValue(currentItem, _defaultValue);
    }
}
