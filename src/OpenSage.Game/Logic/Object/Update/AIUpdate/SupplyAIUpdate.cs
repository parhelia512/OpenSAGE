﻿using System.Numerics;
using OpenSage.Audio;
using OpenSage.Content;
using OpenSage.Data.Ini;

namespace OpenSage.Logic.Object;

public abstract class SupplyAIUpdate : AIUpdate
{
    public GameObject CurrentSupplyTarget;
    private SupplyCenterDockUpdate _currentTargetDockUpdate;
    public SupplyGatherStates SupplyGatherState;
    protected SupplyGatherStates SupplyGatherStateToResume;

    public enum SupplyGatherStates
    {
        Default,
        SearchingForSupplySource,
        ApproachingSupplySource,
        RequestingSupplies,
        GatheringSupplies,
        PickingUpSupplies,
        SearchingForSupplyTarget,
        ApproachingSupplyTarget,
        EnqueuedAtSupplyTarget,
        StartDumpingSupplies,
        DumpingSupplies,
        FinishedDumpingSupplies
    }

    internal override SupplyAIUpdateModuleData ModuleData { get; }

    public GameObject CurrentSupplySource { get; set; }
    private SupplyWarehouseDockUpdate _currentSourceDockUpdate;
    protected LogicFrame _waitUntil;
    protected int _numBoxes;
    public bool CarryingSupplies => _numBoxes > 0;

    public int SupplyWarehouseScanDistance => ModuleData.SupplyWarehouseScanDistance;

    protected virtual int GetAdditionalValuePerSupplyBox(ScopedAssetCollection<UpgradeTemplate> upgrades) => 0;

    internal SupplyAIUpdate(GameObject gameObject, IGameEngine gameEngine, SupplyAIUpdateModuleData moduleData) : base(gameObject, gameEngine, moduleData)
    {
        ModuleData = moduleData;
        SupplyGatherState = SupplyGatherStates.Default;
        // todo: this is not always the case - workers produced from a command center do not go looking for supplies
        SupplyGatherStateToResume = SupplyGatherStates.SearchingForSupplySource;
        _numBoxes = 0;
    }

    internal override void SetTargetPoint(Vector3 targetPoint)
    {
        SupplyGatherStateToResume = SupplyGatherState;
        SupplyGatherState = SupplyGatherStates.Default;
        ClearConditionFlags();
        base.SetTargetPoint(targetPoint);
    }

    internal virtual void ClearConditionFlags()
    {
        GameObject.ModelConditionFlags.Set(ModelConditionFlag.Docking, false);
    }

    internal virtual float GetHarvestActivationRange() => 0.0f;
    internal virtual LogicFrameSpan GetPreparationTime() => LogicFrameSpan.Zero;

    internal virtual bool SupplySourceHasBoxes(SupplyWarehouseDockUpdate dockUpdate, GameObject supplySource)
    {
        return dockUpdate?.HasBoxes() ?? false;
    }

    internal virtual void GetBox()
    {
        if (_currentSourceDockUpdate?.GetBox() == true && !_currentSourceDockUpdate.HasBoxes())
        {
            GameEngine.AudioSystem.PlayAudioEvent(GameObject, ModuleData.SuppliesDepletedVoice.Value);
        }
    }

    internal virtual void SetGatheringConditionFlags()
    {

    }

    internal virtual LogicFrameSpan GetPickingUpTime() => LogicFrameSpan.Zero;

    internal virtual void SetActionConditionFlags()
    {

    }

    internal virtual void ClearActionConditionFlags()
    {

    }

    internal virtual GameObject FindClosestSupplyWarehouse()
    {
        return GameObject.Owner.SupplyManager.FindClosestSupplyWarehouse(GameObject);
    }

    private GameObject FindClosestSupplyCenter()
    {
        return GameObject.Owner.SupplyManager.FindClosestSupplyCenter(GameObject);
    }

