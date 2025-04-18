﻿using OpenSage.Data.Ini;

namespace OpenSage.Logic.Object;

public sealed class AssistedTargetingUpdate : UpdateModule
{
    public AssistedTargetingUpdate(GameObject gameObject, IGameEngine gameEngine) : base(gameObject, gameEngine)
    {
    }

    public override UpdateSleepTime Update()
    {
        // TODO(Port): Use correct value.
        return UpdateSleepTime.None;
    }

    internal override void Load(StatePersister reader)
    {
        reader.PersistVersion(1);

        base.Load(reader);
    }
}

/// <summary>
/// Allows weapons (or defense) to relay with a similar weapon (or defense) within its range.
/// </summary>
public sealed class AssistedTargetingUpdateModuleData : UpdateModuleData
{
    internal static AssistedTargetingUpdateModuleData Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

    private static readonly IniParseTable<AssistedTargetingUpdateModuleData> FieldParseTable = new IniParseTable<AssistedTargetingUpdateModuleData>
    {
        { "AssistingClipSize", (parser, x) => x.AssistingClipSize = parser.ParseInteger() },
        { "AssistingWeaponSlot", (parser, x) => x.AssistingWeaponSlot = parser.ParseEnum<WeaponSlot>() },
        { "LaserFromAssisted", (parser, x) => x.LaserFromAssisted = parser.ParseAssetReference() },
        { "LaserToTarget", (parser, x) => x.LaserToTarget = parser.ParseAssetReference() }
    };

    public int AssistingClipSize { get; private set; } = 1;
    public WeaponSlot AssistingWeaponSlot { get; private set; } = WeaponSlot.Primary;
    public string LaserFromAssisted { get; private set; }
    public string LaserToTarget { get; private set; }

    internal override BehaviorModule CreateModule(GameObject gameObject, IGameEngine gameEngine)
    {
        return new AssistedTargetingUpdate(gameObject, gameEngine);
    }
}
