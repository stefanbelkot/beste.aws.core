using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Beste.Aws.Databases.Connector;
using Beste.Aws.Module;
using Beste.Core.Models;
using Beste.Databases.Connector;
using Beste.Databases.User;
using Beste.GameServer.SDaysTDie.Connections;
using Beste.Rights;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Beste.GameServer.SDaysTDie.Modules
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ModifySettingsResult
    {
        RIGHT_VIOLATION,
        GAME_SEED_ALREADY_EXISTS,
        SETTING_ADDED,
        USER_NOT_FOUND,
        EXCEPTION,
        SETTING_NOT_FOUND,
        SETTING_EDITED,
        SETTING_DELETED,
        SERVER_MUST_BE_STOPPED
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum GetSettingsResult
    {
        OK,
        RIGHT_VIOLATION,
        EXCEPTION,
        SETTING_NOT_FOUND
    }
    public class ModifySettingsResponse : IResponse<ModifySettingsResult>
    {
        public ModifySettingsResult Result { get; private set; }
        public ModifySettingsResponse(ModifySettingsResult result)
        {
            Result = result;
        }
    }
    public class GetSettingsResponse : IResponse<GetSettingsResult>
    {
        public GetSettingsResult Result { get; private set; }
        public List<ServerSetting> ServerSettings { get; private set; }
        public GetSettingsResponse(GetSettingsResult result, List<ServerSetting> serverSettings)
        {
            Result = result;
            ServerSettings = serverSettings;
        }
    }
    class ServerSettingsHandler
    {
        Random random = new Random();
        static Beste.Aws.Module.BesteUser BesteUser { get; set; } = new Beste.Aws.Module.BesteUser();
        public static int TABLE_ID = 1;
        public static RightControl RightControl { get; set; } = new RightControl("Beste.GameServer.SDaysTDie.ServerSettings");

        internal static async Task RegisterUser(User user, string token)
        {
            List<PureRight> pureRights = new List<PureRight>();
            pureRights.Add(new PureRight
            {
                Authorized = true,
                Operation = "AddServerSettings_" + user.Username,
                RecourceModule = "ServerSetting"
            });
            pureRights.Add(new PureRight
            {
                Authorized = true,
                Operation = "GetServerSettings_" + user.Username,
                RecourceModule = "ServerSetting"
            });

            
            await RightControl.Register(user.Uuid, pureRights, token);
        }



        internal async static Task AddServerSettings(WebSocketHandler webSocketHandler)
        {
            Command resonseCommand = await ModifyServerSettings(async () =>
            {
                    ServerSetting serverSettings = JsonConvert.DeserializeObject<ServerSetting>(webSocketHandler.ReceivedCommand.CommandData.ToString());
                    return await AddServerSetting(serverSettings, webSocketHandler.User, webSocketHandler.ConnectedUserToken);
                },
                "AddServerSettings",
                webSocketHandler);
            await webSocketHandler.Send(resonseCommand);
        }
        internal async static Task EditServerSettings(WebSocketHandler webSocketHandler)
        {
            Command resonseCommand = await ModifyServerSettings(async () =>
            {
                ServerSetting serverSettings = JsonConvert.DeserializeObject<ServerSetting>(webSocketHandler.ReceivedCommand.CommandData.ToString());
                return await EditServerSetting(serverSettings, webSocketHandler.User, webSocketHandler.ConnectedUserToken);
            },
                "EditServerSettings",
                webSocketHandler,
                JsonConvert.DeserializeObject<ServerSetting>(webSocketHandler.ReceivedCommand.CommandData.ToString()).Id);
            await webSocketHandler.Send(resonseCommand);
        }
        internal async static Task DeleteServerSettings(WebSocketHandler webSocketHandler)
        {
            Command resonseCommand = await ModifyServerSettings(async () =>
            {
                ServerSetting serverSettings = JsonConvert.DeserializeObject<ServerSetting>(webSocketHandler.ReceivedCommand.CommandData.ToString());
                return await DeleteServerSetting(serverSettings, webSocketHandler.User, webSocketHandler.ConnectedUserToken);
            },
                "DeleteServerSettings",
                webSocketHandler,
                JsonConvert.DeserializeObject<ServerSetting>(webSocketHandler.ReceivedCommand.CommandData.ToString()).Id);
            await webSocketHandler.Send(resonseCommand);
        }


        private static async Task<Command> ModifyServerSettings(Func<Task<ModifySettingsResponse>> modifyAction, string actionName, WebSocketHandler webSocketHandler, int? ressourceId = null)
        {
            ModifySettingsResponse response = null;
            if (RightControl.IsGranted(webSocketHandler.ConnectedUserToken, actionName, "ServerSetting", ressourceId))
            {
                response = await modifyAction();
            }
            else if (RightControl.IsGranted(webSocketHandler.ConnectedUserToken, actionName + "_" + webSocketHandler.User.Username, "ServerSetting"))
            {
                response = await modifyAction();
            }
            else
            {
                response = new ModifySettingsResponse(ModifySettingsResult.RIGHT_VIOLATION);
            }
            return new Command(actionName + "Response", response);
        }

        private static async Task<ModifySettingsResponse> AddServerSetting(ServerSetting serverSettings, User user, string token)
        {
            return await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                try
                {
                    QueryResponse response = await GetServerSettingBySpecificProperty("WorldGenSeed", serverSettings.WorldGenSeed);
                    if (response.Items.Count > 0)
                    {
                        return new ModifySettingsResponse(ModifySettingsResult.GAME_SEED_ALREADY_EXISTS);
                    }
                    GetUsersResponse getUsersResponse = await BesteUser.GetUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
                    if (getUsersResponse.Result != GetUsersResult.SUCCESS)
                    {
                        return new ModifySettingsResponse(ModifySettingsResult.USER_NOT_FOUND);
                    }
                    serverSettings.TableId = TABLE_ID;
                    serverSettings.UserUuid = getUsersResponse.Users[0].Uuid;
                    serverSettings.ServerConfigFilepath = "";
                    serverSettings.ServerPort = 0;
                    serverSettings.TelnetPassword = "";
                    serverSettings.TelnetPort = 0;
                    serverSettings.TerminalWindowEnabled = false;
                    serverSettings.Id = await GenerateNewServerSettingsId();

                    await CreateRightsForNewServerSettings(serverSettings, user, token);

                    await context.SaveAsync(serverSettings);
                    return new ModifySettingsResponse(ModifySettingsResult.SETTING_ADDED);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString() + "\n" + ex.StackTrace);
                    return new ModifySettingsResponse(ModifySettingsResult.EXCEPTION);
                }
            });
        }

        private static async Task<int> GenerateNewServerSettingsId()
        {
            return await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                Random random = new Random();
                for(int tries = 0; tries < 5; tries++)
                {
                    byte[] buf = new byte[4];
                    random.NextBytes(buf);
                    int generatedId = BitConverter.ToInt32(buf, 0);
                    ServerSetting serverSetting = new ServerSetting
                    {
                        TableId = TABLE_ID,
                        Id = generatedId
                    };
                    serverSetting = await context.LoadAsync(serverSetting);
                    //QueryResponse response = await GetServerSettingBySpecificProperty("Id", generatedId);
                    if (serverSetting == null)
                    {
                        return generatedId;
                    }
                }
                throw new Exception("Was not able to generate a Unique Id for ServerSetting!");
            });
        }

        private static async Task<QueryResponse> GetServerSettingByWorldGenSeed(string worldGenSeed)
        {
            return await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                var request = new QueryRequest
                {
                    TableName = ServerSetting.TableName,
                    //ScanIndexForward = false,
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
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        { "#WorldGenSeed", "WorldGenSeed" }
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":WorldGenSeed", new AttributeValue { S = worldGenSeed } }
                    },
                    FilterExpression = "#WorldGenSeed = :WorldGenSeed"
                };
                return await client.QueryAsync(request);
            });
        }

        private static async Task<QueryResponse> GetServerSettingBySpecificProperty(string propertyName, string propertyValue)
        {
            return await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                var request = new QueryRequest
                {
                    TableName = ServerSetting.TableName,
                    //ScanIndexForward = false,
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
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        { "#name", propertyName }
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":value", new AttributeValue { S = propertyValue } }
                    },
                    FilterExpression = "#name = :value"
                };
                return await client.QueryAsync(request);
            });
        }
        private static async Task<QueryResponse> GetServerSettingBySpecificProperty(string propertyName, int propertyValue)
        {
            return await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                var request = new QueryRequest
                {
                    TableName = ServerSetting.TableName,
                    //ScanIndexForward = false,
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
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        { "#Property", propertyName }
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":Property", new AttributeValue { N = propertyValue.ToString() } }
                    },
                    FilterExpression = "#Property = :Property"
                };
                var response = await client.QueryAsync(request);
                return response;
            });
        }
        private static async Task CreateRightsForNewServerSettings(ServerSetting serverSettings, User user, string token)
        {
            await AddOrUpdateServerSettingRight("GetServerSettings", serverSettings.Id, user.Uuid, token);
            await AddOrUpdateServerSettingRight("EditServerSettings", serverSettings.Id, user.Uuid, token);
            await AddOrUpdateServerSettingRight("DeleteServerSettings", serverSettings.Id, user.Uuid, token);
            await AddOrUpdateServerSettingRight("UseServerSettings", serverSettings.Id, user.Uuid, token);
        }

        private static async Task AddOrUpdateServerSettingRight(string operation, int serverSettingsId, string userUuid, string token)
        {
            BesteRightsAuthorization besteRightsAuthorizationDb = await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                var request = new QueryRequest
                {
                    TableName = BesteRightsAuthorization.TableName,
                    //ScanIndexForward = false,
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
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        { "#Namespace", "Namespace" },
                        { "#Operation", "Operation" },
                        { "#RecourceModule", "RecourceModule" },
                        { "#RecourceId", "RecourceId" }
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":Namespace", new AttributeValue { S =  "Beste.GameServer.SDaysTDie.ServerSettings" } },
                        { ":Operation", new AttributeValue { S =  operation } },
                        { ":RecourceModule", new AttributeValue { S =  "ServerSetting" } },
                        { ":RecourceId", new AttributeValue { N =  serverSettingsId.ToString() } }
                    },
                    FilterExpression = "#Namespace = :Namespace and" +
                        "#Operation = :Operation and" +
                        "#RecourceModule = :RecourceModule and" +
                        "#RecourceId = :RecourceId"
                };
                var response = await client.QueryAsync(request);
                if(response.Items.Count > 0)
                {
                    return new BesteRightsAuthorization
                    {
                        TableId = TABLE_ID,
                        Uuid = response.Items[0]["Uuid"].S
                    };
                }
                return null;
            });
            await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                BesteRightsAuthorization besteRightsAuthorization = new BesteRightsAuthorization
                {
                    TableId = TABLE_ID,
                    Namespace = "Beste.GameServer.SDaysTDie.ServerSettings",
                    Operation = operation,
                    RecourceModule = "ServerSetting",
                    RecourceId = serverSettingsId,
                    Authorized = true,
                    LegitimationUuid = userUuid,
                    Uuid = besteRightsAuthorizationDb == null ? 
                        Guid.NewGuid().ToString() :
                        besteRightsAuthorizationDb.Uuid
                };
                await context.SaveAsync(besteRightsAuthorization);
            });
            RightControl.Grant(token, operation, "ServerSetting", serverSettingsId);
        }

        private static async Task<ModifySettingsResponse> EditServerSetting(ServerSetting serverSettings, User user, string token)
        {
            ServerSetting dbServerSetting = await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                serverSettings.TableId = TABLE_ID;
                return await context.LoadAsync(serverSettings);
            });
            return await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                try
                {
                    if (dbServerSetting == null)
                    {
                        return new ModifySettingsResponse(ModifySettingsResult.SETTING_NOT_FOUND);
                    }
                    if (SDaysTDieServerHandler.IsServerRunningBySeed(dbServerSetting.WorldGenSeed))
                    {
                        return new ModifySettingsResponse(ModifySettingsResult.SERVER_MUST_BE_STOPPED);
                    }
                    QueryResponse response = await GetServerSettingBySpecificProperty("WorldGenSeed", serverSettings.WorldGenSeed);
                    if (response.Items.Count > 0 && Convert.ToInt32(response.Items[0]["Id"].N.ToString()) != dbServerSetting.Id)
                    {
                        return new ModifySettingsResponse(ModifySettingsResult.GAME_SEED_ALREADY_EXISTS);
                    }
                    string oldUserId = dbServerSetting.UserUuid;
                    GetUsersResponse getUsersResponse = await BesteUser.GetUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
                    if (getUsersResponse.Result != GetUsersResult.SUCCESS)
                    {
                        return new ModifySettingsResponse(ModifySettingsResult.USER_NOT_FOUND);
                    }
                    User dbUser = getUsersResponse.Users[0];
                    serverSettings.UserUuid = dbUser.Uuid;
                    serverSettings.CopyAllButId(dbServerSetting);
                    if (oldUserId != getUsersResponse.Users[0].Uuid)
                    {
                        await AddOrUpdateServerSettingRight("EditServerSettings", serverSettings.Id, dbUser.Uuid, token);
                    }
                    await context.SaveAsync(dbServerSetting);
                    return new ModifySettingsResponse(ModifySettingsResult.SETTING_EDITED);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString() + "\n" + ex.StackTrace);
                    return new ModifySettingsResponse(ModifySettingsResult.EXCEPTION);
                }
            });
        }

        private static async Task<ModifySettingsResponse> DeleteServerSetting(ServerSetting serverSettings, User user, string connectedUserToken)
        {
            ServerSetting dbServerSetting = await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                serverSettings.TableId = TABLE_ID;
                return await context.LoadAsync(serverSettings);
            });
            return await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                try
                {
                    if (dbServerSetting == null)
                    {
                        return new ModifySettingsResponse(ModifySettingsResult.SETTING_NOT_FOUND);
                    }
                    if (SDaysTDieServerHandler.IsServerRunningBySeed(dbServerSetting.WorldGenSeed))
                    {
                        return new ModifySettingsResponse(ModifySettingsResult.SERVER_MUST_BE_STOPPED);
                    }
                    await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (clientDelete, contextDelete) =>
                    {
                        await contextDelete.DeleteAsync(dbServerSetting);
                    });
                    await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (clientDelete, contextDelete) =>
                    {
                        await DeleteAllRightsByServerSettingId(dbServerSetting.Id);
                    });
                    return new ModifySettingsResponse(ModifySettingsResult.SETTING_DELETED);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString() + "\n" + ex.StackTrace);
                    return new ModifySettingsResponse(ModifySettingsResult.EXCEPTION);
                }
            });
        }

        private static async Task DeleteAllRightsByServerSettingId(int serverSettingId)
        {

            await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                var request = new QueryRequest
                {
                    TableName = BesteRightsAuthorization.TableName,
                    //ScanIndexForward = false,
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
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        { "#Namespace", "Namespace" },
                        { "#RecourceModule", "RecourceModule" },
                        { "#RecourceId", "RecourceId" }
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":Namespace", new AttributeValue { S =  "Beste.GameServer.SDaysTDie.ServerSettings" } },
                        { ":RecourceModule", new AttributeValue { S =  "ServerSetting" } },
                        { ":RecourceId", new AttributeValue { N =  serverSettingId.ToString() } }
                    },
                    FilterExpression = "#Namespace = :Namespace and" +
                      "#RecourceModule = :RecourceModule and" +
                      "#RecourceId = :RecourceId"
                };
                var response = await client.QueryAsync(request);
                foreach (var item in response.Items)
                {
                    await context.DeleteAsync(new BesteRightsAuthorization
                    {
                        TableId = TABLE_ID,
                        Uuid = item["Uuid"].S
                    });
                }
            });
 
        }

        internal async static Task GetServerSettingsOfLoggedInUser(WebSocketHandler webSocketHandler)
        {
            Command resonseCommand = await GetServerSettings(async () =>
            {
                return await GetServerSettingsOfUser(webSocketHandler.User);
            },
                "GetServerSettings",
                webSocketHandler,
                null);
            await webSocketHandler.Send(resonseCommand);
        }
        internal async static Task GetServerSettingsById(WebSocketHandler webSocketHandler)
        {
            ServerSetting serverSettings = JsonConvert.DeserializeObject<ServerSetting>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            Command resonseCommand = await GetServerSettings(async () =>
            {
                return await GetServerSettingsByIdAndUser(webSocketHandler.User, serverSettings.Id);
            },
                "GetServerSettings",
                webSocketHandler,
                serverSettings.Id);
            await webSocketHandler.Send(resonseCommand);
        }
        internal async static Task<GetSettingsResponse> GetServerSettingsByIdAndUser(WebSocketHandler webSocketHandler, User user, int serverSettingId)
        {
            Command resonseCommand = await GetServerSettings(async () =>
            {
                return await GetServerSettingsByIdAndUser(user, serverSettingId);
            },
                "GetServerSettings",
                webSocketHandler,
                serverSettingId);
            return (GetSettingsResponse)resonseCommand.CommandData;
        }

        internal static async Task<GetSettingsResponse> GetServerSettingsByIdAndUser(User user, int serverSettingId)
        {
            ServerSetting dbServerSetting = await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                ServerSetting serverSetting = new ServerSetting
                {
                    TableId = TABLE_ID,
                    Id = serverSettingId
                };
                return await context.LoadAsync(serverSetting);
            });
            List<ServerSetting> serverSettings = new List<ServerSetting> { dbServerSetting };
            return new GetSettingsResponse(GetSettingsResult.OK, serverSettings);
        }

        private static async Task<GetSettingsResponse> GetServerSettingsOfUser(User user)
        {
            QueryResponse response = await GetServerSettingBySpecificProperty("UserUuid", user.Uuid);
            List<ServerSetting> serverSettings = new List<ServerSetting>();
            response.Items.ForEach(item =>
            {
                serverSettings.Add(ServerSetting.FromDynamoDbDictionary(item));
            });
            return new GetSettingsResponse(GetSettingsResult.OK, serverSettings);
        }

        private static async Task<Command> GetServerSettings(Func<Task<GetSettingsResponse>> getAction, string actionName, WebSocketHandler webSocketHandler, int? ressourceId = null)
        {
            try
            {
                GetSettingsResponse response = null;
                if (RightControl.IsGranted(webSocketHandler.ConnectedUserToken, actionName, "ServerSetting", ressourceId))
                {
                    response = await getAction();
                }
                else if (RightControl.IsGranted(webSocketHandler.ConnectedUserToken, actionName + "_" + webSocketHandler.User.Username, "ServerSetting"))
                {
                    response = await getAction();
                }
                else
                {
                    response = new GetSettingsResponse(GetSettingsResult.RIGHT_VIOLATION, null);
                }
                return new Command(actionName + "Response", response);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return new Command(actionName + "Response", GetSettingsResult.EXCEPTION);
            }
        }
        
    }
}
