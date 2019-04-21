using Beste.Databases.Connector;
using Beste.Databases.User;
using Beste.GameServer.SDaysTDie;
using Beste.GameServer.SDaysTDie.Connections;
using Beste.GameServer.SDaysTDie.Models;
using Beste.GameServer.SDaysTDie.Modules;
using Beste.GameServer.SDaysTDie.Modules.Types;
using Beste.Module;
using Beste.Rights;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static Beste.GameServer.SDaysTDie.Modules.TelnetHandler;

namespace Testproject
{
    [TestClass]
    public class TestPlayground
    {
        private static readonly char SEP = Path.DirectorySeparatorChar;

        [TestInitialize]
        public async Task TestInit()
        {
            foreach (var process in Process.GetProcessesByName("7DaysToDie"))
            {
                process.Kill();
            }
            await Task.Delay(5);
        }

        public void GenerateGameModsXml()
        {

            GameMods gameMods = new GameMods();
            gameMods.ConfiguredGameMods = new List<GameMod>();
            gameMods.ConfiguredGameMods.Add(new GameMod
            {
                ModName = "",
                PathToGameFiles = ""
            });
            gameMods.SaveToFile("config" + SEP + "gamemodstest.xml");
        }
    }
}
