using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.LibDataInput;
using AppCliTools.LibMenuInput;
using SystemTools.SystemToolsShared;

namespace TravelGuide.Menu.Visits;

//სურათის ფაილის ამრჩევი: პარამეტრებში მითითებული ფოლდერის ზედა დონეზე მდებარე სურათების სიიდან
//ერთი ფაილის სახელს ირჩევს. სია შექმნის თარიღის კლებადობითაა დალაგებული — ბოლოს გადაღებული სურათი
//სიის თავშია. გადაფურცვლა CliMenuSet-ის ჩაშენებული PageUp/PageDown-ით ხდება
internal static class ImageFileSelector
{
    //CA1861 — მუდმივი მასივი არგუმენტად არ გამოდგება, ამიტომ ცალკე ველია.
    //რეგისტრის უგულებელმყოფელი შედარება: .JPG და .jpg ერთი და იგივეა
    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".heic" };

    //აბრუნებს არჩეული ფაილის სახელს. თუ ასარჩევი არაფერია, ListIsEmptyException იგზავნება —
    //მას CliMenuCommand.Run იჭერს და მენიუში მკაფიო შეტყობინებას აჩვენებს
    public static string SelectFileName(string? imagesFolderPath, ICollection<string> alreadyLinkedFileNames)
    {
        if (string.IsNullOrWhiteSpace(imagesFolderPath))
        {
            throw new ListIsEmptyException("Images Folder Path is not set in parameters");
        }

        if (!Directory.Exists(imagesFolderPath))
        {
            throw new ListIsEmptyException($"Images folder {imagesFolderPath} does not exists");
        }

        var alreadyLinked = new HashSet<string>(alreadyLinkedFileNames, StringComparer.OrdinalIgnoreCase);

        List<FileInfo> imageFiles;
        try
        {
            //ერთი ჩამონათვალით ფაილებიც და მათი თარიღებიც მოდის; ქვეფოლდერებში ჩასვლა არ ხდება
            imageFiles =
            [
                .. new DirectoryInfo(imagesFolderPath).GetFiles()
                    .Where(w => ImageExtensions.Contains(w.Extension) && !alreadyLinked.Contains(w.Name))
                    .OrderByDescending(o => o.CreationTime)
            ];
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
            throw new ListIsEmptyException($"Images folder {imagesFolderPath} could not be read");
        }

        if (imageFiles.Count == 0)
        {
            throw new ListIsEmptyException($"No new image files found in {imagesFolderPath}");
        }

        //ამრჩევის მენიუში მხოლოდ უგასაღებო პუნქტები უნდა იყოს — CliMenuSet გვერდის ზომას მათი
        //რაოდენობით ითვლის და თვითონვე ამატებს PageUp/PageDown-ს, სადაც საჭიროა
        var imageFilesMenuSet = new CliMenuSet();
        foreach (FileInfo imageFile in imageFiles)
        {
            imageFilesMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand(imageFile.Name,
                string.Create(CultureInfo.InvariantCulture, $"{imageFile.CreationTime:yyyy-MM-dd HH:mm}")));
        }

        int selectedId = MenuInputer.InputIdFromMenuList("Image File", imageFilesMenuSet);
        return selectedId < 0 || selectedId >= imageFiles.Count
            ? throw new DataInputException("Selected invalid Image File")
            : imageFiles[selectedId].Name;
    }
}
