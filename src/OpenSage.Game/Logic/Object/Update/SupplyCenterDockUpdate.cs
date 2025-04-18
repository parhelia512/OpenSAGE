﻿using OpenSage.Content;
using OpenSage.Data.Ini;
using OpenSage.Mathematics;

namespace OpenSage.Logic.Object;

public class SupplyCenterDockUpdate : DockUpdate
{
    private SupplyCenterDockUpdateModuleData _moduleData;

    internal SupplyCenterDockUpdate(GameObject gameObject, IGameEngine gameEngine, SupplyCenterDockUpdateModuleData moduleData)
        : base(gameObject, gameEngine, moduleData)
    {
        _moduleData = moduleData;
    }

    public int DumpBoxes(AssetStore assetStore, ref int numBoxes, int additionalAmountPerBox)
    {
        var gameData = assetStore.GameData.Current;
        var amountPerBox = (gameData.ValuePerSupplyBox + additionalAmountPerBox) * _moduleData.ValueMultiplier;

        if (_moduleData.BonusScience != null)
        {
            var bonusUpgradeDefinition = assetStore.Upgrades.GetByName(_moduleData.BonusScience);
            if (GameObject.HasUpgrade(bonusUpgradeDefinition))
            {
                amountPerBox *= _moduleData.BonusScienceMultiplier;
            }
        }

        var amount = (int)(numBoxes * amountPerBox * GameObject.ProductionModifier);
        GameObject.Owner.BankAccount.Deposit((uint)amount);
        numBoxes = 0;

        return amount;
    }

    public override UpdateSleepTime Update()
    {
        return base.Update();
    }

    internal override void Load(StatePersister reader)
    {
        reader.PersistVersion(1);

        reader.BeginObject("Base");
        base.Load(reader);
        reader.EndObject();
    }
}

public sealed class SupplyCenterDockUpdateModuleData : DockUpdateModuleData
{
    internal static SupplyCenterDockUpdateModuleData Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

    private static new readonly IniParseTable<SupplyCenterDockUpdateModuleData> FieldParseTable = DockUpdateModuleData.FieldParseTable
        .Concat(new IniParseTable<SupplyCenterDockUpdateModuleData>
        {
            { "GrantTemporaryStealth", (parser, x) => x.GrantTemporaryStealth = parser.ParseInteger() },
            { "BonusScience", (parser, x) => x.BonusScience = parser.ParseString() },
            { "BonusScienceMultiplier", (parser, x) => x.BonusScienceMultiplier = parser.ParsePercentage() },
            { "ValueMultiplier", (parser, x) => x.ValueMultiplier = parser.ParseFloat() }
        });

    [AddedIn(SageGame.CncGeneralsZeroHour)]
    public int GrantTemporaryStealth { get; private set; }

    [AddedIn(SageGame.Bfme)]
    public string BonusScience { get; private set; }

    [AddedIn(SageGame.Bfme)]
    public Percentage BonusScienceMultiplier { get; private set; }

    [AddedIn(SageGame.Bfme)]
    public float ValueMultiplier { get; private set; } = 1.0f;

    internal override BehaviorModule CreateModule(GameObject gameObject, IGameEngine gameEngine)
    {
        return new SupplyCenterDockUpdate(gameObject, gameEngine, this);
    }
}
