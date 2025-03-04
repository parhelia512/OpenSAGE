﻿using OpenSage.Data.Ini;
using OpenSage.Logic.Object.Damage;

namespace OpenSage.Logic.Object;

public sealed class BaseRegenerateUpdate : UpdateModule, IDamageModule
{
    private readonly GameObject _gameObject;
    private readonly GameContext _context;

    protected override LogicFrameSpan FramesBetweenUpdates => LogicFrameSpan.OneSecond;

    internal BaseRegenerateUpdate(GameObject gameObject, GameContext context)
    {
        _gameObject = gameObject;
        _context = context;
        SetNextUpdateFrame(new LogicFrame(uint.MaxValue));
    }

    /// <summary>
    /// Increments the frame after which healing is allowed.
    /// </summary>
    public void OnDamage(in DamageData damageData)
    {
        var currentFrame = _context.GameLogic.CurrentFrame;
        SetNextUpdateFrame(currentFrame + _context.AssetLoadContext.AssetStore.GameData.Current.BaseRegenDelay);
    }

    private protected override void RunUpdate(BehaviorUpdateContext context)
    {
        _gameObject.HealDirectly(_context.AssetLoadContext.AssetStore.GameData.Current.BaseRegenHealthPercentPerSecond);

        if (_gameObject.IsFullHealth)
        {
            SetNextUpdateFrame(new LogicFrame(uint.MaxValue));
        }
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
/// Forces object to auto-repair itself over time. Parameters are defined in GameData.INI
/// through <see cref="GameData.BaseRegenHealthPercentPerSecond"/> and
/// <see cref="GameData.BaseRegenDelay"/>.
/// </summary>
public sealed class BaseRegenerateUpdateModuleData : UpdateModuleData
{
    internal static BaseRegenerateUpdateModuleData Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

    private static readonly IniParseTable<BaseRegenerateUpdateModuleData> FieldParseTable = new IniParseTable<BaseRegenerateUpdateModuleData>();

    internal override BehaviorModule CreateModule(GameObject gameObject, GameContext context)
    {
        return new BaseRegenerateUpdate(gameObject, context);
    }
}
