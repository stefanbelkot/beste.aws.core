using Beste.Core.Models;
using Beste.Databases.Connector;
using Beste.Databases.User;
using Beste.GameServer.SDaysTDie.Connections;
using Beste.GameServer.SDaysTDie.Modules;
using Beste.Module;
using Beste.Rights;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Testproject
{
    internal static class TestHelper
    {
        static Beste.Module.BesteUser BesteUser { get; set; } = new Beste.Module.BesteUser();
        static readonly Assembly[] Assemblies =
        {
                Assembly.GetAssembly(typeof(User)),
                Assembly.GetAssembly(typeof(BesteRightsDefinition)),
                Assembly.GetAssembly(typeof(ServerSetting))
        };
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        internal static async Task Login(string userName, string password, ClientWebSocket webSocket, WebSocketHandler webSocketHandler)
        {
            try
            {
                byte[] buffer = new byte[1024 * 4];
                User user = new User
                {
                    Username = userName,
                    Password = password
                };
                Command command = new Command("Login", user);
                string sendString = command.ToJson();
                byte[] sendBytes = System.Text.UTF8Encoding.UTF8.GetBytes(sendString);
                await webSocketHandler.ExtractCompleteMessage(buffer, 60);
                if (webSocketHandler.ReceivedCommand.CommandName != "Connected")
                {
                    Assert.Fail();
                }
                await webSocket.SendAsync(new ArraySegment<byte>(sendBytes, 0, sendBytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                await webSocketHandler.ExtractCompleteMessage(buffer, 60);
                BesteUserAuthentificationResponse authResponse = JsonConvert.DeserializeObject<BesteUserAuthentificationResponse>(webSocketHandler.ReceivedCommand.CommandData.ToString());
                if (authResponse.Result != BesteUserAuthentificationResult.SUCCESS &&
                    authResponse.Result != BesteUserAuthentificationResult.MUST_CHANGE_PASSWORT)
                {
                    Assert.Fail();
                }
                webSocketHandler.User = authResponse.UserData;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Assert.Fail();
            }
        }

        public static async Task UseWebsocketContext(Action<ClientWebSocket, WebSocketHandler> action)
        {
            ClientWebSocket webSocket = new ClientWebSocket();
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocket.ConnectAsync(new Uri("ws://localhost:80/ws"), CancellationToken.None);
            action(webSocket, webSocketHandler);
        }


        internal static async Task ExecuteCommandAndAwaitResponse(ClientWebSocket webSocket, WebSocketHandler webSocketHandler, Command command)
        {
            string sendString = command.ToJson();
            byte[] buffer = new byte[1024 * 4];
            byte[] sendBytes = System.Text.UTF8Encoding.UTF8.GetBytes(sendString);
            await webSocket.SendAsync(new ArraySegment<byte>(sendBytes, 0, sendBytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            await webSocketHandler.ExtractCompleteMessage(buffer, 60);
        }
        internal static async Task ExecuteCommandAndAwaitCommandResponse(ClientWebSocket webSocket, WebSocketHandler webSocketHandler, Command command, string expectedResponse)
        {
            string sendString = command.ToJson();
            byte[] buffer = new byte[1024 * 4];
            byte[] sendBytes = System.Text.UTF8Encoding.UTF8.GetBytes(sendString);
            await webSocket.SendAsync(new ArraySegment<byte>(sendBytes, 0, sendBytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            webSocketHandler.ReceivedCommand = null;
            while (webSocketHandler.ReceivedCommand == null || 
                webSocketHandler.ReceivedCommand.CommandName != expectedResponse)
            {
                await webSocketHandler.ExtractCompleteMessage(buffer, 60);
            }
        }

        public static ServerSetting GenerateNewServerSetting()
        {
            ServerSetting serverSettings = new ServerSetting
            {
                GameName = "MyGame",
                GameWorld = Beste.GameServer.SDaysTDie.Modules.Types.GameWorld.RWG,
                ServerConfigFilepath = "MyGameConfigFilePath" + TestHelper.RandomString(8) + ".xml",
                ServerDescription = "My Server Desc",
                ServerName = "MyServer Name",
                ServerPassword = "MyPassword",
                ServerPort = 50001,
                TelnetPassword = "MyTelnetPW",
                TelnetPort = 8089,
                TerminalWindowEnabled = false,
                WorldGenSeed = "Abrakadabra" + TestHelper.RandomString(8)
            };
            return serverSettings;
        }
        public static void CreateInitialUsersAndRights()
        {
            using (ISession s = SessionFactory.GetSession())
            {
                s.Delete("from ServerSetting s");
                s.Delete("from User o");
                s.Delete("from BesteRightsAuthorization a");
                s.Delete("from BesteRightsDefinition a");
                s.Flush();
            }
            User user = new User
            {
                Username = "Admin",
                Lastname = "Admin",
                Firstname = "Admin",
                Email = "Email",
                Password = "Passwort1$"
            };
            ModifyUserResponse response = BesteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            TestHelper.ValiateResponse(response, ModifyUserResult.SUCCESS);

            user = new User
            {
                Username = "User",
                Lastname = "User",
                Firstname = "User",
                Email = "Email",
                Password = "Passwort1$"
            };
            response = BesteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            TestHelper.ValiateResponse(response, ModifyUserResult.SUCCESS);

            user = new User
            {
                Username = "UserTryChangePassword",
                Lastname = "User",
                Firstname = "User",
                Email = "Email",
                Password = "Passwort1$"
            };
            response = BesteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            TestHelper.ValiateResponse(response, ModifyUserResult.SUCCESS);

            using (NHibernate.ISession session = SessionFactory.GetSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                user = session.QueryOver<User>()
                    .Where(u => u.Username == "Admin")
                    .SingleOrDefault();
            }

            using (ISession s = SessionFactory.GetSession())
            {
                s.Delete("from BesteRightsAuthorization o");
                s.Delete("from BesteRightsDefinition p");
                s.Delete("from BesteRightsNamespace l");
                s.Flush();
            }
            using (NHibernate.IStatelessSession session = SessionFactory.GetStatelessSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                BesteRightsNamespace besteRightsNamespace = new BesteRightsNamespace();
                besteRightsNamespace.Name = "Beste.GameServer.SDaysTDie.User";
                session.Insert(besteRightsNamespace);

                List<BesteRightsDefinition> besteRightsDefinitions = new List<BesteRightsDefinition>();

                BesteRightsDefinition besteRightsDefinitionChangePasswordByUser = new BesteRightsDefinition
                {
                    BesteRightsNamespace = besteRightsNamespace,
                    Operation = "ChangePassword",
                    RecourceModule = "User"
                };
                besteRightsDefinitions.Add(besteRightsDefinitionChangePasswordByUser);

                BesteRightsDefinition besteRightsDefinitionCreateUser = new BesteRightsDefinition
                {
                    BesteRightsNamespace = besteRightsNamespace,
                    Operation = "CreateUser",
                    RecourceModule = "User"
                };
                besteRightsDefinitions.Add(besteRightsDefinitionCreateUser);

                BesteRightsDefinition besteRightsDefinitionDeleteUser = new BesteRightsDefinition
                {
                    BesteRightsNamespace = besteRightsNamespace,
                    Operation = "DeleteUser",
                    RecourceModule = "User"
                };
                besteRightsDefinitions.Add(besteRightsDefinitionDeleteUser);

                BesteRightsDefinition besteRightsDefinitionEditUser = new BesteRightsDefinition
                {
                    BesteRightsNamespace = besteRightsNamespace,
                    Operation = "EditUser",
                    RecourceModule = "User"
                };
                besteRightsDefinitions.Add(besteRightsDefinitionEditUser);

                BesteRightsDefinition besteRightsDefinitionGetUsers = new BesteRightsDefinition
                {
                    BesteRightsNamespace = besteRightsNamespace,
                    Operation = "GetUsers",
                    RecourceModule = "User"
                };
                besteRightsDefinitions.Add(besteRightsDefinitionGetUsers);

                BesteRightsDefinition besteRightsDefinitionGetUser = new BesteRightsDefinition
                {
                    BesteRightsNamespace = besteRightsNamespace,
                    Operation = "GetUser",
                    RecourceModule = "User"
                };
                besteRightsDefinitions.Add(besteRightsDefinitionGetUser);

                foreach (BesteRightsDefinition item in besteRightsDefinitions)
                {
                    session.Insert(item);
                }

                List<BesteRightsAuthorization> besteRightsAuthorizations = new List<BesteRightsAuthorization>();
                besteRightsAuthorizations.Add(new BesteRightsAuthorization
                {
                    Authorized = true,
                    LegitimationId = user.UserId,
                    BesteRightsDefinition = besteRightsDefinitionChangePasswordByUser
                });
                besteRightsAuthorizations.Add(new BesteRightsAuthorization
                {
                    Authorized = true,
                    LegitimationId = user.UserId,
                    BesteRightsDefinition = besteRightsDefinitionCreateUser
                });
                besteRightsAuthorizations.Add(new BesteRightsAuthorization
                {
                    Authorized = true,
                    LegitimationId = user.UserId,
                    BesteRightsDefinition = besteRightsDefinitionDeleteUser
                });
                besteRightsAuthorizations.Add(new BesteRightsAuthorization
                {
                    Authorized = true,
                    LegitimationId = user.UserId,
                    BesteRightsDefinition = besteRightsDefinitionEditUser
                });
                besteRightsAuthorizations.Add(new BesteRightsAuthorization
                {
                    Authorized = true,
                    LegitimationId = user.UserId,
                    BesteRightsDefinition = besteRightsDefinitionGetUsers
                });
                besteRightsAuthorizations.Add(new BesteRightsAuthorization
                {
                    Authorized = true,
                    LegitimationId = user.UserId,
                    BesteRightsDefinition = besteRightsDefinitionGetUser
                });

                foreach (BesteRightsAuthorization item in besteRightsAuthorizations)
                {
                    session.Insert(item);
                }
                transaction.Commit();
            }
        }



        internal static void CreateInitialSettingsAndRights()
        {
            using (NHibernate.IStatelessSession session = SessionFactory.GetStatelessSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                BesteRightsNamespace besteRightsNamespace = new BesteRightsNamespace();
                besteRightsNamespace.Name = "Beste.GameServer.SDaysTDie.ServerSettings";
                session.Insert(besteRightsNamespace);


                User user = session.QueryOver<User>()
                    .Where(u => u.Username == "Admin")
                    .SingleOrDefault();

                List<BesteRightsDefinition> besteRightsDefinitions = new List<BesteRightsDefinition>();

                BesteRightsDefinition besteRightsDefinitionEdit = new BesteRightsDefinition
                {
                    BesteRightsNamespace = besteRightsNamespace,
                    Operation = "Edit",
                    RecourceModule = "ServerSetting"
                };
                besteRightsDefinitions.Add(besteRightsDefinitionEdit);
                
                foreach (BesteRightsDefinition item in besteRightsDefinitions)
                {
                    session.Insert(item);
                }

                List<BesteRightsAuthorization> besteRightsAuthorizations = new List<BesteRightsAuthorization>();
                besteRightsAuthorizations.Add(new BesteRightsAuthorization
                {
                    Authorized = true,
                    LegitimationId = user.UserId,
                    BesteRightsDefinition = besteRightsDefinitionEdit
                });

                foreach (BesteRightsAuthorization item in besteRightsAuthorizations)
                {
                    session.Insert(item);
                }

                transaction.Commit();
            }
        }
        internal static void ActivateTestSchema(bool regenerateSchema = false)
        {
            SessionFactory.Assemblies = Assemblies;
            SessionFactory.ResetFactory();
            SessionFactory.Assemblies = Assemblies;
            string pathToConfig = "Resources" + Path.DirectorySeparatorChar;
            DbSettings dbSettings = DbSettings.LoadFromFile<DbSettings>(pathToConfig + "DBConnectionSettings.xml");
            dbSettings.DbSchema = "beste_test";
            dbSettings.SaveToFile(pathToConfig + "DBConnectionSettings_test.xml");
            SessionFactory.SettingsPath = pathToConfig + "DBConnectionSettings_test.xml";
            if (regenerateSchema)
            {
                SessionFactory.GenerateTables();
            }

            // try to connect (check if table available)
            try
            {
                using (NHibernate.ISession session = SessionFactory.GetSession())
                using (ITransaction transaction = session.BeginTransaction())
                {
                    var users = session.QueryOver<User>();
                    var rights = session.QueryOver<BesteRightsAuthorization>();
                    var serverSettings = session.QueryOver<ServerSetting>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                // try to generate tables if connection failed
                SessionFactory.GenerateTables();
            }
        }

        internal static void ValiateResponse<T, T2>(T2 response, T expectedResult) 
            where T2 : IResponse<T>
            where T : IComparable
        {
            if (!response.Result.Equals(expectedResult))
            {
                Console.WriteLine("response.Result = " + response.Result.ToString() + " Expected = " + expectedResult.ToString());
                Assert.Fail("response.Result = " + response.Result.ToString() + " Expected = " + expectedResult.ToString());
            }
        }
        internal static void ValiateResponse<T, T2>(T2 response, T[] expectedResults)
            where T2 : IResponse<T>
            where T : IComparable
        {
            bool foundExpectedResult = false;
            expectedResults.ForEach((item) =>
            {
                if(!foundExpectedResult)
                {
                    foundExpectedResult = response.Result.Equals(item);
                }
            });
            if (!foundExpectedResult)
            {
                Console.WriteLine("response.Result = " + response.Result.ToString() + " Expected = " + expectedResults.ToString(" | "));
                Assert.Fail("response.Result = " + response.Result.ToString() + " Expected = " + expectedResults.ToString(" | "));
            }
        }

        internal static void KillAllSDaysTDieProcesses()
        {
            foreach (var process in Process.GetProcessesByName("7DaysToDie"))
            {
                process.Kill();
            }
        }

        public static void ForEach<T>(this T[] array, Action<T> action)
        {
            foreach(T item in array)
            {
                action(item);
            }
        }
        public static string ToString<T>(this T[] array, string delimiter)
        {
            string result = array[0].ToString();
            for(int index = 1; index < array.Length; index++)
            {
                result += delimiter + array[index];
            }
            return result;
        }
    }
}
