﻿using OpenSage.Data.Ini;

namespace OpenSage.Logic.Object;

internal sealed class StealthUpgrade : UpgradeModule
{
    public StealthUpgrade(GameObject gameObject, IGameEngine gameEngine, StealthUpgradeModuleData moduleData)
        : base(gameObject, gameEngine, moduleData)
    {
    }

    internal override void Load(StatePersister reader)
    {
        reader.PersistVersion(1);

        reader.BeginObject("Base");
        base.Load(reader);
        reader.EndObject();
    }
}

/// <summary>
/// Eenables use of <see cref="StealthUpdateModuleData"/> module on this object. Requires
/// <see cref="StealthUpdateModuleData.InnateStealth"/> = No defined in the <see cref="StealthUpdateModuleData"/>
/// module.
/// </summary>
public sealed class StealthUpgradeModuleData : UpgradeModuleData
{
    internal static StealthUpgradeModuleData Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

    private static new readonly IniParseTable<StealthUpgradeModuleData> FieldParseTable = UpgradeModuleData.FieldParseTable
        .Concat(new IniParseTable<StealthUpgradeModuleData>());

    internal override BehaviorModule CreateModule(GameObject gameObject, IGameEngine gameEngine)
    {
        return new StealthUpgrade(gameObject, gameEngine, this);
    }
}
