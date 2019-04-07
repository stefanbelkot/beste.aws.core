using Beste.Core.Models;
using Beste.Databases.Connector;
using Beste.Databases.User;
using Beste.GameServer.SDaysTDie.Connections;
using Beste.Rights;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NHibernate;
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

        public static RightControl RightControl { get; set; } = new RightControl("Beste.GameServer.SDaysTDie.ServerSettings");

        internal static void RegisterUser(User user, string token)
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

            
            RightControl.Register(user.UserId, pureRights, token);
        }



        internal async static Task AddServerSettings(WebSocketHandler webSocketHandler)
        {
            Command resonseCommand = ModifyServerSettings(() => 
                {
                    ServerSetting serverSettings = JsonConvert.DeserializeObject<ServerSetting>(webSocketHandler.ReceivedCommand.CommandData.ToString());
                    return AddServerSetting(serverSettings, webSocketHandler.User, webSocketHandler.ConnectedUserToken);
                },
                "AddServerSettings",
                webSocketHandler);
            await webSocketHandler.Send(resonseCommand);
        }
        internal async static Task EditServerSettings(WebSocketHandler webSocketHandler)
        {
            Command resonseCommand = ModifyServerSettings(() =>
            {
                ServerSetting serverSettings = JsonConvert.DeserializeObject<ServerSetting>(webSocketHandler.ReceivedCommand.CommandData.ToString());
                return EditServerSetting(serverSettings, webSocketHandler.User, webSocketHandler.ConnectedUserToken);
            },
                "EditServerSettings",
                webSocketHandler,
                JsonConvert.DeserializeObject<ServerSetting>(webSocketHandler.ReceivedCommand.CommandData.ToString()).Id);
            await webSocketHandler.Send(resonseCommand);
        }
        internal async static Task DeleteServerSettings(WebSocketHandler webSocketHandler)
        {
            Command resonseCommand = ModifyServerSettings(() =>
            {
                ServerSetting serverSettings = JsonConvert.DeserializeObject<ServerSetting>(webSocketHandler.ReceivedCommand.CommandData.ToString());
                return DeleteServerSetting(serverSettings, webSocketHandler.User, webSocketHandler.ConnectedUserToken);
            },
                "DeleteServerSettings",
                webSocketHandler,
                JsonConvert.DeserializeObject<ServerSetting>(webSocketHandler.ReceivedCommand.CommandData.ToString()).Id);
            await webSocketHandler.Send(resonseCommand);
        }


        private static Command ModifyServerSettings(Func<ModifySettingsResponse> modifyAction, string actionName, WebSocketHandler webSocketHandler, int? ressourceId = null)
        {
            ModifySettingsResponse response = null;
            if (RightControl.IsGranted(webSocketHandler.ConnectedUserToken, actionName, "ServerSetting", ressourceId))
            {
                response = modifyAction();
            }
            else if (RightControl.IsGranted(webSocketHandler.ConnectedUserToken, actionName + "_" + webSocketHandler.User.Username, "ServerSetting"))
            {
                response = modifyAction();
            }
            else
            {
                response = new ModifySettingsResponse(ModifySettingsResult.RIGHT_VIOLATION);
            }
            return new Command(actionName + "Response", response);
        }

        private static ModifySettingsResponse AddServerSetting(ServerSetting serverSettings, User user, string token)
        {
            using (NHibernate.IStatelessSession session = SessionFactory.GetStatelessSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                try
                {
                    if (session.QueryOver<ServerSetting>()
                        .Where(p => p.WorldGenSeed == serverSettings.WorldGenSeed)
                        .SingleOrDefault() != null)
                    {
                        return new ModifySettingsResponse(ModifySettingsResult.GAME_SEED_ALREADY_EXISTS);
                    }
                    User dbUser = session.QueryOver<User>()
                        .Where(p => p.Username == user.Username)
                        .SingleOrDefault();
                    if (dbUser == null)
                    {
                        return new ModifySettingsResponse(ModifySettingsResult.USER_NOT_FOUND);
                    }
                    serverSettings.User = dbUser;
                    serverSettings.ServerConfigFilepath = "";
                    serverSettings.ServerPort = 0;
                    serverSettings.TelnetPassword = "";
                    serverSettings.TelnetPort = 0;
                    serverSettings.TerminalWindowEnabled = false;
                    session.Insert(serverSettings);

                    CreateRightsForNewServerSettings(serverSettings, user, token, session);

                    transaction.Commit();
                    return new ModifySettingsResponse(ModifySettingsResult.SETTING_ADDED);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString() + "\n" + ex.StackTrace);
                    return new ModifySettingsResponse(ModifySettingsResult.EXCEPTION);
                }
            }
        }

        private static void CreateRightsForNewServerSettings(ServerSetting serverSettings, User user, string token, IStatelessSession session)
        {
            BesteRightsNamespace besteRightsNamespace = session.QueryOver<BesteRightsNamespace>()
                .Where(p => p.Name == "Beste.GameServer.SDaysTDie.ServerSettings")
                .SingleOrDefault();
            if (besteRightsNamespace == null)
                throw new Exception();

            AddOrUpdateServerSettingRight(session, "GetServerSettings", serverSettings.Id, user.UserId, token, besteRightsNamespace);
            AddOrUpdateServerSettingRight(session, "EditServerSettings", serverSettings.Id, user.UserId, token, besteRightsNamespace);
            AddOrUpdateServerSettingRight(session, "DeleteServerSettings", serverSettings.Id, user.UserId, token, besteRightsNamespace);
            AddOrUpdateServerSettingRight(session, "UseServerSettings", serverSettings.Id, user.UserId, token, besteRightsNamespace);

        }

        private static void AddOrUpdateServerSettingRight(IStatelessSession session, string operation, int serverSettingsId, int userId, string token, BesteRightsNamespace besteRightsNamespace)
        {
            
            BesteRightsDefinition besteRightsDefinition = null;
            besteRightsDefinition = session.QueryOver(() => besteRightsDefinition)
                .JoinAlias(() => besteRightsDefinition.BesteRightsNamespace, () => besteRightsNamespace)
                .Where(p => p.BesteRightsNamespace == besteRightsNamespace)
                .And(p => p.Operation == operation && p.RecourceId == serverSettingsId && p.RecourceModule == "ServerSetting")
                .SingleOrDefault();
            if(besteRightsDefinition == null)
            {
                besteRightsDefinition = new BesteRightsDefinition();
                besteRightsDefinition.BesteRightsNamespace = besteRightsNamespace;
                besteRightsDefinition.Operation = operation;
                besteRightsDefinition.RecourceModule = "ServerSetting";
                besteRightsDefinition.RecourceId = serverSettingsId;
            }

            BesteRightsAuthorization besteRightsAuthorization = null;
            besteRightsAuthorization = session.QueryOver(() => besteRightsAuthorization)
                .JoinAlias(() => besteRightsAuthorization.BesteRightsDefinition, () => besteRightsDefinition)
                .Where(p => p.BesteRightsDefinition.Id == besteRightsDefinition.Id)
                .And(p => p.LegitimationId == userId)
                .SingleOrDefault();
            if (besteRightsAuthorization == null)
            {
                besteRightsAuthorization = new BesteRightsAuthorization();
                besteRightsAuthorization.Authorized = true;
                besteRightsAuthorization.BesteRightsDefinition = besteRightsDefinition;
            }
            besteRightsAuthorization.LegitimationId = userId;

            //BesteRightsAuthorization besteRightsAuthorization;
            //besteRightsAuthorization = new BesteRightsAuthorization();
            //besteRightsAuthorization.LegitimationId = userId;
            //besteRightsAuthorization.Authorized = true;
            //besteRightsAuthorization.BesteRightsDefinition = besteRightsDefinition;

            session.Insert(besteRightsDefinition);
            session.Insert(besteRightsAuthorization);
            RightControl.Grant(token, operation, "ServerSetting", serverSettingsId);
        }

        private static ModifySettingsResponse EditServerSetting(ServerSetting serverSettings, User user, string token)
        {
            using (NHibernate.ISession session = SessionFactory.GetSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                try
                {
                    ServerSetting dbServerSetting = session.QueryOver<ServerSetting>()
                        .Where(p => p.Id == serverSettings.Id)
                        .SingleOrDefault();
                    if (dbServerSetting == null)
                    {
                        return new ModifySettingsResponse(ModifySettingsResult.SETTING_NOT_FOUND);
                    }
                    if (SDaysTDieServerHandler.IsServerRunningBySeed(dbServerSetting.WorldGenSeed))
                    {
                        return new ModifySettingsResponse(ModifySettingsResult.SERVER_MUST_BE_STOPPED);
                    }
                    ServerSetting checkExistingGameSeed = session.QueryOver<ServerSetting>()
                        .Where(p => p.WorldGenSeed == serverSettings.WorldGenSeed)
                        .SingleOrDefault();
                    if (checkExistingGameSeed != null && checkExistingGameSeed.Id != dbServerSetting.Id)
                    {
                        return new ModifySettingsResponse(ModifySettingsResult.GAME_SEED_ALREADY_EXISTS);
                    }
                    int oldUserId = dbServerSetting.User.UserId;
                    User dbUser = session.QueryOver<User>()
                        .Where(p => p.Username == user.Username)
                        .SingleOrDefault();
                    serverSettings.User = dbUser;
                    serverSettings.CopyAllButId(dbServerSetting);
                    if(oldUserId != dbUser.UserId)
                    {
                        BesteRightsAuthorization besteRightsAuthorization = session.QueryOver<BesteRightsAuthorization>()
                            .Where(p => p.LegitimationId == oldUserId)
                            .SingleOrDefault();
                        besteRightsAuthorization.LegitimationId = dbUser.UserId;
                        if (RightControl.IsDenied(token, "EditServerSettings", "ServerSetting", dbUser.UserId))
                            RightControl.Grant(token, "EditServerSettings", "ServerSetting", serverSettings.Id);
                        session.Save(besteRightsAuthorization);
                    }

                    session.Save(dbServerSetting);
                    transaction.Commit();
                    return new ModifySettingsResponse(ModifySettingsResult.SETTING_EDITED);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString() + "\n" + ex.StackTrace);
                    return new ModifySettingsResponse(ModifySettingsResult.EXCEPTION);
                }
            }
        }

        private static ModifySettingsResponse DeleteServerSetting(ServerSetting serverSettings, User user, string connectedUserToken)
        {
            using (NHibernate.ISession session = SessionFactory.GetSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                try
                {
                    ServerSetting dbServerSetting = session.QueryOver<ServerSetting>()
                        .Where(p => p.Id == serverSettings.Id)
                        .SingleOrDefault();
                    if (dbServerSetting == null)
                    {
                        return new ModifySettingsResponse(ModifySettingsResult.SETTING_NOT_FOUND);
                    }
                    if(SDaysTDieServerHandler.IsServerRunningBySeed(dbServerSetting.WorldGenSeed))
                    {
                        return new ModifySettingsResponse(ModifySettingsResult.SERVER_MUST_BE_STOPPED);
                    }

                    session.Delete(dbServerSetting);

                    BesteRightsDefinition besteRightsDefinition = null;
                    BesteRightsAuthorization besteRightsAuthorization = null;
                    var result = session.QueryOver<BesteRightsAuthorization>(() => besteRightsAuthorization)
                        .JoinAlias(() => besteRightsAuthorization.BesteRightsDefinition, () => besteRightsDefinition)
                        .Where(() => besteRightsDefinition.RecourceId == dbServerSetting.Id)
                        .List<BesteRightsAuthorization>();
                    List<BesteRightsDefinition> besteRightsDefinitions = new List<BesteRightsDefinition>(); 
                    foreach (var item in result)
                    {
                        if (!besteRightsDefinitions.Contains(item.BesteRightsDefinition))
                            besteRightsDefinitions.Add(item.BesteRightsDefinition);
                        session.Delete(item);
                    }
                    foreach (var item in besteRightsDefinitions)
                    {
                        session.Delete(item);
                    }
                    transaction.Commit();
                    return new ModifySettingsResponse(ModifySettingsResult.SETTING_DELETED);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString() + "\n" + ex.StackTrace);
                    return new ModifySettingsResponse(ModifySettingsResult.EXCEPTION);
                }
            }
        }


        internal async static Task GetServerSettingsOfLoggedInUser(WebSocketHandler webSocketHandler)
        {
            Command resonseCommand = GetServerSettings(() =>
            {
                //ServerSetting serverSettings = JsonConvert.DeserializeObject<ServerSetting>(webSocketHandler.ReceivedCommand.CommandData.ToString());
                return GetServerSettingsOfUser(webSocketHandler.User);
            },
                "GetServerSettings",
                webSocketHandler,
                webSocketHandler.User.UserId);
            await webSocketHandler.Send(resonseCommand);
        }
        internal async static Task GetServerSettingsById(WebSocketHandler webSocketHandler)
        {
            Command resonseCommand = GetServerSettings(() =>
            {
                ServerSetting serverSettings = JsonConvert.DeserializeObject<ServerSetting>(webSocketHandler.ReceivedCommand.CommandData.ToString());
                return GetServerSettingsByIdAndUser(webSocketHandler.User, serverSettings.Id);
            },
                "GetServerSettings",
                webSocketHandler,
                JsonConvert.DeserializeObject<User>(webSocketHandler.ReceivedCommand.CommandData.ToString()).UserId);
            await webSocketHandler.Send(resonseCommand);
        }
        internal static GetSettingsResponse GetServerSettingsByIdAndUser(WebSocketHandler webSocketHandler, User user, int serverSettingId)
        {
            Command resonseCommand = GetServerSettings(() =>
            {
                return GetServerSettingsByIdAndUser(user, serverSettingId);
            },
                "GetServerSettings",
                webSocketHandler,
                JsonConvert.DeserializeObject<User>(webSocketHandler.ReceivedCommand.CommandData.ToString()).UserId);
            return (GetSettingsResponse)resonseCommand.CommandData;
        }

        internal static GetSettingsResponse GetServerSettingsByIdAndUser(User user, int serverSettingId)
        {
            List<ServerSetting> serverSettings = SessionFactory.ExecuteInTransactionContext<List<ServerSetting>>((session, transaction) =>
            {
                ServerSetting serverSetting = null;
                var result = session.QueryOver<ServerSetting>(() => serverSetting)
                    .JoinAlias(() => serverSetting.User, () => user)
                    .Where(k => k.User.UserId == user.UserId &&
                        k.Id == serverSettingId)
                    .List<ServerSetting>();
                return new List<ServerSetting>(result);
            });
            return new GetSettingsResponse(GetSettingsResult.OK, serverSettings);
        }

        private static GetSettingsResponse GetServerSettingsOfUser(User user)
        {
            List<ServerSetting> serverSettings = SessionFactory.ExecuteInTransactionContext<List<ServerSetting>>((session, transaction) =>
            {
                ServerSetting serverSetting = null;
                var result = session.QueryOver<ServerSetting>(() => serverSetting)
                    .JoinAlias(() => serverSetting.User, () => user)
                    .Where(k => k.User.UserId == user.UserId)
                    .List<ServerSetting>();
                List<ServerSetting> convertedServerSettings = new List<ServerSetting>();
                foreach(ServerSetting item in result)
                {
                    item.IsRunning = SDaysTDieServerHandler.IsServerRunningBySeed(item.WorldGenSeed);
                    convertedServerSettings.Add(item);
                }
                return convertedServerSettings;
            });
            return new GetSettingsResponse(GetSettingsResult.OK, serverSettings);
        }

        private static Command GetServerSettings(Func<GetSettingsResponse> getAction, string actionName, WebSocketHandler webSocketHandler, int? ressourceId = null)
        {
            try
            {
                GetSettingsResponse response = null;
                if (RightControl.IsGranted(webSocketHandler.ConnectedUserToken, actionName, "ServerSetting", ressourceId))
                {
                    response = getAction();
                }
                else if (RightControl.IsGranted(webSocketHandler.ConnectedUserToken, actionName + "_" + webSocketHandler.User.Username, "ServerSetting"))
                {
                    response = getAction();
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
