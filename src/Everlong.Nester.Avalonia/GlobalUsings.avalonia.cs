// NOTE: Platform-paired file — the WPF side keeps its own GlobalUsings.wpf.cs.
//
// HARD RULE 5 exemption, narrow by design: only ALIASES may be declared here.
// A single-source file is compiled into both platform packages, so a name it
// spells has to resolve on both sides — the platform types behind these names
// differ, the names do not.  Namespace imports are not aliases and do not
// belong here: they go in the file that needs them, as everywhere else under
// src/.  The alias set differs per platform, so the two files have no section
// in common to keep in step.

global using Color = Avalonia.Media.Color;
global using ContentControl = Avalonia.Controls.ContentControl;
global using HorizontalAlignment = Avalonia.Layout.HorizontalAlignment;
global using PlatformApp = Avalonia.Application;
global using PlatformCanvas = Avalonia.Controls.Canvas;
global using PlatformControl = Avalonia.Controls.Control;
global using PlatformGrid = Avalonia.Controls.Grid;
global using PlatformWindow = Avalonia.Controls.Window;
global using Rect = Avalonia.Rect;
global using VerticalAlignment = Avalonia.Layout.VerticalAlignment;
global using Visual = Avalonia.Visual;
global using WindowState = Avalonia.Controls.WindowState;
