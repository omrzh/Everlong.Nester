// NOTE: Platform-paired file — the WPF side keeps its own GlobalUsings.wpf.cs.
//
// Exemption from the per-file-import rule, narrow by design: only ALIASES may
// be declared here.
// A single-source file is compiled into both platform packages, so a name it
// spells has to resolve on both sides — the platform types behind these names
// differ, the names do not.  Namespace imports are not aliases and do not
// belong here: they go in the file that needs them, as everywhere else under
// src/.  The alias set differs per platform, so the two files have no section
// in common to keep in step.  The `P` prefix follows the `P/Invoke` precedent:
// it marks a name that stands for a platform type rather than one of ours.

global using PColor = Avalonia.Media.Color;
global using PContentControl = Avalonia.Controls.ContentControl;
global using PHorizontalAlignment = Avalonia.Layout.HorizontalAlignment;
global using PApp = Avalonia.Application;
global using PCanvas = Avalonia.Controls.Canvas;
global using PControl = Avalonia.Controls.Control;
global using PPanel = Avalonia.Controls.Panel;
global using PWindow = Avalonia.Controls.Window;
global using PRect = Avalonia.Rect;
global using PVerticalAlignment = Avalonia.Layout.VerticalAlignment;
global using PVisual = Avalonia.Visual;
global using PWindowState = Avalonia.Controls.WindowState;
