﻿using System;
using System.Collections.Generic;
using System.Linq;
using OpenSage.Mods.Bfme;
using OpenSage.Mods.Bfme2;
using OpenSage.Mods.Generals;

namespace OpenSage.Mods.BuiltIn;

public static class GameDefinition
{
    private static readonly Dictionary<SageGame, IGameDefinition> Games;

    public static IEnumerable<IGameDefinition> All => Games.Values;
    public static IGameDefinition FromGame(SageGame game) => Games[game];

    public static bool TryGetByName(string name, out IGameDefinition? definition)
    {
        // TODO: Use a short identifier defined in IGameDefinition instead of stringified SageGame
        definition = All.FirstOrDefault(def =>
            string.Equals(def.Game.ToString(), name, StringComparison.InvariantCultureIgnoreCase));
        return definition != null;
    }

    static GameDefinition()
    {
        Games = new Dictionary<SageGame, IGameDefinition>
        {
            [SageGame.CncGenerals] = GeneralsDefinition.Instance,
            [SageGame.CncGeneralsZeroHour] = GeneralsZeroHourDefinition.Instance,
            [SageGame.Bfme] = BfmeDefinition.Instance,
            [SageGame.Bfme2] = Bfme2Definition.Instance,
            [SageGame.Bfme2Rotwk] = Bfme2RotwkDefinition.Instance,
        };
    }
}
