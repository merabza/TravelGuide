using System.Runtime.InteropServices;

namespace TravelGuide.Menu.Visits;

//ძველი conhost კონსოლისთვის ANSI/VT მიმდევრობების ჩართვას Win32 ფუნქციები სჭირდება.
//CA1060 მოითხოვს, რომ P/Invoke მეთოდები ზუსტად ამ სახელის მქონე კლასში იდოს
internal static class NativeMethods
{
    internal const int StdOutputHandle = -11;
    internal const uint EnableVirtualTerminalProcessing = 4;

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern nint GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetConsoleMode(nint hConsoleHandle, uint dwMode);
}
