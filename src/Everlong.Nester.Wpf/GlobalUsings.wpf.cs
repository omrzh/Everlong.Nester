// NOTE: Platform-paired file — the Avalonia side keeps its own GlobalUsings.avalonia.cs.
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

global using PColor = System.Windows.Media.Color;
global using PContentControl = System.Windows.Controls.ContentControl;
global using PCornerRadius = System.Windows.CornerRadius;
global using PHorizontalAlignment = System.Windows.HorizontalAlignment;
global using PApp = System.Windows.Application;
global using PCanvas = System.Windows.Controls.Canvas;
global using PControl = System.Windows.FrameworkElement;
global using PPanel = System.Windows.Controls.Panel;
global using PWindow = System.Windows.Window;
global using PRect = System.Windows.Rect;
global using PThickness = System.Windows.Thickness;
global using PVerticalAlignment = System.Windows.VerticalAlignment;
global using PVisual = System.Windows.UIElement;
global using PWindowState = System.Windows.WindowState;
