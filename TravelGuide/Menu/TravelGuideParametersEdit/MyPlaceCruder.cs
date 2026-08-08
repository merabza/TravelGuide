using System.Collections.Generic;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.FieldEditors;
using DoTravelGuide.Models;
using ParametersManagement.LibParameters;

namespace TravelGuide.Menu.TravelGuideParametersEdit;

public sealed class MyPlaceCruder : ParCruder<MyPlace>
{
    public MyPlaceCruder(IParametersManager parametersManager, Dictionary<string, MyPlace> currentValuesDictionary) :
        base(parametersManager, currentValuesDictionary, "My Place", "My Places")
    {
        FieldEditors.Add(new DoubleFieldEditor(nameof(MyPlace.Latitude), 0, true));
        FieldEditors.Add(new DoubleFieldEditor(nameof(MyPlace.Longitude), 0, true));
    }

    public static MyPlaceCruder Create(IParametersManager parametersManager)
    {
        var parameters = (TravelGuideParameters)parametersManager.Parameters;
        return new MyPlaceCruder(parametersManager, parameters.MyPlaces);
    }
}
