using Beste.Databases.Connector;
using Beste.Databases.User;
using Beste.GameServer.SDaysTDie;
using Beste.GameServer.SDaysTDie.Connections;
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
using Beste.GameServer.SDaysTDie.Modules;
using static Beste.GameServer.SDaysTDie.Modules.UserManager;
using Beste.Aws.Module;

namespace Testproject
{
    [TestClass]
    public class ServerUserModuleTests
    {

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
            await Task.Delay(3000);
        }


        [TestMethod, Timeout(15000)]
        public async Task AdminLoginAndChangePassword()
        {

            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            
            await TestHelper.Login("Admin", "Passwort1$", webSocket, webSocketHandler);

            byte[] buffer = new byte[1024 * 4];
            User user = new User
            {
                Username = "Admin",
                Password = "Passwort1$",
                MustChangePassword = false
            };
            
            Command command = new Command("ChangePassword", user);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            ModifyUserResponse response = JsonConvert.DeserializeObject<ModifyUserResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(response, ModifyUserResult.SUCCESS);
        }

        [TestMethod, Timeout(15000)]
        public async Task ChangeLoggedInUsersPassword()
        {

            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            await TestHelper.Login("User", "Passwort1$", webSocket, webSocketHandler);
            
            User user = new User
            {
                Username = "User",
                Password = "Passwort2$",
                MustChangePassword = false
            };
            
            Command command = new Command("ChangePassword", user);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            ModifyUserResponse response = JsonConvert.DeserializeObject<ModifyUserResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(response, ModifyUserResult.SUCCESS);

            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None);
            webSocket = new ClientWebSocket();
            webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            await TestHelper.Login("User", "Passwort2$", webSocket, webSocketHandler);

        }

        [TestMethod, Timeout(15000)]
        public async Task ChangeOtherUsersPasswordNotAllowed()
        {

            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            await TestHelper.Login("UserTryChangePassword", "Passwort1$", webSocket, webSocketHandler);
            BesteUserAuthentificationResponse loginResponse = JsonConvert.DeserializeObject<BesteUserAuthentificationResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            
            User user = new User
            {
                Username = "Admin",
                Password = "Passwort2$",
                MustChangePassword = false
            };
            Command command = new Command("ChangePassword", user);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            ModifyUserResponse response = JsonConvert.DeserializeObject<ModifyUserResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(response, ModifyUserResult.RIGHT_VIOLATION);

        }

        [TestMethod, Timeout(5000)]
        public async Task WrongPassword()
        {
            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);

            byte[] buffer = new byte[1024 * 4];
            await webSocketHandler.ExtractCompleteMessage(buffer, 60);
            if (webSocketHandler.ReceivedCommand.CommandName != "Connected")
            {
                Assert.Fail();
            }

            User user = new User
            {
                Username = "Admin",
                Password = "12345"
            };
            Command command = new Command("Login", user);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            BesteUserAuthentificationResponse authResponse = JsonConvert.DeserializeObject<BesteUserAuthentificationResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(authResponse, BesteUserAuthentificationResult.WRONG_PASSWORD);
        }

        [TestMethod, Timeout(15000)]
        public async Task CreateUserModifyAndDelete()
        {

            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            await TestHelper.Login("Admin", "Passwort1$", webSocket, webSocketHandler);

            User user = new User
            {
                Username = "NewUsernameToDelete",
                Lastname = "Lastname",
                Firstname = "Firstname",
                Email = "Email",
                Password = "Passwort1$"
            };
            Command command = new Command("CreateUser", user);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            ModifyUserResponse modifyResponse = JsonConvert.DeserializeObject<ModifyUserResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifyUserResult.SUCCESS);

            user.Lastname = "LastnameNew";
            command = new Command("EditUser", user);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            modifyResponse = JsonConvert.DeserializeObject<ModifyUserResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifyUserResult.SUCCESS);

            command = new Command("DeleteUser", user);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            modifyResponse = JsonConvert.DeserializeObject<ModifyUserResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(modifyResponse, ModifyUserResult.SUCCESS);
        }

        [TestMethod, Timeout(15000)]
        public async Task CreateUsersAndGetList()
        {

            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            await TestHelper.Login("Admin", "Passwort1$", webSocket, webSocketHandler);
            List<User> users = new List<User>();
            Command command;
            ModifyUserResponse modifyResponse;
            GetUsersResponse getResponse;
            for (int i = 0; i < 10; i++)
            {
                User user = new User
                {
                    Username = "Aa_" + i + "NewUsername",
                    Lastname = "Lastname",
                    Firstname = "Firstname",
                    Email = "Email",
                    Password = "Passwort1$"
                };
                command = new Command("CreateUser", user);
                await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
                modifyResponse = JsonConvert.DeserializeObject<ModifyUserResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
                TestHelper.ValiateResponse(modifyResponse, ModifyUserResult.SUCCESS);

                command = new Command("GetUser", user);
                await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
                getResponse = JsonConvert.DeserializeObject<GetUsersResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
                TestHelper.ValiateResponse(getResponse, GetUsersResult.SUCCESS);
                if(getResponse.Users[0].Username != user.Username)
                {
                    Assert.Fail("getResponse.Users[0].Username != user.Username");
                }
                users.Add(user);
            }

            int offSet = 1;
            int limit = 5;
            GetUsersParams getUsersParams = new GetUsersParams(limit, offSet, SortUsersBy.USERNAME);
            command = new Command("GetUsers", getUsersParams);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            getResponse = JsonConvert.DeserializeObject<GetUsersResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(getResponse, GetUsersResult.SUCCESS);
            for(int index = 0; index < limit; index++)
            {
                if (getResponse.Users[index].Username != users[offSet+index].Username)
                {
                    Assert.Fail("getResponse.Users[index].Username != users[offSet].Username");
                }
            }
            if(getResponse.Users.Count != 5)
            {
                Assert.Fail("(getResponse.Users.Count != 5");
            }

            users.ForEach(async (item) =>
            {
                command = new Command("DeleteUser", item);
                await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
                modifyResponse = JsonConvert.DeserializeObject<ModifyUserResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
                TestHelper.ValiateResponse(modifyResponse, ModifyUserResult.SUCCESS);
            });
        }

        [TestMethod, Timeout(15000)]
        public async Task GetUsersForNormalUser()
        {

            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            await TestHelper.Login("User", "Passwort1$", webSocket, webSocketHandler);
            Command command;

            GetUsersResponse getResponse;
            int offSet = 1;
            int limit = 5;
            GetUsersParams getUsersParams = new GetUsersParams(limit, offSet, SortUsersBy.USERNAME);
            command = new Command("GetUsers", getUsersParams);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            getResponse = JsonConvert.DeserializeObject<GetUsersResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(getResponse, GetUsersResult.RIGHT_VIOLATION);

            User user = new User
            {
                Username = "User"
            };
            command = new Command("GetUser", user);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            getResponse = JsonConvert.DeserializeObject<GetUsersResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(getResponse, GetUsersResult.SUCCESS);

            user.Username = "Admin";
            command = new Command("GetUser", user);
            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            getResponse = JsonConvert.DeserializeObject<GetUsersResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(getResponse, GetUsersResult.RIGHT_VIOLATION);
            
        }

        [TestMethod, Timeout(15000)]
        public async Task LoggedInUserHasRights()
        {
            
            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            await TestHelper.Login("User", "Passwort1$", webSocket, webSocketHandler);
            Command command;

            HasRightsResponse response;
            List<UserManager.Right> rights = new List<UserManager.Right>();
            rights.Add(new UserManager.Right("EditUser_User", "User", null));
            rights.Add(new UserManager.Right("ChangePassword_User", "User", null));
            rights.Add(new UserManager.Right("DeleteUser_User", "User", null));
            rights.Add(new UserManager.Right("GetUser_User", "User", null));
            rights.Add(new UserManager.Right("GetUser", "User", null));
            rights.Add(new UserManager.Right("GetUsers", "User", null));
            command = new Command("LoggedInUserHasRights", rights);

            await TestHelper.ExecuteCommandAndAwaitResponse(webSocket, webSocketHandler, command);
            response = JsonConvert.DeserializeObject<HasRightsResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(response, HasRightsResult.SUCCESS);
            response.Rights.ForEach((right) =>
            {
                if(right.HasRight == true &&
                    (right.Action == "GetUser" &&
                    right.Action == "GetUsers"))
                {
                    Assert.Fail("User: " + right.Action + " -> " + (right.HasRight ? "true" : "false"));
                }
                else if (right.HasRight == false &&
                    (right.Action == "EditUser_User" ||
                    right.Action == "ChangePassword_User" ||
                    right.Action == "DeleteUser_User" ||
                    right.Action == "GetUser_User"))
                {
                    Assert.Fail("User: " + right.Action + " -> " + (right.HasRight ? "true" : "false"));
                }
            });
            
            ClientWebSocket webSocketAdmin = new ClientWebSocket();
            WebSocketHandler webSocketHandlerAdmin = new WebSocketHandler(webSocketAdmin);
            await webSocketAdmin.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            await TestHelper.Login("Admin", "Passwort1$", webSocketAdmin, webSocketHandlerAdmin);

            rights = new List<UserManager.Right>();
            rights.Add(new UserManager.Right("EditUser_Admin", "User", null));
            rights.Add(new UserManager.Right("ChangePassword_Admin", "User", null));
            rights.Add(new UserManager.Right("DeleteUser_Admin", "User", null));
            rights.Add(new UserManager.Right("GetUser_Admin", "User", null));
            rights.Add(new UserManager.Right("GetUser", "User", null));
            rights.Add(new UserManager.Right("GetUsers", "User", null));
            command = new Command("LoggedInUserHasRights", rights);

            await TestHelper.ExecuteCommandAndAwaitResponse(webSocketAdmin, webSocketHandlerAdmin, command);
            response = JsonConvert.DeserializeObject<HasRightsResponse>(webSocketHandlerAdmin.ReceivedCommand.CommandData.ToString());
            TestHelper.ValiateResponse(response, HasRightsResult.SUCCESS);
            response.Rights.ForEach((right) =>
            {
                if (right.HasRight == false)
                {
                    Assert.Fail("Admin: " + right.Action + " -> " + (right.HasRight ? "true" : "false"));
                }
            });
        }
    }
}
