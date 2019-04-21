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
    public class ServerSDaysTDieServerTests
    {
        private static readonly char SEP = Path.DirectorySeparatorChar;
        readonly string SDaysToDiePath = "C:" + SEP + "Program Files (x86)" + SEP + "Steam" + SEP + "steamapps" + SEP + "common" + SEP + "7 Days To Die";
        [TestInitialize]
        public async Task TestInit()
        {
            for(int retry = 0; retry <= 2; retry++)
            {
                try
                {
                    TestHelper.KillAllSDaysTDieProcesses();
                    break;
                }
                catch
                {
                    await Task.Delay(1000);
                }
            }
        }

        [ClassInitialize]
        public async static Task AssemblyInit(TestContext context)
        {

            var t = Task.Run(() =>
            {
                var host = new WebHostBuilder()
                    .UseKestrel(options => options.ConfigureEndpoints())
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>()
                    .Build();
                host.Run();
            });
            
            await TestHelper.CreateInitialUsersAndRights();
            await TestHelper.CreateInitialSettingsAndRights();
            await Task.Delay(3000);
        }

        [TestMethod]
        public async Task StartAndShutdownServer()
        {
            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);

            await TestHelper.Login("User", "Passwort1$", webSocket, webSocketHandler);
            
            ServerSetting serverSettings = TestHelper.GenerateNewServerSetting();
            Command command = new Command("AddServerSetting", serverSettings);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            ModifySettingsResponse modifyResponse = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifySettingsResult.SETTING_ADDED);

            command = new Command("GetServerSettingsOfLoggedInUser", null);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            GetSettingsResponse getResponse = JsonConvert.DeserializeObject<GetSettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(getResponse, GetSettingsResult.OK);

            bool foundServerSetting = false;
            foreach(var item in getResponse.ServerSettings)
            {
                if(serverSettings.WorldGenSeed == item.WorldGenSeed)
                {
                    serverSettings = item;
                    foundServerSetting = true;
                }
            }
            if (!foundServerSetting)
                Assert.Fail("Added ServerSettingsId not found in response");

            command = new Command("StartServer", serverSettings);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            StartServerResponse startResponse = JsonConvert.DeserializeObject<StartServerResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(startResponse, StartServerResult.SERVER_STARTED);

            command = new Command("StopServer", serverSettings);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            StopServerResponse stopResponse = JsonConvert.DeserializeObject<StopServerResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(stopResponse, StopServerResult.SERVER_STOPPED);

        }


        [TestMethod]
        public async Task StartAndShutdownServerWithGameMod()
        {
            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);

            await TestHelper.Login("User", "Passwort1$", webSocket, webSocketHandler);

            ServerSetting serverSettings = TestHelper.GenerateNewServerSetting();
            serverSettings.GameMod = "FastProgress";
            Command command = new Command("AddServerSetting", serverSettings);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            ModifySettingsResponse modifyResponse = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifySettingsResult.SETTING_ADDED);

            command = new Command("GetServerSettingsOfLoggedInUser", null);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            GetSettingsResponse getResponse = JsonConvert.DeserializeObject<GetSettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(getResponse, GetSettingsResult.OK);

            bool foundServerSetting = false;
            foreach (var item in getResponse.ServerSettings)
            {
                if (serverSettings.WorldGenSeed == item.WorldGenSeed)
                {
                    serverSettings = item;
                    foundServerSetting = true;
                }
            }
            if (!foundServerSetting)
                Assert.Fail("Added ServerSettingsId not found in response");

            command = new Command("StartServer", serverSettings);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            StartServerResponse startResponse = JsonConvert.DeserializeObject<StartServerResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(startResponse, StartServerResult.SERVER_STARTED);

            command = new Command("StopServer", serverSettings);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            StopServerResponse stopResponse = JsonConvert.DeserializeObject<StopServerResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(stopResponse, StopServerResult.SERVER_STOPPED);

        }

        [TestMethod, Timeout(90000)]
        public async Task StartServerAndConnectToTelnet()
        {
            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);

            await TestHelper.Login("User", "Passwort1$", webSocket, webSocketHandler);

            ServerSetting serverSettings = TestHelper.GenerateNewServerSetting();
            Command command = new Command("AddServerSetting", serverSettings);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            ModifySettingsResponse modifyResponse = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifySettingsResult.SETTING_ADDED);

            command = new Command("GetServerSettingsOfLoggedInUser", null);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            GetSettingsResponse getResponse = JsonConvert.DeserializeObject<GetSettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(getResponse, GetSettingsResult.OK);

            bool foundServerSetting = false;
            foreach (var item in getResponse.ServerSettings)
            {
                if (serverSettings.WorldGenSeed == item.WorldGenSeed)
                {
                    serverSettings = item;
                    foundServerSetting = true;
                    break;
                }
            }
            if (!foundServerSetting)
                Assert.Fail("Added ServerSettingsId not found in response");

            command = new Command("StartServer", serverSettings);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            StartServerResponse startResponse = JsonConvert.DeserializeObject<StartServerResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(startResponse, StartServerResult.SERVER_STARTED);

            command = new Command("ConnectTelnet", serverSettings);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            ConnectTelnetResponse connectResponse = JsonConvert.DeserializeObject<ConnectTelnetResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(connectResponse, ConnectTelnetResult.OK);
            
            byte[] buffer = new byte[1024 * 4];
            int receivedTelnetMessages = 0;
            for(int i = 0; receivedTelnetMessages < 1; i++)
            {
                await webSocketHandler.ExtractCompleteMessage(buffer);
                if(webSocketHandler.ReceivedCommand.CommandName == "OnTelnetReceived")
                {
                    Console.WriteLine(webSocketHandler.ReceivedCommand.CommandData.ToString());
                    Trace.Write(webSocketHandler.ReceivedCommand.CommandData.ToString());
                    receivedTelnetMessages++;
                }
                else
                {
                    Console.WriteLine("*** NO TELNET MESSAGE ***");
                }
            }

            command = new Command("StopServer", serverSettings);
            await TestHelper.ExecuteCommandAndAwaitCommandResponse(webSocket, webSocketHandler, command, "StopServerResponse");
            StopServerResponse stopResponse = JsonConvert.DeserializeObject<StopServerResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(stopResponse, new StopServerResult[]{ StopServerResult.SERVER_STOPPED, StopServerResult.SERVER_KILLED});
        }

        [TestMethod]
        public async Task ServerLimitsOfUser()
        {
            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);

            await TestHelper.Login("User", "Passwort1$", webSocket, webSocketHandler);

            ServerSetting serverSettingsOneOfUserOne = TestHelper.GenerateNewServerSetting();
            Command command = new Command("AddServerSetting", serverSettingsOneOfUserOne);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            ModifySettingsResponse modifyResponse = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifySettingsResult.SETTING_ADDED);

            ServerSetting serverSettingsTwoOfUserOne = TestHelper.GenerateNewServerSetting();
            command = new Command("AddServerSetting", serverSettingsTwoOfUserOne);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            modifyResponse = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifySettingsResult.SETTING_ADDED);

            command = new Command("GetServerSettingsOfLoggedInUser", null);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            GetSettingsResponse getResponse = JsonConvert.DeserializeObject<GetSettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(getResponse, GetSettingsResult.OK);

            bool foundServerSettingOne = false;
            bool foundServerSettingTwo = false;
            foreach (var item in getResponse.ServerSettings)
            {
                if (serverSettingsOneOfUserOne.WorldGenSeed == item.WorldGenSeed)
                {
                    serverSettingsOneOfUserOne = item;
                    foundServerSettingOne = true;
                }
                if (serverSettingsTwoOfUserOne.WorldGenSeed == item.WorldGenSeed)
                {
                    serverSettingsTwoOfUserOne = item;
                    foundServerSettingTwo = true;
                }
            }
            if (!foundServerSettingOne || !foundServerSettingTwo)
                Assert.Fail("Added ServerSettingsId not found in response");

            command = new Command("StartServer", serverSettingsOneOfUserOne);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            StartServerResponse startResponse = JsonConvert.DeserializeObject<StartServerResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(startResponse, StartServerResult.SERVER_STARTED);

            command = new Command("StartServer", serverSettingsTwoOfUserOne);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            startResponse = JsonConvert.DeserializeObject<StartServerResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(startResponse, StartServerResult.SERVER_COUNT_OF_USER_EXCEEDING);

            command = new Command("StopServer", serverSettingsOneOfUserOne);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            StopServerResponse stopResponse = JsonConvert.DeserializeObject<StopServerResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(stopResponse, StopServerResult.SERVER_STOPPED);


        }

        [TestMethod, Timeout(90000)]
        public async Task StartTwoServers()
        {
            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);

            await TestHelper.Login("User", "Passwort1$", webSocket, webSocketHandler);

            ServerSetting serverSettingOne = new ServerSetting
            {
                GameName = "MyGameStartTwoServesOne",
                GameWorld = Beste.GameServer.SDaysTDie.Modules.Types.GameWorld.Navezgane.ToString(),
                ServerConfigFilepath = "MyGameConfigFilePath" + TestHelper.RandomString(8) + ".xml",
                ServerDescription = "My Server Desc",
                ServerName = "MyServerStartTwoServesOne",
                ServerPassword = "MyPassword",
                ServerPort = 50001,
                TelnetPassword = "MyTelnetPW",
                TelnetPort = 8089,
                TerminalWindowEnabled = false,
                WorldGenSeed = "MyGameStartTwoServesOne"
            };

            Command command = new Command("AddServerSetting", serverSettingOne);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            ModifySettingsResponse modifyResponse = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifySettingsResult.SETTING_ADDED);

            command = new Command("GetServerSettingsOfLoggedInUser", null);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            GetSettingsResponse getResponse = JsonConvert.DeserializeObject<GetSettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(getResponse, GetSettingsResult.OK);

            bool foundServerSetting = false;
            foreach (var item in getResponse.ServerSettings)
            {
                if (serverSettingOne.WorldGenSeed == item.WorldGenSeed)
                {
                    serverSettingOne = item;
                    foundServerSetting = true;
                    break;
                }
            }
            if (!foundServerSetting)
                Assert.Fail("Added ServerSettingsId not found in response");



            command = new Command("StartServer", serverSettingOne);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            StartServerResponse startResponse = JsonConvert.DeserializeObject<StartServerResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(startResponse, StartServerResult.SERVER_STARTED);
            
            ClientWebSocket webSocketTwo = new ClientWebSocket();
            WebSocketHandler webSocketHandlerTwo = new WebSocketHandler(webSocketTwo);
            await webSocketTwo.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            await TestHelper.Login("Admin", "Passwort1$", webSocketTwo, webSocketHandlerTwo);

            ServerSetting serverSettingTwo = new ServerSetting
            {
                GameName = "MyGameStartTwoServesTwo",
                GameWorld = Beste.GameServer.SDaysTDie.Modules.Types.GameWorld.Navezgane.ToString(),
                ServerConfigFilepath = "MyGameConfigFilePath" + TestHelper.RandomString(8) + ".xml",
                ServerDescription = "My Server Desc",
                ServerName = "MyServerNameStartTwoServesTwo",
                ServerPassword = "MyPassword",
                ServerPort = 50001,
                TelnetPassword = "MyTelnetPW",
                TelnetPort = 8089,
                TerminalWindowEnabled = false,
                WorldGenSeed = "MyGameStartTwoServesTwo"
            };

            command = new Command("AddServerSetting", serverSettingTwo);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocketTwo, webSocketHandlerTwo, command);
            modifyResponse = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandlerTwo.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifySettingsResult.SETTING_ADDED);

            command = new Command("GetServerSettingsOfLoggedInUser", null);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocketTwo, webSocketHandlerTwo, command);
            getResponse = JsonConvert.DeserializeObject<GetSettingsResponse>(webSocketHandlerTwo.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(getResponse, GetSettingsResult.OK);

            foundServerSetting = false;
            foreach (var item in getResponse.ServerSettings)
            {
                if (serverSettingTwo.WorldGenSeed == item.WorldGenSeed)
                {
                    serverSettingTwo = item;
                    foundServerSetting = true;
                    break;
                }
            }
            if (!foundServerSetting)
                Assert.Fail("Added ServerSettingsId not found in response");



            command = new Command("StartServer", serverSettingTwo);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocketTwo, webSocketHandlerTwo, command);
            startResponse = JsonConvert.DeserializeObject<StartServerResponse>(webSocketHandlerTwo.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(startResponse, StartServerResult.SERVER_STARTED);

            await Task.Delay(10000);
            int processCount = 0;
            foreach (var process in Process.GetProcessesByName("7DaysToDie"))
            {
                processCount++;
            }
            if(processCount != 2)
            {
                Assert.Fail("processCount != 2");
            }

            command = new Command("StopServer", serverSettingOne);
            await TestHelper.ExecuteCommandAndAwaitCommandResponse(webSocket, webSocketHandler, command, "StopServerResponse");
            StopServerResponse stopResponse = JsonConvert.DeserializeObject<StopServerResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(stopResponse, new StopServerResult[] { StopServerResult.SERVER_STOPPED, StopServerResult.SERVER_KILLED });

            command = new Command("StopServer", serverSettingTwo);
            await TestHelper.ExecuteCommandAndAwaitCommandResponse(webSocketTwo, webSocketHandlerTwo, command, "StopServerResponse");
            stopResponse = JsonConvert.DeserializeObject<StopServerResponse>(webSocketHandlerTwo.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(stopResponse, new StopServerResult[] { StopServerResult.SERVER_STOPPED, StopServerResult.SERVER_KILLED });
        }
    }
}
