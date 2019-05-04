using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Beste.Aws.Databases.Connector;
using Beste.Aws.Module;
using Beste.Core.Models;
using Beste.Databases.User;
using Beste.GameServer.SDaysTDie.Connections;
using Beste.GameServer.SDaysTDie.Modules;
using Beste.Rights;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Testproject
{
    internal static class TestHelper
    {
        public static int TABLE_ID = 1;
        static Beste.Aws.Module.BesteUser BesteUser { get; set; } = new Beste.Aws.Module.BesteUser();

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
                GameWorld = Beste.GameServer.SDaysTDie.Modules.Types.GameWorld.RWG.ToString(),
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
        public static async Task CreateInitialUsersAndRights()
        {
            await CleanUpBesteRightsAuthorization();
            await CleanUpBesteUser();
            await CleanUpServerSettings();

            User adminUser = new User
            {
                Username = "Admin",
                Lastname = "Admin",
                Firstname = "Admin",
                Email = "Email",
                Password = "Passwort1$"
            };
            ModifyUserResponse response = await BesteUser.CreateUser(JsonConvert.SerializeObject(adminUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            TestHelper.ValiateResponse(response, ModifyUserResult.SUCCESS);

            User user = new User
            {
                Username = "User",
                Lastname = "User",
                Firstname = "User",
                Email = "Email",
                Password = "Passwort1$"
            };
            response = await BesteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            TestHelper.ValiateResponse(response, ModifyUserResult.SUCCESS);

            user = new User
            {
                Username = "UserTryChangePassword",
                Lastname = "User",
                Firstname = "User",
                Email = "Email",
                Password = "Passwort1$"
            };
            response = await BesteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            TestHelper.ValiateResponse(response, ModifyUserResult.SUCCESS);



            adminUser = await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                adminUser.TableId = TABLE_ID;
                return await context.LoadAsync(adminUser);
            });

            await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                await context.SaveAsync(new BesteRightsAuthorization
                {
                    TableId = TABLE_ID,
                    Namespace = "Beste.GameServer.SDaysTDie.User",
                    Operation = "ChangePassword",
                    RecourceModule = "User",
                    RecourceId = null,
                    Authorized = true,
                    LegitimationUuid = adminUser.Uuid,
                    Uuid = Guid.NewGuid().ToString()
                });
                await context.SaveAsync(new BesteRightsAuthorization
                {
                    TableId = TABLE_ID,
                    Namespace = "Beste.GameServer.SDaysTDie.User",
                    Operation = "CreateUser",
                    RecourceModule = "User",
                    RecourceId = null,
                    Authorized = true,
                    LegitimationUuid = adminUser.Uuid,
                    Uuid = Guid.NewGuid().ToString()
                });
                await context.SaveAsync(new BesteRightsAuthorization
                {
                    TableId = TABLE_ID,
                    Namespace = "Beste.GameServer.SDaysTDie.User",
                    Operation = "DeleteUser",
                    RecourceModule = "User",
                    RecourceId = null,
                    Authorized = true,
                    LegitimationUuid = adminUser.Uuid,
                    Uuid = Guid.NewGuid().ToString()
                });
                await context.SaveAsync(new BesteRightsAuthorization
                {
                    TableId = TABLE_ID,
                    Namespace = "Beste.GameServer.SDaysTDie.User",
                    Operation = "EditUser",
                    RecourceModule = "User",
                    RecourceId = null,
                    Authorized = true,
                    LegitimationUuid = adminUser.Uuid,
                    Uuid = Guid.NewGuid().ToString()
                });
                await context.SaveAsync(new BesteRightsAuthorization
                {
                    TableId = TABLE_ID,
                    Namespace = "Beste.GameServer.SDaysTDie.User",
                    Operation = "GetUsers",
                    RecourceModule = "User",
                    RecourceId = null,
                    Authorized = true,
                    LegitimationUuid = adminUser.Uuid,
                    Uuid = Guid.NewGuid().ToString()
                });
                await context.SaveAsync(new BesteRightsAuthorization
                {
                    TableId = TABLE_ID,
                    Namespace = "Beste.GameServer.SDaysTDie.User",
                    Operation = "GetUser",
                    RecourceModule = "User",
                    RecourceId = null,
                    Authorized = true,
                    LegitimationUuid = adminUser.Uuid,
                    Uuid = Guid.NewGuid().ToString()
                });
            });
        }

        private static async Task CleanUpBesteRightsAuthorization()
        {
            var request = new QueryRequest
            {
                TableName = BesteRightsAuthorization.TableName,
                KeyConditions = new Dictionary<string, Condition>
                    {
                        { "TableId", new Condition()
                            {
                                ComparisonOperator = ComparisonOperator.EQ,
                                AttributeValueList = new List<AttributeValue>
                                {
                                  new AttributeValue { N = TABLE_ID.ToString() }
                                }

                            }
                        }
                    },
                //AttributesToGet = new List<string> { "user_id" }
            };
            var response = await AmazonDynamoDBFactory.Client.QueryAsync(request);
            foreach (var item in response.Items)
            {
                BesteRightsAuthorization auth = new BesteRightsAuthorization()
                {
                    TableId = TABLE_ID,
                    Uuid = item["Uuid"].S
                };
                await AmazonDynamoDBFactory.Context.DeleteAsync(auth);
            }
        }
        private static async Task CleanUpBesteUser()
        {
            var request = new QueryRequest
            {
                TableName = User.TableName,
                KeyConditions = new Dictionary<string, Condition>
                    {
                        { "id", new Condition()
                            {
                                ComparisonOperator = ComparisonOperator.EQ,
                                AttributeValueList = new List<AttributeValue>
                                {
                                  new AttributeValue { N = TABLE_ID.ToString() }
                                }

                            }
                        }
                    },
                //AttributesToGet = new List<string> { "user_id" }
            };
            var response = await AmazonDynamoDBFactory.Client.QueryAsync(request);
            foreach (var item in response.Items)
            {
                User user = new User()
                {
                    TableId = TABLE_ID,
                    Username = item["username"].S
                };
                await AmazonDynamoDBFactory.Context.DeleteAsync(user);
            }
        }
        private static async Task CleanUpServerSettings()
        {
            var request = new QueryRequest
            {
                TableName = ServerSetting.TableName,
                KeyConditions = new Dictionary<string, Condition>
                    {
                        { "TableId", new Condition()
                            {
                                ComparisonOperator = ComparisonOperator.EQ,
                                AttributeValueList = new List<AttributeValue>
                                {
                                  new AttributeValue { N = TABLE_ID.ToString() }
                                }

                            }
                        }
                    },
                //AttributesToGet = new List<string> { "user_id" }
            };
            var response = await AmazonDynamoDBFactory.Client.QueryAsync(request);
            foreach (var item in response.Items)
            {
                ServerSetting serverSetting = new ServerSetting()
                {
                    TableId = TABLE_ID,
                    Id = Convert.ToInt32( item["Id"].N)
                };
                await AmazonDynamoDBFactory.Context.DeleteAsync(serverSetting);
            }
        }

        internal static async Task CreateInitialSettingsAndRights()
        {
            User adminUser = new User
            {
                TableId = TABLE_ID,
                Username = "Admin"
            };
            adminUser = await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                return await context.LoadAsync(adminUser);
            });
            await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                await context.SaveAsync(new BesteRightsAuthorization
                {
                    TableId = TABLE_ID,
                    Namespace = "Beste.GameServer.SDaysTDie.ServerSettings",
                    Operation = "Edit",
                    RecourceModule = "ServerSetting",
                    RecourceId = null,
                    Authorized = true,
                    LegitimationUuid = adminUser.Uuid,
                    Uuid = Guid.NewGuid().ToString()
                });
            });
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

        internal static void InitializeDatabaseConnection()
        {
            AmazonDynamoDBFactory.ResetFactory();

            string localApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string subPath = "Beste.Core" + Path.DirectorySeparatorChar + "Tests";
            string configFileName = "configAws.xml";
            string directoryOfTestConfigAws = System.IO.Path.Combine(localApplicationDataPath, subPath);
            string pathToTestConfigAws = System.IO.Path.Combine(localApplicationDataPath, subPath, configFileName);
            if (!Directory.Exists(directoryOfTestConfigAws) ||
                !File.Exists(pathToTestConfigAws))
            {
                AwsConfig awsConfigPattern = new AwsConfig()
                {
                    RegionEndpoint = "REGIONENDPOINT",
                    AccessKey = "YOURACCESSKEY",
                    SecretKey = "YOURSECRETKEY"
                };
                if (!Directory.Exists(directoryOfTestConfigAws))
                {
                    Directory.CreateDirectory(directoryOfTestConfigAws);
                }
                string pathToTestConfigAwsPattern = System.IO.Path.Combine(localApplicationDataPath, subPath, "configAws_pattern.xml");
                awsConfigPattern.SaveToFile(pathToTestConfigAwsPattern);
                Assert.Fail("For AWS tests the to test config file must be found in: '" + pathToTestConfigAws + "'. Please create the file with valid endpoint+key+secret\n" +
                    "A pattern was saved in: '" + pathToTestConfigAwsPattern + "'");
            }
            AwsConfig awsConfig = AwsConfig.LoadFromFile<AwsConfig>(pathToTestConfigAws);


            string pathToConfig = "config" + Path.DirectorySeparatorChar;

            if (!Directory.Exists(pathToConfig))
            {
                Directory.CreateDirectory(pathToConfig);
            }
            if (File.Exists(pathToConfig + configFileName))
            {
                File.Delete(pathToConfig + configFileName);
            }

            awsConfig.SaveToFile(pathToConfig + configFileName);
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
