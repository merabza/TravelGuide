using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.LibDataInput;
using AppCliTools.LibMenuInput;
using SystemTools.SystemToolsShared;
using TravelGuideCore.Domain.PlaceModels;
using TravelGuideCore.Domain.UrlModels;

namespace TravelGuide.FieldEditors;

//ადგილის მისამართის სტატუსის ველის რედაქტორი: სტატუსი ადგილს კი არა, მის მისამართს აქვს (UrlModel.State), ამიტომ
//რედაქტორი ჩანაწერის (PlaceModel) UrlNavigation-ზე მუშაობს და საბაზისო კლასის რეფლექსიით ველის ძებნას არ იყენებს;
//უმისამართო (ხელით შეყვანილ) ადგილს სტატუსი არ აქვს — მისთვის ველი არ იკითხება და ცარიელი ჩანს.
//ასარჩევად მხოლოდ ბაზაში შესანახი სტატუსები გამოდის — შუალედური Opening/Opened/Analysing ბაზაში არასდროს
//იწერება და რედაქტორითაც არ უნდა ჩაიწეროს. ახალი მისამართის ნაგულისხმევი სტატუსი Analysed-ია, რომ ხელით
//შექმნილ ადგილს ქროულერი არ შეეხოს
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
        if (recordForUpdate is not PlaceModel { UrlNavigation: { } urlModel })
        {
            return ValueTask.CompletedTask;
        }

        var selectFromListInput = new SelectFromListInput(FieldName, [.. PersistedStates.Select(s => s.ToString())],
            urlModel.State.ToString());
        if (!selectFromListInput.DoInput() || selectFromListInput.Text is null)
        {
            throw new DataInputException($"Input {FieldName} Escaped");
        }

        urlModel.State = Enum.Parse<EState>(selectFromListInput.Text);
        return ValueTask.CompletedTask;
    }

    public override string GetValueStatus(object? record)
    {
        return record is PlaceModel { UrlNavigation: { } urlModel } ? urlModel.State.ToString() : string.Empty;
    }

    public override void SetDefault(ItemData currentItem)
    {
        if (currentItem is PlaceModel { UrlNavigation: { } urlModel })
        {
            urlModel.State = EState.Analysed;
        }
    }
}
