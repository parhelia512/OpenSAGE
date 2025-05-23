﻿using System;
using System.Linq;
using System.Numerics;
using OpenSage.Graphics.Cameras;
using OpenSage.Graphics.Rendering;
using OpenSage.Input;
using OpenSage.Logic.Object;
using OpenSage.Logic.OrderGenerators;

namespace OpenSage.Logic;

public class OrderGeneratorSystem : GameSystem
{
    private IOrderGenerator _activeGenerator;

    public IOrderGenerator ActiveGenerator
    {
        get => _activeGenerator;
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }
            if (_activeGenerator is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _activeGenerator = value;
        }
    }

    public OrderGeneratorSystem(IGame game)
        : base(game)
    {
        _activeGenerator = new UnitOrderGenerator(game);
    }

    private Vector3? _worldPosition;

    public void UpdatePosition(Vector2 mousePosition)
    {
        _worldPosition = GetTerrainPosition(mousePosition);

        if (_worldPosition.HasValue)
        {
            ActiveGenerator?.UpdatePosition(mousePosition, _worldPosition.Value);
        }
    }

    public void UpdateDrag(Vector2 mousePosition)
    {
        var worldPosition = GetTerrainPosition(mousePosition);

        if (worldPosition.HasValue)
        {
            ActiveGenerator?.UpdateDrag(worldPosition.Value);
        }
    }

    public bool TryActivate(KeyModifiers keyModifiers)
    {
        if (!_worldPosition.HasValue)
        {
            return false;
        }

        var result = ActiveGenerator.TryActivate(Game.Scene3D, keyModifiers);

        switch (result)
        {
            case OrderGeneratorResult.Success success:
                {
                    foreach (var order in success.Orders)
                    {
                        Game.NetworkMessageBuffer.AddLocalOrder(order);
                    }

                    if (success.Exit)
                    {
                        ActiveGenerator = new UnitOrderGenerator(Game);
                    }

                    return true;
                }

            case OrderGeneratorResult.InapplicableResult _:
                return false;

            case OrderGeneratorResult.FailureResult _:
                // TODO: Show error message in HUD
                return true;

            default:
                throw new InvalidOperationException();
        }
    }

    public void Update(in TimeInterval time, KeyModifiers keyModifiers)
    {
        var cursor = ActiveGenerator.GetCursor(keyModifiers);
        if (cursor != null)
        {
            Game.Cursors.IsCursorVisible = true;
            Game.Cursors.SetCursor(cursor, time);
        }
        else
        {
            Game.Cursors.IsCursorVisible = false;
        }
    }

    public void BuildRenderList(RenderList renderList, Camera camera, in TimeInterval gameTime)
    {
        ActiveGenerator?.BuildRenderList(renderList, camera, gameTime);
    }

    internal Vector3? GetTerrainPosition(Vector2 mousePosition)
    {
        var ray = Game.Scene3D.Camera.ScreenPointToRay(mousePosition);
        return Game.Scene3D.Terrain.Intersect(ray);
    }

    public void StartSpecialPower(in SpecialPowerCursorInformation cursorInformation)
    {
        StartSpecialPower(cursorInformation, SpecialPowerTargetType.None);
    }

    public void StartSpecialPowerAtLocation(in SpecialPowerCursorInformation cursorInformation)
    {
        StartSpecialPower(cursorInformation, SpecialPowerTargetType.Location);
    }

    public void StartSpecialPowerAtObject(in SpecialPowerCursorInformation cursorInformation)
    {
        StartSpecialPower(cursorInformation, SpecialPowerTargetType.Object);
    }

    private void StartSpecialPower(in SpecialPowerCursorInformation cursorInformation,
        SpecialPowerTargetType targetType)
    {
        var gameData = Game.AssetStore.GameData.Current;

        ActiveGenerator = new SpecialPowerOrderGenerator(cursorInformation, gameData, Game.Scene3D.LocalPlayer,
            Game.Scene3D.GameEngine, targetType, Game.Scene3D, Game.MapTime);

        if (cursorInformation.OrderFlags.HasFlag(SpecialPowerOrderFlags.CheckLike))
        {
            // check-like options are activated immediately with no cursor
            TryActivate(KeyModifiers.None);
        }
    }

    public void StartConstructBuilding(ObjectDefinition buildingDefinition)
    {
        if (!buildingDefinition.KindOf.Get(ObjectKinds.Structure))
        {
            throw new ArgumentException("Building must have the STRUCTURE kind.", nameof(buildingDefinition));
        }

        // TODO: Handle ONLY_BY_AI
        // TODO: Copy default settings from DefaultThingTemplate
        /*if (buildingDefinition.Buildable != ObjectBuildableType.Yes)
        {
            throw new ArgumentException("Building must be buildable.", nameof(buildingDefinition));
        }*/

        // TODO: Check that the builder can build that building.
        // TODO: Check that the building has been unlocked.
        // TODO: Check that the builder isn't building something else right now?

        var gameData = Game.AssetStore.GameData.Current;
        var definitionIndex = buildingDefinition.InternalId;

        ActiveGenerator = new ConstructBuildingOrderGenerator(buildingDefinition, definitionIndex, gameData, Game.Scene3D.LocalPlayer, Game.Scene3D.GameEngine, Game.Scene3D);
    }

    public void SetRallyPoint()
    {
        ActiveGenerator = new RallyPointOrderGenerator(Game, Game.PlayerManager.LocalPlayer.SelectedUnits.Single());
    }

    public void CancelOrderGenerator()
    {
        ActiveGenerator = new UnitOrderGenerator(Game);
    }
}
