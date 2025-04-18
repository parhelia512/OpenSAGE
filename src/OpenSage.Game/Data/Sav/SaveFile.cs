﻿using System;
using System.Collections.Generic;
using System.IO;
using OpenSage.Graphics.Cameras;
using OpenSage.IO;

namespace OpenSage.Data.Sav;

public static class SaveFile
{
    public static GameState GetGameState(FileSystemEntry entry, IGame game)
    {
        using (var stream = entry.Open())
        {
            var reader = new StateReader(stream, game);

            while (true)
            {
                var chunkName = "";
                reader.PersistAsciiString(ref chunkName);

                reader.BeginSegment(chunkName);

                if (chunkName == "CHUNK_GameState")
                {
                    var gameState = new GameState();
                    gameState.Persist(reader);
                    return gameState;
                }

                reader.EndSegment();
            }
        }

        throw new InvalidStateException();
    }

    public static void Load(FileSystemEntry entry, IGame game)
    {
        using var stream = entry.Open();
        LoadFromStream(stream, game);
    }

    public static void LoadFromStream(Stream stream, IGame game)
    {
        using var statePersister = new StateReader(stream, game);

        Persist(statePersister);
    }

    private record struct ChunkDefinition(string ChunkName, Func<IGame, IPersistableObject> GetPersistableObject);

    // This correlates to GameState::init in Generals
    private static readonly List<ChunkDefinition> ChunkDefinitions = new()
    {
        new ChunkDefinition("CHUNK_GameState", game => game.GameState),
        new ChunkDefinition("CHUNK_Campaign", game => game.CampaignManager),
        new ChunkDefinition("CHUNK_GameStateMap", game => game.GameStateMap),
        new ChunkDefinition("CHUNK_TerrainLogic", game => game.TerrainLogic),
        new ChunkDefinition("CHUNK_TeamFactory", game => game.TeamFactory),
        new ChunkDefinition("CHUNK_Players", game => game.PlayerManager),
        new ChunkDefinition("CHUNK_GameLogic", game => game.GameLogic),
        new ChunkDefinition("CHUNK_ParticleSystem", game => game.Scene3D.ParticleSystemManager),
        new ChunkDefinition("CHUNK_Radar", game => game.Scene3D.Radar),
        new ChunkDefinition("CHUNK_ScriptEngine", game => game.Scripting),
        // TODO(Port): This is not entirely correct. The serialised data does indeed consist solely of the player scripts,
        // but in Generals the containing object is the global SidesList object.
        // So we need to replace PlayerScripts in Scene3D / Game with SidesList.
        new ChunkDefinition("CHUNK_SidesList", game => game.Scene3D.PlayerScripts),
        new ChunkDefinition("CHUNK_TacticalView", game => game.Scene3D.TacticalView),
        new ChunkDefinition("CHUNK_GameClient", game => game.GameClient),
        new ChunkDefinition("CHUNK_InGameUI", game => game.AssetStore.InGameUI.Current),
        new ChunkDefinition("CHUNK_Partition", game => game.PartitionCellManager),
        new ChunkDefinition("CHUNK_TerrainVisual", game => game.TerrainVisual),
        new ChunkDefinition("CHUNK_GhostObject", game => game.GhostObjectManager),

    };

    public static void Persist(StatePersister persister)
    {
        //using var binaryReader = new BinaryReader(stream, Encoding.Unicode, true);

        //if (game.SageGame >= SageGame.Bfme)
        //{
        //    var header1 = binaryReader.ReadFourCc(bigEndian: true);
        //    if (header1 != "EALA")
        //    {
        //        throw new InvalidStateException();
        //    }

        //    var header2 = binaryReader.ReadFourCc(bigEndian: true);
        //    if (header2 != "RTS1")
        //    {
        //        throw new InvalidStateException();
        //    }

        //    var header3 = binaryReader.ReadUInt32();
        //    if (header3 != 0)
        //    {
        //        throw new InvalidStateException();
        //    }
        //}

        persister.BeginArray();

        if (persister.Mode == StatePersistMode.Read)
        {
            while (true)
            {
                persister.BeginObject();

                var chunkName = "";
                persister.PersistAsciiString(ref chunkName);

                if (chunkName == "SG_EOF")
                {
                    //if (stream.Position != stream.Length)
                    //{
                    //    throw new InvalidStateException();
                    //}
                    break;
                }

                var chunkLength = persister.BeginSegment(chunkName);

                var chunkDefinition = ChunkDefinitions.Find(x => x.ChunkName == chunkName);

                if (chunkDefinition == default)
                {
                    throw new InvalidDataException($"Unknown chunk type '{chunkName}'.");
                }

                var persistableObject = chunkDefinition.GetPersistableObject(persister.Game);
                persister.PersistObject(persistableObject, "ChunkData");

                persister.EndSegment();

                persister.EndObject();
            }

            // If we haven't started a game yet (which will be the case for
            // "mission start" save files), then start it now.
            if (!persister.Game.InGame)
            {
                persister.Game.StartCampaign(
                    persister.Game.CampaignManager.CampaignName,
                    persister.Game.CampaignManager.MissionName);
            }
        }
        else
        {
            foreach (var chunkDefinition in ChunkDefinitions)
            {
                persister.BeginObject();

                var chunkName = chunkDefinition.ChunkName;
                persister.PersistAsciiString(ref chunkName);

                persister.BeginSegment(chunkName);

                var persistableObject = chunkDefinition.GetPersistableObject(persister.Game);
                persister.PersistObject(persistableObject, "ChunkData");

                persister.EndSegment();

                persister.EndObject();
            }

            persister.BeginObject();

            var endChunkName = "SG_EOF";
            persister.PersistAsciiString(ref endChunkName, "ChunkName");

            persister.EndObject();
        }

        persister.EndArray();
    }
}

public enum SaveGameType : uint
{
    Skirmish,
    SinglePlayer
}
