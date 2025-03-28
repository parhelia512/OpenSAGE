﻿
namespace OpenSage.Diagnostics;

internal sealed class CameraView : DiagnosticView
{
    public CameraView(DiagnosticViewContext context)
        : base(context)
    {

    }

    public override string DisplayName { get; } = "Camera";

    protected override void DrawOverride(ref bool isGameViewFocused)
    {
        var tacticalView = Context.Game.Scene3D.TacticalView;
        tacticalView.DrawInspector();
    }
}
