﻿using OpenSage.Gui.Apt;
using OpenSage.Gui.Apt.ActionScript;

namespace OpenSage.Mods.Bfme.Gui;

[AptCallbacks(SageGame.Bfme, SageGame.Bfme2, SageGame.Bfme2Rotwk)]
static class AptSkirmish
{
    // Called after the initialization has been performed
    public static void OnInitialized(string param, ActionContext context, AptWindow window, IGame game)
    {
    }

    public static void Exit(string param, ActionContext context, AptWindow window, IGame game)
    {
        var aptWindow = game.LoadAptWindow("MainMenu.apt");
        game.Scene2D.AptWindowManager.QueryTransition(aptWindow);
    }

    public static void DisableComponents(string param, ActionContext context, AptWindow window, IGame game)
    {
        // do we need to hide the buttons from MainMenu here?
    }
}
