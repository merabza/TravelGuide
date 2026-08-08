using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using ParametersManagement.LibParameters;

namespace TravelGuide.Menu.TravelGuideParametersEdit;

public sealed class MyPlaceNameFieldEditor : FieldEditor<string>
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public MyPlaceNameFieldEditor(string propertyName, IParametersManager parametersManager,
        bool enterFieldDataOnCreate = false) : base(propertyName, enterFieldDataOnCreate)
    {
        _parametersManager = parametersManager;
    }

    public override async ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        var myPlaceCruder = MyPlaceCruder.Create(_parametersManager);
        SetValue(recordForUpdate,
            await myPlaceCruder.GetNameWithPossibleNewName(FieldName, GetValue(recordForUpdate), null, true,
                cancellationToken));
    }
}
