using Beste.Aws.Databases.Connector;
using Beste.Databases.Connector;
using Beste.Databases.User;
using Beste.GameServer.SDaysTDie;
using Beste.GameServer.SDaysTDie.Connections;
using Beste.GameServer.SDaysTDie.Modules;
using Beste.Module;
using Beste.Rights;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Testproject
{
    [TestClass]
    public class ServerSDaysTDieSettingsTests
    {
        private static char SEP { get; set; } = Path.DirectorySeparatorChar;
        string SDaysToDiePath { get; set; } = "C:" + SEP + "Program Files (x86)" + SEP + "Steam" + SEP + "steamapps" + SEP + "common" + SEP + "7 Days To Die";

        public static int TABLE_ID = 1;
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

            TestHelper.InitializeDatabaseConnection();
            await TestHelper.CreateInitialUsersAndRights();
            await TestHelper.CreateInitialSettingsAndRights();
            
            await Task.Delay(3000);
        }

        [TestMethod]
        public async Task AddServerSettings()
        {

            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            
            await TestHelper.Login("User", "Passwort1$", webSocket, webSocketHandler);

            byte[] buffer = new byte[1024 * 4];
            ServerSetting serverSettings = TestHelper.GenerateNewServerSetting();
            Command command = new Command("AddServerSetting", serverSettings);
            string sendString = command.ToJson();
            byte[] sendBytes = System.Text.UTF8Encoding.UTF8.GetBytes(sendString);
            await webSocket.SendAsync(new ArraySegment<byte>(sendBytes, 0, sendBytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            await webSocketHandler.ExtractCompleteMessage(buffer, 60);
            ModifySettingsResponse response = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            if (response.Result != ModifySettingsResult.SETTING_ADDED)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public async Task AddAlreadyExistingServerSettingsInDatabase()
        {
            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            await Task.Delay(50);
            await TestHelper.Login("User", "Passwort1$", webSocket, webSocketHandler);

            byte[] buffer = new byte[1024 * 4];
            ServerSetting serverSettings = TestHelper.GenerateNewServerSetting();

            Command command = new Command("AddServerSetting", serverSettings);
            string sendString = command.ToJson();
            byte[] sendBytes = System.Text.UTF8Encoding.UTF8.GetBytes(sendString);
            await webSocket.SendAsync(new ArraySegment<byte>(sendBytes, 0, sendBytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            await webSocketHandler.ExtractCompleteMessage(buffer, 60);
            ModifySettingsResponse response = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(response, ModifySettingsResult.SETTING_ADDED);

            await webSocket.SendAsync(new ArraySegment<byte>(sendBytes, 0, sendBytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            await webSocketHandler.ExtractCompleteMessage(buffer, 60);
            response = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(response, ModifySettingsResult.GAME_SEED_ALREADY_EXISTS);

        }

        [TestMethod]
        public async Task EditServerSettingsInDatabase()
        {
            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            await Task.Delay(50);
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
            
            getResponse.ServerSettings[0].GameName = "Edited!";
            command = new Command("EditServerSettings", getResponse.ServerSettings[0]);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            modifyResponse = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifySettingsResult.SETTING_EDITED);
        }

        [TestMethod]
        public async Task EditNotExistingServerSettingsInDatabase()
        {

            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            await Task.Delay(50);
            await TestHelper.Login("User", "Passwort1$", webSocket, webSocketHandler);

            ServerSetting serverSettings = TestHelper.GenerateNewServerSetting();
            serverSettings.GameName = "Edited!";
            serverSettings.Id = 532345823;

            await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                BesteRightsAuthorization besteRightsAuthorization = new BesteRightsAuthorization
                {
                    TableId = TABLE_ID,
                    Namespace = "Beste.GameServer.SDaysTDie.ServerSettings",
                    Operation = "EditServerSettings",
                    RecourceModule = "ServerSetting",
                    RecourceId = serverSettings.Id,
                    Authorized = true,
                    LegitimationUuid = webSocketHandler.User.Uuid,
                    Uuid = Guid.NewGuid().ToString()
                };
                await AmazonDynamoDBFactory.Context.SaveAsync(besteRightsAuthorization);
            });
            
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, webSocketHandler.Result.CloseStatusDescription, CancellationToken.None);

            webSocket = new ClientWebSocket();
            webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            await TestHelper.Login("User", "Passwort1$", webSocket, webSocketHandler);



            Command command = new Command("EditServerSettings", serverSettings);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            ModifySettingsResponse modifyResponse = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifySettingsResult.SETTING_NOT_FOUND);

        }

        [TestMethod]
        public async Task NoRightsOnEditServerSettings()
        {

            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            await Task.Delay(50);
            await TestHelper.Login("User", "Passwort1$", webSocket, webSocketHandler);

            ServerSetting serverSettings = TestHelper.GenerateNewServerSetting();
            serverSettings.GameName = "Edited!";
            serverSettings.Id = 532345828;
            
            Command command = new Command("EditServerSettings", serverSettings);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            ModifySettingsResponse modifyResponse = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifySettingsResult.RIGHT_VIOLATION);

        }

        [TestMethod]
        public async Task DeleteServerSettingsInDatabase()
        {
            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            await Task.Delay(50);
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
            
            command = new Command("DeleteServerSettings", getResponse.ServerSettings[0]);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            modifyResponse = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifySettingsResult.SETTING_DELETED);
        }

        [TestMethod]
        public async Task NoRightsOnDeleteServerSettings()
        {

            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            await Task.Delay(50);
            await TestHelper.Login("User", "Passwort1$", webSocket, webSocketHandler);

            ServerSetting serverSettings = TestHelper.GenerateNewServerSetting();
            serverSettings.GameName = "Edited!";
            serverSettings.Id = 532345828;

            Command command = new Command("DeleteServerSettings", serverSettings);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            ModifySettingsResponse modifyResponse = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifySettingsResult.RIGHT_VIOLATION);

        }

        [TestMethod]
        public async Task AlreadyExistingSeed_Add()
        {
            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            await Task.Delay(50);
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
            
            serverSettings = TestHelper.GenerateNewServerSetting();
            serverSettings.WorldGenSeed = getResponse.ServerSettings[0].WorldGenSeed;
            command = new Command("AddServerSetting", serverSettings);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            modifyResponse = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifySettingsResult.GAME_SEED_ALREADY_EXISTS);


        }

        [TestMethod]
        public async Task AlreadyExistingSeed_Edit()
        {
            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            await Task.Delay(50);
            await TestHelper.Login("User", "Passwort1$", webSocket, webSocketHandler);

            ServerSetting serverSettings = TestHelper.GenerateNewServerSetting();

            Command command = new Command("AddServerSetting", serverSettings);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            ModifySettingsResponse modifyResponse = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifySettingsResult.SETTING_ADDED);

            serverSettings = TestHelper.GenerateNewServerSetting();
            command = new Command("AddServerSetting", serverSettings);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            modifyResponse = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifySettingsResult.SETTING_ADDED);

            command = new Command("GetServerSettingsOfLoggedInUser", null);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            GetSettingsResponse getResponse = JsonConvert.DeserializeObject<GetSettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(getResponse, GetSettingsResult.OK);
            
            getResponse.ServerSettings[0].WorldGenSeed = getResponse.ServerSettings[1].WorldGenSeed;
            command = new Command("EditServerSettings", getResponse.ServerSettings[0]);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            modifyResponse = JsonConvert.DeserializeObject<ModifySettingsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifySettingsResult.GAME_SEED_ALREADY_EXISTS);
        }

    }
}