    public override UpdateSleepTime Update()
    {
        var sleepTime = base.Update();

        var isMoving = GameObject.ModelConditionFlags.Get(ModelConditionFlag.Moving);

        switch (SupplyGatherState)
        {
            case SupplyGatherStates.SearchingForSupplySource:
                if (isMoving)
                {
                    break;
                }

                if (CurrentSupplySource == null
                    || (_currentSourceDockUpdate != null && !_currentSourceDockUpdate.HasBoxes()))
                {
                    CurrentSupplySource = FindClosestSupplyWarehouse();
                }

                if (CurrentSupplySource == null)
                {
                    break;
                }

                _currentSourceDockUpdate = CurrentSupplySource.FindBehavior<SupplyWarehouseDockUpdate>();

                var direction = Vector3.Normalize(CurrentSupplySource.Translation - GameObject.Translation);

                SetTargetPoint(CurrentSupplySource.Translation - direction * GetHarvestActivationRange());
                SupplyGatherState = SupplyGatherStates.ApproachingSupplySource;
                break;

            case SupplyGatherStates.ApproachingSupplySource:
                if (!isMoving)
                {
                    SupplyGatherState = SupplyGatherStates.RequestingSupplies;
                    GameObject.ModelConditionFlags.Set(ModelConditionFlag.Docking, true);
                }
                break;

            case SupplyGatherStates.RequestingSupplies:
                var boxesAvailable = SupplySourceHasBoxes(_currentSourceDockUpdate, CurrentSupplySource);

                if (!boxesAvailable)
                {
                    CurrentSupplySource = null;
                    if (_numBoxes == 0)
                    {
                        GameObject.ModelConditionFlags.Set(ModelConditionFlag.Docking, false);
                        SupplyGatherState = SupplyGatherStates.SearchingForSupplySource;
                        break;
                    }
                }
                else if (_numBoxes < ModuleData.MaxBoxes)
                {
                    GetBox();
                    var waitTime = ModuleData.SupplyWarehouseActionDelay + GetPreparationTime();
                    _waitUntil = GameEngine.GameLogic.CurrentFrame + waitTime;
                    SupplyGatherState = SupplyGatherStates.GatheringSupplies;
                    SetGatheringConditionFlags();
                    break;
                }

                GameObject.ModelConditionFlags.Set(ModelConditionFlag.Docking, false);
                SetActionConditionFlags();
                _waitUntil = GameEngine.GameLogic.CurrentFrame + GetPickingUpTime();
                SupplyGatherState = SupplyGatherStates.PickingUpSupplies;
                break;

            case SupplyGatherStates.GatheringSupplies:
                if (GameEngine.GameLogic.CurrentFrame >= _waitUntil)
                {
                    _numBoxes++;
                    GameObject.Supply = _numBoxes;
                    GameObject.ModelConditionFlags.Set(ModelConditionFlag.Carrying, true);
                    SupplyGatherState = SupplyGatherStates.RequestingSupplies;
                }
                break;

            case SupplyGatherStates.PickingUpSupplies:
                if (GameEngine.GameLogic.CurrentFrame >= _waitUntil)
                {
                    SupplyGatherState = SupplyGatherStates.SearchingForSupplyTarget;
                    ClearActionConditionFlags();
                }
                break;

            case SupplyGatherStates.SearchingForSupplyTarget:
                if (CurrentSupplyTarget == null)
                {
                    CurrentSupplyTarget = FindClosestSupplyCenter();
                }

                if (CurrentSupplyTarget == null)
                {
                    break;
                }

                _currentTargetDockUpdate = CurrentSupplyTarget.FindBehavior<SupplyCenterDockUpdate>();

                if (!_currentTargetDockUpdate.CanApproach())
                {
                    break;
                }

                SetTargetPoint(_currentTargetDockUpdate.GetApproachTargetPosition(this));
                SupplyGatherState = SupplyGatherStates.ApproachingSupplyTarget;
                break;

            case SupplyGatherStates.ApproachingSupplyTarget:
                if (!isMoving)
                {
                    SupplyGatherState = SupplyGatherStates.EnqueuedAtSupplyTarget;
                }
                break;

            case SupplyGatherStates.EnqueuedAtSupplyTarget:
                // wait until the DockUpdate moves us forward
                break;

            case SupplyGatherStates.StartDumpingSupplies:
                GameObject.ModelConditionFlags.Set(ModelConditionFlag.Docking, true);
                SupplyGatherState = SupplyGatherStates.DumpingSupplies;
                // todo: this might not be entirely accurate since partial loads can be deposited if unloading is manually aborted early
                _waitUntil = GameEngine.GameLogic.CurrentFrame + ModuleData.SupplyCenterActionDelay * _numBoxes;
                break;

            case SupplyGatherStates.DumpingSupplies:
                if (GameEngine.GameLogic.CurrentFrame >= _waitUntil)
                {
                    SupplyGatherState = SupplyGatherStates.FinishedDumpingSupplies;

                    var assetStore = GameEngine.AssetLoadContext.AssetStore;
                    var bonusAmountPerBox = GetAdditionalValuePerSupplyBox(assetStore.Upgrades);
                    var amountDeposited = _currentTargetDockUpdate.DumpBoxes(assetStore, ref _numBoxes, bonusAmountPerBox);
                    GameObject.ActiveCashEvent = new CashEvent(amountDeposited, GameObject.Owner.Color);

                    GameObject.Supply = _numBoxes;
                    GameObject.ModelConditionFlags.Set(ModelConditionFlag.Docking, false);
                    GameObject.ModelConditionFlags.Set(ModelConditionFlag.Carrying, false);
                }
                break;

            case SupplyGatherStates.FinishedDumpingSupplies:
                break;
        }

        return sleepTime;
    }
}

/// <summary>
/// Requires the object to have KindOf = HARVESTER.
/// </summary>
public abstract class SupplyAIUpdateModuleData : AIUpdateModuleData
{
    internal new static readonly IniParseTable<SupplyAIUpdateModuleData> FieldParseTable = AIUpdateModuleData.FieldParseTable
        .Concat(new IniParseTable<SupplyAIUpdateModuleData>
        {
            { "MaxBoxes", (parser, x) => x.MaxBoxes = parser.ParseInteger() },
            { "SupplyCenterActionDelay", (parser, x) => x.SupplyCenterActionDelay = parser.ParseTimeMillisecondsToLogicFrames() },
            { "SupplyWarehouseActionDelay", (parser, x) => x.SupplyWarehouseActionDelay = parser.ParseTimeMillisecondsToLogicFrames() },
            { "SupplyWarehouseScanDistance", (parser, x) => x.SupplyWarehouseScanDistance = parser.ParseInteger() },
            { "SuppliesDepletedVoice", (parser, x) => x.SuppliesDepletedVoice = parser.ParseAudioEventReference() }
        });

    public int MaxBoxes { get; private set; }
    // ms for whole thing (one transaction)
    public LogicFrameSpan SupplyCenterActionDelay { get; private set; }
    // ms per box (many small transactions)
    public LogicFrameSpan SupplyWarehouseActionDelay { get; private set; }
    // Max distance to look for a warehouse, or we go home.  (Direct dock command on warehouse overrides, and no max on Center Scan)
    public int SupplyWarehouseScanDistance { get; private set; }
    public LazyAssetReference<BaseAudioEventInfo> SuppliesDepletedVoice { get; private set; }
}
