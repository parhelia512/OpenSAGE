﻿using System.Linq;
using OpenSage.Data;
using OpenSage.Mods.Generals;
using OpenSage.Tests.Data;
using Veldrid;
using Xunit;
using Xunit.Abstractions;

namespace OpenSage.Tests.Content;

public class LoadMapsTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public LoadMapsTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [GameFact(SageGame.CncGenerals, Skip = "Can take up to 30 minutes to run")]
    public void LoadGeneralsMaps()
    {
        var rootFolder = InstalledFilesTestData.GetInstallationDirectory(SageGame.CncGenerals);
        var installation = new GameInstallation(new GeneralsDefinition(), rootFolder);

        Platform.Start();

        using (var game = new Game(installation))
        {
            var maps = game.ContentManager.FileSystem
                .GetFilesInDirectory("maps", "*.map")
                .ToList();

            foreach (var map in maps)
            {
                _testOutputHelper.WriteLine($"Loading {map.FilePath}...");

                //game.AssetStore.PushScope();

                throw new System.NotImplementedException();

                // TODO: Need to update to use new way of starting game.
                //using (var scene = game.LoadMap(map.FilePath))
                //{
                //    Assert.NotNull(scene);
                //}

                //game.AssetStore.PopScope();
            }
        }

        Platform.Stop();
    }
}
