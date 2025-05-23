﻿#nullable enable

using System.Numerics;
using OpenSage.Content;
using OpenSage.Data.Ini;
using OpenSage.Logic.AI;
using OpenSage.Logic.AI.AIStates;
using OpenSage.Mathematics;

namespace OpenSage.Logic.Object;

public class WorkerAIUpdate : SupplyAIUpdate, IBuilderAIUpdate
{
    public GameObject? BuildTarget => _state.BuildTarget;
    public GameObject? RepairTarget => _state.RepairTarget;
    internal override WorkerAIUpdateModuleData ModuleData { get; }

    private readonly DozerAndWorkerState _state;

    private readonly SupplyAIUpdateStateMachine _supplyAIUpdateStateMachine;
    private ObjectId _unknownObjectId;
    private int _unknown5;
    private int _unknown6;
    private readonly WorkerAIUpdateStateMachine3 _stateMachine3;

    internal WorkerAIUpdate(GameObject gameObject, IGameEngine gameEngine, WorkerAIUpdateModuleData moduleData) : base(gameObject, gameEngine, moduleData)
    {
        ModuleData = moduleData;
        _state = new DozerAndWorkerState(gameObject, gameEngine, this);
        _supplyAIUpdateStateMachine = new SupplyAIUpdateStateMachine(gameObject, gameEngine, this);
        _stateMachine3 = new WorkerAIUpdateStateMachine3(gameObject, gameEngine, this);
    }

    internal override void Stop()
    {
        _state.ClearDozerTasks();
        // todo: stop supply gathering?
        base.Stop();
    }

    protected override void ArrivedAtDestination()
    {
        _state.ArrivedAtDestination();
    }

    #region Dozer stuff

    public void SetBuildTarget(GameObject gameObject)
    {
        // note that the order here is important, as SetTargetPoint will clear any existing buildTarget
        // TODO: target should not be directly on the building, but rather a point along the foundation perimeter
        SetTargetPoint(gameObject.Translation);
        _state.SetBuildTarget(gameObject, GameEngine.GameLogic.CurrentFrame.Value);
        ResetSupplyState();
    }

    public void SetRepairTarget(GameObject gameObject)
    {
        // note that the order here is important, as SetTargetPoint will clear any existing repairTarget
        SetTargetPoint(gameObject.Translation);
        _state.SetRepairTarget(gameObject, GameEngine.GameLogic.CurrentFrame.Value);
        ResetSupplyState();
    }

    internal override void SetTargetPoint(Vector3 targetPoint)
    {
        _state.ClearDozerTasks();
        base.SetTargetPoint(targetPoint);
    }

    #endregion

    #region Supply Stuff

    private void ResetSupplyState()
    {
        CurrentSupplyTarget = null;
        SupplyGatherState = SupplyGatherStates.Default;
    }

    internal override void ClearConditionFlags()
    {
        GameObject.ModelConditionFlags.Set(ModelConditionFlag.HarvestPreparation, false);
        GameObject.ModelConditionFlags.Set(ModelConditionFlag.HarvestAction, false);
        base.ClearConditionFlags();
    }

    protected override int GetAdditionalValuePerSupplyBox(ScopedAssetCollection<UpgradeTemplate> upgrades)
    {
        // this is also hardcoded in original SAGE, replaced by BonusScience and BonusScienceMultiplier (SupplyCenterDockUpdate) in later games
        var upgradeDefinition = upgrades.GetByName("Upgrade_GLAWorkerShoes");
        return GameObject.HasUpgrade(upgradeDefinition) ? ModuleData.UpgradedSupplyBoost : 0;
    }

    internal override GameObject? FindClosestSupplyWarehouse()
    {
        if (ModuleData.HarvestTrees)
        {
            var nearbyTrees = GameEngine.Game.PartitionCellManager.QueryObjects(
                GameObject,
                GameObject.Translation,
                ModuleData.SupplyWarehouseScanDistance,
                new PartitionQueries.KindOfQuery(ObjectKinds.Tree));

            GameObject? closestTree = null;
            var closestDistance = float.MaxValue;

            foreach (var tree in nearbyTrees)
            {
                if (!tree.Definition.IsHarvestable)
                {
                    continue;
                }

                if (tree.Supply <= 0)
                {
                    continue;
                }

                var distance = GameEngine.Game.PartitionCellManager.GetDistanceBetweenObjectsSquared(GameObject, tree);

                if (distance < closestDistance)
                {
                    closestTree = tree;
                    closestDistance = distance;
                }
            }

            return closestTree;
        }
        else
        {
            return base.FindClosestSupplyWarehouse();
        }
    }

    internal override float GetHarvestActivationRange() => ModuleData.HarvestActivationRange;
    internal override LogicFrameSpan GetPreparationTime() => ModuleData.HarvestPreparationTime;

    internal override bool SupplySourceHasBoxes(SupplyWarehouseDockUpdate dockUpdate, GameObject supplySource)
    {
        if (ModuleData.HarvestTrees && supplySource.Definition.KindOf.Get(ObjectKinds.Tree))
        {
            return supplySource.Supply > 0;
        }
        return base.SupplySourceHasBoxes(dockUpdate, supplySource);
    }

    internal override void GetBox()
    {
        if (ModuleData.HarvestTrees && CurrentSupplySource.Definition.KindOf.Get(ObjectKinds.Tree))
        {
            CurrentSupplySource.Supply -= GameEngine.AssetLoadContext.AssetStore.GameData.Current.ValuePerSupplyBox;
            if (CurrentSupplySource.Supply <= 0)
            {
                CurrentSupplySource.Update();
                CurrentSupplySource = null;
            }
            return;
        }
        base.GetBox();
    }

