#requires -Version 7
<#
.SYNOPSIS
  Launches a Nester template and, on a key press, simulates an OS
  session-ending request against it — no machine shutdown involved.

.DESCRIPTION
  Builds and starts the selected template without waiting for it, waits for a
  key, then sends WM_QUERYENDSESSION to the platform's hidden message window:

    WPF      Application's parking window     class  HwndWrapper[...]
    Avalonia Win32Platform's message window   class  AvaloniaMessageWindow ...

  The template's own window is NOT the target: neither Window.WndProc handles
  WM_QUERYENDSESSION — the platform hook does, and it lives on the hidden
  window.  WPF matches several HwndWrapper[...] windows (real windows share the
  class); only the parking one carries the hook, the rest return TRUE through
  DefWindowProc.

.PARAMETER Template
  Which template to test.  Omitted, the script asks interactively.

.PARAMETER Reason
  Shutdown (lParam 0) or Logoff (ENDSESSION_LOGOFF).  Defaults to Shutdown.

.NOTES
  Run at the same or higher integrity than the target — UIPI blocks a
  lower-integrity sender.  SendMessage blocks until the app finished handling
  the message, including any modal prompt a guard raises.
#>
[CmdletBinding()]
param(
  [ValidateSet('wpf', 'avalonia')]
  [string]$Template,

  [ValidateSet('Shutdown', 'Logoff')]
  [string]$Reason = 'Shutdown'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

# Testable templates: windowed hosts whose platform raises a session ending.
$Templates = [ordered]@{
  wpf      = 'examples/Template.Wpf/Template.Wpf.csproj'
  avalonia = 'examples/Template.Avalonia.Desktop/Template.Avalonia.Desktop.csproj'
}

if (-not $Template)
{
  $names = @($Templates.Keys)
  Write-Host 'Select a template to test:'
  for ($i = 0; $i -lt $names.Count; $i++)
  {
    Write-Host ("  {0}) {1,-8} {2}" -f ($i + 1), $names[$i], $Templates[$names[$i]])
  }

  $choice = Read-Host ("Template [1-{0}]" -f $names.Count)
  if ([string]::IsNullOrWhiteSpace($choice))
  {
    $choice = '1'
  }

  $index = 0
  if (-not [int]::TryParse($choice, [ref]$index) -or $index -lt 1 -or $index -gt $names.Count)
  {
    Write-Host "Not a choice: $choice" -ForegroundColor Red
    exit 2
  }

  $Template = $names[$index - 1]
}

$project = Join-Path $repoRoot $Templates[$Template]
if (-not (Test-Path $project))
{
  Write-Host "Project not found: $project" -ForegroundColor Red
  exit 1
}

Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class SessionEndingProbe
{
  private const uint WM_QUERYENDSESSION = 0x0011;
  private const long ENDSESSION_LOGOFF = 0x80000000L;

  private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

  [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
  [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hWnd, StringBuilder sb, int max);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

  private static bool IsTarget(string cls)
    => cls.StartsWith("HwndWrapper[", StringComparison.Ordinal)
       || cls.StartsWith("AvaloniaMessageWindow", StringComparison.Ordinal);

  public static List<string> Targets(int pid)
  {
    var found = new List<string>();
    EnumWindows((hWnd, _) =>
    {
      GetWindowThreadProcessId(hWnd, out uint wpid);
      if ((int)wpid != pid)
        return true;

      var sb = new StringBuilder(256);
      GetClassName(hWnd, sb, sb.Capacity);
      string cls = sb.ToString();
      if (IsTarget(cls))
        found.Add(cls);
      return true;
    }, IntPtr.Zero);
    return found;
  }

  public static int Send(int pid, bool logoff)
  {
    long reason = logoff ? ENDSESSION_LOGOFF : 0L;
    int sent = 0;

    EnumWindows((hWnd, _) =>
    {
      GetWindowThreadProcessId(hWnd, out uint wpid);
      if ((int)wpid != pid)
        return true;

      var sb = new StringBuilder(256);
      GetClassName(hWnd, sb, sb.Capacity);
      if (!IsTarget(sb.ToString()))
        return true;

      SendMessage(hWnd, WM_QUERYENDSESSION, IntPtr.Zero, new IntPtr(reason));
      sent++;
      return true;
    }, IntPtr.Zero);

    return sent;
  }
}
'@

# ── build + launch (not waiting for it) ──
$name = [IO.Path]::GetFileNameWithoutExtension($project)
$projectDir = Split-Path -Parent $project

Write-Host "Building $name ..."
dotnet build $project -v q --nologo | Out-Host

$exe = Get-ChildItem -Path (Join-Path $projectDir 'bin/Debug') -Recurse -Filter "$name.exe" `
         -ErrorAction SilentlyContinue |
       Select-Object -First 1 -ExpandProperty FullName
if (-not $exe -or -not (Test-Path $exe))
{
  Write-Host "Built exe not found under $projectDir/bin/Debug" -ForegroundColor Red
  exit 1
}

$app = Start-Process -FilePath $exe -PassThru
Write-Host "Launched $name (PID $($app.Id)) — not waiting for it."

# Wait for the platform's hidden message window.
$deadline = [Environment]::TickCount64 + 10000
while ([Environment]::TickCount64 -lt $deadline -and $app.HasExited -eq $false)
{
  if ([SessionEndingProbe]::Targets($app.Id).Count -gt 0)
  {
    break
  }

  Start-Sleep -Milliseconds 100
}

if ($app.HasExited)
{
  Write-Host "The app exited before the probe could reach it (code $($app.ExitCode))." -ForegroundColor Red
  exit 1
}

$targets = [SessionEndingProbe]::Targets($app.Id)
if ($targets.Count -eq 0)
{
  Write-Host 'No hidden message window appeared within 10s.' -ForegroundColor Red
  exit 1
}

Write-Host 'Message windows:'
foreach ($cls in $targets)
{
  Write-Host "  $cls"
}

# ── wait for the key ──
Write-Host "Press any key to send WM_QUERYENDSESSION ($Reason) ..."
$null = [Console]::ReadKey($true)
Write-Host ''

$sent = [SessionEndingProbe]::Send($app.Id, $Reason -eq 'Logoff')
Write-Host "Sent to $sent window(s). The app is still running — close it yourself."
exit 0
