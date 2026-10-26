# Waits for an EditMode run started by RunEditModeTests.cs and prints its result.
# Unity's Test Runner opens a modal "Scene(s) Have Been Modified" dialog at the start of a run even when nothing
# changed, which freezes the Editor. RunEditModeTests.cs has already refused to start if the scene really differs
# from disk, so clicking Save here writes the same file.
# Start it in the background, then call execute_script on RunEditModeTests.cs.
param([int]$TimeoutSeconds = 300)

Add-Type @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class UnityDialog {
    delegate bool P(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumWindows(P p, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, P p, IntPtr l);
    [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    static string Text(IntPtr h) { var s = new StringBuilder(256); GetWindowText(h, s, 256); return s.ToString(); }
    public static bool ClickSaveIfShown() {
        IntPtr dlg = IntPtr.Zero;
        EnumWindows((h, l) => { if (IsWindowVisible(h) && Text(h) == "Scene(s) Have Been Modified") dlg = h; return true; }, IntPtr.Zero);
        if (dlg == IntPtr.Zero) return false;
        IntPtr save = IntPtr.Zero;
        EnumChildWindows(dlg, (h, l) => { if (Text(h) == "Save") save = h; return true; }, IntPtr.Zero);
        if (save == IntPtr.Zero) return false;
        SendMessage(save, 0x00F5, IntPtr.Zero, IntPtr.Zero);   // BM_CLICK
        return true;
    }
}
'@

$results = Join-Path $PSScriptRoot "..\..\Temp\test-results.txt"
if (Test-Path $results) { Remove-Item $results }
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while ((Get-Date) -lt $deadline) {
    if ([UnityDialog]::ClickSaveIfShown()) { "clicked Save on the Test Runner's dialog" }
    if (Test-Path $results) {
        Start-Sleep -Milliseconds 300
        $lines = Get-Content $results
        $lines[0]
        $lines | Select-Object -Skip 1 | Where-Object { $_ -notlike "Passed*" }
        exit 0
    }
    Start-Sleep -Milliseconds 500
}
"timed out after $TimeoutSeconds s with no results"
exit 1