    internal override void SetGatheringConditionFlags()
    {
        GameObject.ModelConditionFlags.Set(ModelConditionFlag.HarvestPreparation, true);
    }

    internal override LogicFrameSpan GetPickingUpTime() => ModuleData.HarvestActionTime;

    internal override void SetActionConditionFlags()
    {
        GameObject.ModelConditionFlags.Set(ModelConditionFlag.HarvestPreparation, false);
        GameObject.ModelConditionFlags.Set(ModelConditionFlag.HarvestAction, true);
    }

    internal override void ClearActionConditionFlags()
    {
        GameObject.ModelConditionFlags.Set(ModelConditionFlag.HarvestAction, false);
    }

    #endregion

    public override UpdateSleepTime Update()
    {
        var sleepTime = base.Update();

        _state.Update();

        var isMoving = GameObject.ModelConditionFlags.Get(ModelConditionFlag.Moving);

        switch (SupplyGatherState)
        {
            case SupplyGatherStates.Default:
                if (!isMoving)
                {
                    SupplyGatherState = SupplyGatherStateToResume;
                    break;
                }
                _waitUntil = GameEngine.GameLogic.CurrentFrame + ModuleData.BoredTime;
                break;
        }

        return sleepTime;
    }

    internal override void Load(StatePersister reader)
    {
        reader.PersistVersion(1);

        reader.BeginObject("Base");
        base.Load(reader);
        reader.EndObject();

        reader.PersistObject(_state);

        reader.PersistObject(_supplyAIUpdateStateMachine);
        reader.PersistObjectId(ref _unknownObjectId);
        reader.PersistInt32(ref _unknown5);

        reader.PersistInt32(ref _unknown6); // was 1 when worker was carrying supplies, wandering around cc with no supply stash available

        reader.PersistObject(_stateMachine3);
    }

    internal sealed class WorkerStateIds
    {
        public static readonly StateId AsDozer = new(0);
        public static readonly StateId AsSupplyTruck = new(1);
    }

    internal sealed class WorkerAIUpdateStateMachine3 : StateMachineBase
    {
        public override WorkerAIUpdate AIUpdate { get; }

        public WorkerAIUpdateStateMachine3(GameObject gameObject, IGameEngine gameEngine, WorkerAIUpdate aiUpdate) : base(gameObject, gameEngine, aiUpdate)
        {
            AIUpdate = aiUpdate;

            // TODO(Port): This configuration is incomplete.
            DefineState(WorkerStateIds.AsDozer, new WorkerUnknown0State(this), StateId.Invalid, StateId.Invalid);
            DefineState(WorkerStateIds.AsSupplyTruck, new WorkerUnknown1State(this), StateId.Invalid, StateId.Invalid);
        }

        public override void Persist(StatePersister reader)
        {
            reader.PersistVersion(1);

            reader.BeginObject("Base");
            base.Persist(reader);
            reader.EndObject();
        }
    }
}

/// <summary>
/// Allows the use of VoiceRepair, VoiceBuildResponse, VoiceSupply, VoiceNoBuild, and
/// VoiceTaskComplete within UnitSpecificSounds section of the object.
/// Requires Kindof = DOZER.
/// </summary>
public sealed class WorkerAIUpdateModuleData : SupplyAIUpdateModuleData, IBuilderAIUpdateData
{
    internal new static WorkerAIUpdateModuleData Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

    private new static readonly IniParseTable<WorkerAIUpdateModuleData> FieldParseTable = SupplyAIUpdateModuleData.FieldParseTable
        .Concat(new IniParseTable<WorkerAIUpdateModuleData>
        {
            { "RepairHealthPercentPerSecond", (parser, x) => x.RepairHealthPercentPerSecond = parser.ParsePercentage() },
            { "BoredTime", (parser, x) => x.BoredTime = parser.ParseTimeMillisecondsToLogicFrames() },
            { "BoredRange", (parser, x) => x.BoredRange = parser.ParseInteger() },
            { "UpgradedSupplyBoost", (parser, x) => x.UpgradedSupplyBoost = parser.ParseInteger() },
            { "HarvestTrees", (parser, x) => x.HarvestTrees = parser.ParseBoolean() },
            { "HarvestActivationRange", (parser, x) => x.HarvestActivationRange = parser.ParseInteger() },
            { "HarvestPreparationTime", (parser, x) => x.HarvestPreparationTime = parser.ParseTimeMillisecondsToLogicFrames() },
            { "HarvestActionTime", (parser, x) => x.HarvestActionTime = parser.ParseTimeMillisecondsToLogicFrames() },
        });

    public Percentage RepairHealthPercentPerSecond { get; private set; }
    public LogicFrameSpan BoredTime { get; private set; }
    public int BoredRange { get; private set; }

    [AddedIn(SageGame.CncGeneralsZeroHour)]
    public int UpgradedSupplyBoost { get; private set; }

    [AddedIn(SageGame.Bfme)]
    public bool HarvestTrees { get; private set; }

    [AddedIn(SageGame.Bfme)]
    public int HarvestActivationRange { get; private set; }

    [AddedIn(SageGame.Bfme)]
    public LogicFrameSpan HarvestPreparationTime { get; private set; }

    [AddedIn(SageGame.Bfme)]
    public LogicFrameSpan HarvestActionTime { get; private set; }

    internal override BehaviorModule CreateModule(GameObject gameObject, IGameEngine gameEngine)
    {
        return new WorkerAIUpdate(gameObject, gameEngine, this);
    }
}
