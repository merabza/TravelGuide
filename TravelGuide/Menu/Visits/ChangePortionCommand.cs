using System;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;

namespace TravelGuide.Menu.Visits;

//გადაფურცვლის ბრძანება: არჩევისას მშობელი მენიუს პორციის ნომერს ცვლის და ReloadWithoutPause-ით
//მენიუს თავიდან აწყობინებს — GetSubMenu ხელახლა გამოიძახება და ახალი პორცია ბაზიდან ჩაიტვირთება.
//CliMenuSet-ის ჩაშენებული PageUp/PageDown აქ არ გამოდგება — ის მხოლოდ უკვე ჩატვირთული სიის
//ნაჩვენებ გვერდს ცვლის და მენიუს ხელახლა არ აწყობს
public sealed class ChangePortionCommand : CliMenuCommand
{
    private readonly Action _changePortion;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ChangePortionCommand(string name, Action changePortion) : base(name, EMenuAction.ReloadWithoutPause,
        EMenuAction.ReloadWithoutPause)
    {
        _changePortion = changePortion;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        _changePortion();
        return ValueTask.FromResult(true);
    }
}
