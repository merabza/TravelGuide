using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.LibDataInput;
using AppCliTools.LibMenuInput;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;

namespace TravelGuide.FieldEditors;

//ადგილის სტატუსის ველის რედაქტორი: ასარჩევად მხოლოდ ბაზაში შესანახი სტატუსები გამოდის —
//შუალედური Opening/Opened/Analysing ბაზაში არასდროს იწერება და რედაქტორითაც არ უნდა ჩაიწეროს.
//ახალი ჩანაწერის ნაგულისხმევი სტატუსი Analysed-ია, რომ ხელით შექმნილ ადგილს ქროულერი არ შეეხოს
public sealed class PlaceStateFieldEditor : FieldEditor<EState>
{
    private static readonly List<EState> PersistedStates =
        [EState.New, EState.Analysed, EState.NotAttraction, EState.DownloadError, EState.Duplicate];

    public PlaceStateFieldEditor(string propertyName, bool enterFieldDataOnCreate = false) : base(propertyName,
        enterFieldDataOnCreate)
    {
    }

    public override ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        EState current = GetValue(recordForUpdate);
        var selectFromListInput = new SelectFromListInput(FieldName, [.. PersistedStates.Select(s => s.ToString())],
            current.ToString());
        if (!selectFromListInput.DoInput() || selectFromListInput.Text is null)
        {
            throw new DataInputException($"Input {FieldName} Escaped");
        }

        SetValue(recordForUpdate, Enum.Parse<EState>(selectFromListInput.Text));
        return ValueTask.CompletedTask;
    }

    public override void SetDefault(ItemData currentItem)
    {
        SetValue(currentItem, EState.Analysed);
    }
}
