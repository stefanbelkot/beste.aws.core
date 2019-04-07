using Beste.Databases.Connector;
using Beste.Databases.User;
using Beste.GameServer.SDaysTDie;
using Beste.GameServer.SDaysTDie.Connections;
using Beste.GameServer.SDaysTDie.Modules;
using Beste.GameServer.SDaysTDie.Modules.Types;
using Beste.Module;
using Beste.Rights;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NHibernate;
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
    public class SDaysTDieServerTests
    {
        private static readonly char SEP = Path.DirectorySeparatorChar;
        readonly string SDaysToDiePath = "C:" + SEP + "Program Files (x86)" + SEP + "Steam" + SEP + "steamapps" + SEP + "common" + SEP + "7 Days To Die";
        [TestInitialize]
        public async Task TestInit()
        {
            foreach (var process in Process.GetProcessesByName("7DaysToDie"))
            {
                process.Kill();
            }
            await Task.Delay(5);
        }

        [TestMethod]
        public async Task StartAndShutdownServer()
        {
            bool serverStopped = false;
            Console.WriteLine("*** Starting ***");

            ServerSettings serverSettings = ServerSettings.LoadFromFile<ServerSettings>(SDaysToDiePath + SEP + "serverconfig.xml");
            serverSettings.ServerConfigFilepath = "StartAndShutdownServer.xml";
            serverSettings.SaveToFile(SDaysToDiePath + SEP + serverSettings.ServerConfigFilepath);
            SDaysTDieServer sDaysTDieServer = new SDaysTDieServer(SDaysToDiePath, serverSettings);

            void SDaysTDieServer_OnSDaysTDieServerStoppedHandler(SDaysTDieServer sender, SDaysTDieServer.OnSDaysTDieServerStoppedEventArgs e)
            {
                serverStopped = true;
            }
            sDaysTDieServer.OnSDaysTDieServerStoppedHandler += SDaysTDieServer_OnSDaysTDieServerStoppedHandler;

            sDaysTDieServer.Start();
            await Task.Delay(10000);
            sDaysTDieServer.Stop();
            await Task.Delay(10000);
            Assert.AreEqual(true, serverStopped);
        }

        [TestMethod, Timeout(45000)]
        public async Task FastStartAndShutdownServer()
        {
            bool serverStoppedNormally = false;
            Console.WriteLine("*** Starting ***");

            ServerSettings serverSettings = ServerSettings.LoadFromFile<ServerSettings>(SDaysToDiePath + SEP + "serverconfig.xml");
            serverSettings.ServerConfigFilepath = "FastStartAndShutdownServer.xml";
            serverSettings.SaveToFile(SDaysToDiePath + SEP + serverSettings.ServerConfigFilepath);
            SDaysTDieServer sDaysTDieServer = new SDaysTDieServer(SDaysToDiePath, serverSettings);

            void SDaysTDieServer_OnSDaysTDieServerStoppedHandler(SDaysTDieServer sender, SDaysTDieServer.OnSDaysTDieServerStoppedEventArgs e)
            {
                if(e.Message == "*** Shutdown successful ***")
                    serverStoppedNormally = true;
            }
            sDaysTDieServer.OnSDaysTDieServerStoppedHandler += SDaysTDieServer_OnSDaysTDieServerStoppedHandler;

            sDaysTDieServer.Start();
            sDaysTDieServer.Stop();
            async Task CheckServerStopped()
            {
                while(true)
                {
                    if (serverStoppedNormally)
                        return;
                    await Task.Delay(1000);
                }
            }
            await CheckServerStopped();
        }

        [TestMethod, Timeout(45000)]
        public async Task FastStartAndShutdownServerModdedConfig()
        {
            bool serverStoppedNormally = false;
            Console.WriteLine("*** Starting ***");

            ServerSettings serverSettings = ServerSettings.LoadFromFile<ServerSettings>(SDaysToDiePath + SEP + "serverconfig.xml");
            serverSettings.ServerPort = 26901;
            serverSettings.TelnetPort = 8082;
            serverSettings.TelnetPassword = "Password";
            serverSettings.ServerConfigFilepath = "serverconfigModded.xml";
            serverSettings.SaveToFile(SDaysToDiePath + SEP + serverSettings.ServerConfigFilepath);
            SDaysTDieServer sDaysTDieServer = new SDaysTDieServer(SDaysToDiePath, serverSettings);
            
            void SDaysTDieServer_OnSDaysTDieServerStoppedHandler(SDaysTDieServer sender, SDaysTDieServer.OnSDaysTDieServerStoppedEventArgs e)
            {
                if (e.Message == "*** Shutdown successful ***")
                    serverStoppedNormally = true;
            }
            sDaysTDieServer.OnSDaysTDieServerStoppedHandler += SDaysTDieServer_OnSDaysTDieServerStoppedHandler;

            sDaysTDieServer.Start();
            sDaysTDieServer.Stop();
            async Task CheckServerStopped()
            {
                while (true)
                {
                    if (serverStoppedNormally)
                        return;
                    await Task.Delay(1000);
                }
            }
            await CheckServerStopped();
        }
        [TestMethod, Timeout(45000)]
        public async Task StartAndShutdownTwoServers()
        {
            bool serverOneStoppedNormally = false;
            bool serverTwoStoppedNormally = false;
            Console.WriteLine("*** Starting ***");


            void SDaysTDieServer_One_OnSDaysTDieServerStoppedHandler(SDaysTDieServer sender, SDaysTDieServer.OnSDaysTDieServerStoppedEventArgs e)
            {
                if (e.Message == "*** Shutdown successful ***")
                    serverOneStoppedNormally = true;
            }
            void SDaysTDieServer_Two_OnSDaysTDieServerStoppedHandler(SDaysTDieServer sender, SDaysTDieServer.OnSDaysTDieServerStoppedEventArgs e)
            {
                if (e.Message == "*** Shutdown successful ***")
                    serverTwoStoppedNormally = true;
            }

            ServerSettings serverSettingsOne = ServerSettings.LoadFromFile<ServerSettings>(SDaysToDiePath + SEP + "serverconfig.xml");
            serverSettingsOne.ServerConfigFilepath = "serverconfigOne.xml";
            serverSettingsOne.SaveToFile(SDaysToDiePath + SEP + serverSettingsOne.ServerConfigFilepath);
            SDaysTDieServer sDaysTDieServerOne = new SDaysTDieServer(SDaysToDiePath, serverSettingsOne);
            sDaysTDieServerOne.OnSDaysTDieServerStoppedHandler += SDaysTDieServer_One_OnSDaysTDieServerStoppedHandler;
            sDaysTDieServerOne.Start();
            
            ServerSettings serverSettingsTwo = ServerSettings.LoadFromFile<ServerSettings>(SDaysToDiePath + SEP + "serverconfig.xml");
            serverSettingsTwo.ServerPort = 26901;
            serverSettingsTwo.TelnetPort = 8082;
            serverSettingsTwo.TelnetPassword = "Password";
            serverSettingsTwo.ServerConfigFilepath = "serverconfigTwo.xml";
            serverSettingsTwo.SaveToFile(SDaysToDiePath + SEP + serverSettingsTwo.ServerConfigFilepath);
            SDaysTDieServer sDaysTDieServerTwo = new SDaysTDieServer(SDaysToDiePath, serverSettingsTwo);
            sDaysTDieServerTwo.OnSDaysTDieServerStoppedHandler += SDaysTDieServer_Two_OnSDaysTDieServerStoppedHandler;
            sDaysTDieServerTwo.Start();
            
            sDaysTDieServerOne.Stop();
            sDaysTDieServerTwo.Stop();
            
            async Task CheckServerStopped()
            {
                while (true)
                {
                    if (serverOneStoppedNormally && serverTwoStoppedNormally)
                        return;
                    await Task.Delay(1000);
                }
            }
            await CheckServerStopped();
        }
        [TestMethod]
        public async Task InvalidConfigFile()
        {
            await Task.Delay(500);

            ServerSettings serverSettingsOne = ServerSettings.LoadFromFile<ServerSettings>(SDaysToDiePath + SEP + "serverconfig.xml");
            serverSettingsOne.ServerConfigFilepath = "serverconfigFileNotFound.xml";
            SDaysTDieServer sDaysTDieServerOne = new SDaysTDieServer(SDaysToDiePath, serverSettingsOne);
            try
            {
                sDaysTDieServerOne.Start();
                Assert.Fail("Expected 'Config file not found' Exception!");
            }
            catch(FileNotFoundException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Config file not found at"));
            }
        }
        [TestMethod]
        public async Task ForbidUseAndSaveOfDefaultConfigFile()
        {
            await Task.Delay(500);
            ServerSettings serverSettingsOne = ServerSettings.LoadFromFile<ServerSettings>(SDaysToDiePath + SEP + "serverconfig.xml");
            try
            {
                serverSettingsOne.SaveToFile(SDaysToDiePath + SEP + serverSettingsOne.ServerConfigFilepath);
                Assert.Fail("Expected 'File is not allowed to be saved as 'serverconfig.xml'' Exception!");
            }
            catch (ArgumentException ex)
            {
                Assert.IsTrue(ex.Message.Contains("File is not allowed to be saved as 'serverconfig.xml'"));
            }

            try
            {
                SDaysTDieServer sDaysTDieServerOne = new SDaysTDieServer(SDaysToDiePath, serverSettingsOne);
                Assert.Fail("serverSettings.ServerConfigFilePath.EndsWith('serverconfig.xml')! Use of Default File is not allowed!");
            }
            catch (ArgumentException ex)
            {
                Assert.IsTrue(ex.Message.Contains("serverSettings.ServerConfigFilePath.EndsWith('serverconfig.xml')! Use of Default File is not allowed!"));
            }
        }
        [TestMethod]
        public async Task SetAndCheckUserServerSettingsStringIntBoolType()
        {
            await Task.Delay(500);
            ServerSettings serverSettings = ServerSettings.LoadFromFile<ServerSettings>("Testdata" + SEP + "serverconfig.xml");

            // try set string in xml
            serverSettings.ServerDescription = "Test";
            if (serverSettings.ServerDescription != "Test")
                Assert.Fail("serverSettings.ServerDescription != Test");

            // try set type in xml
            serverSettings.GameWorld = GameWorld.RWG;
            if (serverSettings.GameWorld != GameWorld.RWG)
                Assert.Fail("serverSettings.GameWorld != GameWorld.RWG");

            // try set int in xml
            serverSettings.ServerPort = 12345;
            if (serverSettings.ServerPort != 12345)
                Assert.Fail("serverSettings.ServerPort != 12345");

            // try set boolean in xml
            serverSettings.TerminalWindowEnabled = false;
            if (serverSettings.TerminalWindowEnabled != false)
                Assert.Fail("serverSettings.TerminalWindowEnabled != false");

            //save modified file, load again and check if values still valid
            serverSettings.SaveToFile("serverconfig_modified.xml");
            serverSettings = ServerSettings.LoadFromFile<ServerSettings>("serverconfig_modified.xml");

            if (serverSettings.ServerDescription != "Test")
                Assert.Fail("serverSettings.ServerDescription != Test");
            if (serverSettings.GameWorld != GameWorld.RWG)
                Assert.Fail("serverSettings.GameWorld != GameWorld.RWG");
            if (serverSettings.ServerPort != 12345)
                Assert.Fail("serverSettings.ServerPort != 12345");
            if (serverSettings.TerminalWindowEnabled != false)
                Assert.Fail("serverSettings.TerminalWindowEnabled != false");

        }
        [TestMethod]
        public async Task SetAllUserServerSettings()
        {
            await Task.Delay(500);
            ServerSettings serverSettings = ServerSettings.LoadFromFile<ServerSettings>(SDaysToDiePath + SEP + "serverconfig.xml"); ;
            serverSettings.GameName = "SomeString";
            serverSettings.GameWorld = GameWorld.RWG;
            serverSettings.ServerConfigFilepath = SDaysToDiePath + SEP + "serverconfig.xml";
            serverSettings.ServerDescription = "SomeString";
            serverSettings.ServerName = "SomeString";
            serverSettings.ServerPassword = "SomeString";
            serverSettings.ServerPort = 12345;
            serverSettings.TelnetPassword = "SomeString";
            serverSettings.TelnetPort = 8082;
            serverSettings.TerminalWindowEnabled = false;
            serverSettings.WorldGenSeed = "SomeString";

        }
    }
}
