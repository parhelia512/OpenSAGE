﻿using OpenSage.Data.Ini;

namespace OpenSage.Logic.Object;

[AddedIn(SageGame.CncGeneralsZeroHour)]
public sealed class SpectreGunshipDeploymentUpdate : UpdateModule
{
    public SpectreGunshipDeploymentUpdate(GameObject gameObject, IGameEngine gameEngine) : base(gameObject, gameEngine)
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

        reader.BeginObject("Base");
        base.Load(reader);
        reader.EndObject();

        reader.SkipUnknownBytes(4);
    }
}

[AddedIn(SageGame.CncGeneralsZeroHour)]
public sealed class SpectreGunshipDeploymentUpdateModuleData : BehaviorModuleData
{
    internal static SpectreGunshipDeploymentUpdateModuleData Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

    private static readonly IniParseTable<SpectreGunshipDeploymentUpdateModuleData> FieldParseTable = new IniParseTable<SpectreGunshipDeploymentUpdateModuleData>
    {
        { "SpecialPowerTemplate", (parser, x) => x.SpecialPowerTemplate = parser.ParseAssetReference() },
        { "RequiredScience", (parser, x) => x.RequiredScience = parser.ParseAssetReference() },
        { "GunshipTemplateName", (parser, x) => x.GunshipTemplateName = parser.ParseAssetReference() },
        { "AttackAreaRadius", (parser, x) => x.AttackAreaRadius = parser.ParseInteger() },
        { "CreateLocation", (parser, x) => x.CreateLocation = parser.ParseEnum<OCLCreateLocation>() },
    };

    public string SpecialPowerTemplate { get; private set; }
    public string RequiredScience { get; private set; }
    public string GunshipTemplateName { get; private set; }
    public int AttackAreaRadius { get; private set; }
    public OCLCreateLocation CreateLocation { get; private set; }

    internal override BehaviorModule CreateModule(GameObject gameObject, IGameEngine gameEngine)
    {
        return new SpectreGunshipDeploymentUpdate(gameObject, gameEngine);
    }
}
