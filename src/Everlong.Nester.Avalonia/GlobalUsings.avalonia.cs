// NOTE: Platform-paired file — the WPF side keeps its own GlobalUsings.wpf.cs.
// The shared using block (top section) MUST stay identical in both files;
// only the platform alias block below differs.  Edit both sides together.
//
// HARD RULE 5 exemption, deliberate: this project keeps its imports here rather
// than per file.  The platform packages are one body of source compiled twice —
// single-source files are shared into Everlong.Nester.Wpf by <Compile Include>
// plus Link — so a file that spells its own imports cannot see the same
// namespaces on both sides, and the platform aliases below exist to make that
// one body compile against two UI frameworks.  Every other project under src/
// still imports per file.

global using Everlong.Nester.Diagnostics;
global using System;
global using Color = Avalonia.Media.Color;
global using ContentControl = Avalonia.Controls.ContentControl;
global using PlatformControl = Avalonia.Controls.Control;
global using PlatformWindow = Avalonia.Controls.Window;
