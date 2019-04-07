using Beste.Core.Models;
using Beste.Databases.User;
using Beste.GameServer.SDaysTDie.Connections;
using Beste.GameServer.SDaysTDie.Extensions;
using Beste.Rights;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Beste.GameServer.SDaysTDie.Modules
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum StartServerResult
    {
        RIGHT_VIOLATION,
        RIGHT_VIOLATION_SERVERSETTING,
        SERVER_STARTED,
        SERVER_ALREADY_RUNNING,
        SERVER_COUNT_OF_USER_EXCEEDING,
        SETTING_NOT_FOUND,
        UNKNOWN_SETTINGS_RESPONSE,
        EXCEPTION,
        NO_FREE_PORT,
        SERVER_COUNT_OF_MACHINE_EXCEEDING
    }
    public class StartServerResponse : IResponse<StartServerResult>
    {
        public StartServerResult Result { get; private set; }
        public StartServerResponse(StartServerResult result)
        {
            Result = result;
        }
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum StopServerResult
    {
        RIGHT_VIOLATION,
        SERVER_STOPPED,
        SERVER_KILLED,
        EXCEPTION,
        FAILED_UNKNOWN_REASON,
        STOPPING,
        SERVER_NOT_RUNNING
    }
    public class StopServerResponse : IResponse<StopServerResult>
    {
        public StopServerResult Result { get; private set; }
        public StopServerResponse(StopServerResult result)
        {
            Result = result;
        }
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ConnectTelnetResult
    {
        RIGHT_VIOLATION,
        SERVER_NOT_RUNNING,
        EXCEPTION,
        OK
    }
    public class ConnectTelnetResponse : IResponse<ConnectTelnetResult>
    {
        public ConnectTelnetResult Result { get; private set; }
        public ConnectTelnetResponse(ConnectTelnetResult result)
        {
            Result = result;
        }
    }

    class SDaysTDieServerHandler
    {
        static Dictionary<string, SDaysTDieServer> SDaysTDieServersByUserIds { get; set; } = new Dictionary<string, SDaysTDieServer>();

        public static RightControl RightControl { get; set; } = new RightControl("Beste.GameServer.SDaysTDie.ServerHandler");
        public static SDaysTDieServerHandler MyInstance { get; set; } = new SDaysTDieServerHandler();
        private static char SEP { get; set; } = Path.DirectorySeparatorChar;
        //todo Move the SDaysToDiePath to settings
        //todo Later on create own instances for each user?
        private static string SDaysToDiePath { get; set; } = "C:" + SEP + "Program Files (x86)" + SEP + "Steam" + SEP + "steamapps" + SEP + "common" + SEP + "7 Days To Die";

        private Dictionary<string, SDaysTDieServerHandler> UserInstances { get; set; } = new Dictionary<string, SDaysTDieServerHandler>();
        private SDaysTDieServerHandler()
        {
        }
        internal static async Task RegisterUser(User user, string token)
        {
            List<PureRight> pureRights = new List<PureRight>();
            pureRights.Add(new PureRight
            {
                Authorized = true,
                Operation = "StartServer",
                RecourceModule = "ServerHandler"
            });
            string otherToken = await RightControl.Register(user.Uuid, pureRights, token);
        }


        #region "StartServer"
        internal async static Task StartServer(WebSocketHandler webSocketHandler)
        {
            StartServerResponse response = await TryStartServer(webSocketHandler);
            Command command = new Command("StartServerResponse", response);
            await webSocketHandler.Send(command);
        }

        private static async Task<StartServerResponse> TryStartServer(WebSocketHandler webSocketHandler)
        {
            StartServerResponse response = null;
            ServerSetting serverSetting = JsonConvert.DeserializeObject<ServerSetting>(webSocketHandler.ReceivedCommand.CommandData.ToString());

            if (!HasUserStartServerAndUseSettingsRights(webSocketHandler, serverSetting))
            {
                return new StartServerResponse(StartServerResult.RIGHT_VIOLATION);
            }

            GetSettingsResponse getSettingsResponse = await ServerSettingsHandler.GetServerSettingsByIdAndUser(webSocketHandler, webSocketHandler.User, serverSetting.Id);
            if (getSettingsResponse.Result != GetSettingsResult.OK)
            {
                return ExtractNotOKGetSettingsResult(response, getSettingsResponse);
            }
            else
            {
                return StartServerBySettingsResponse(getSettingsResponse);
            }
        }

        private static StartServerResponse StartServerBySettingsResponse(GetSettingsResponse getSettingsResponse)
        {
            ServerSettings serverSettings = ServerSettings.LoadFromFile<ServerSettings>(SDaysToDiePath + SEP + "serverconfig.xml");
            serverSettings.FromServerSetting(getSettingsResponse.ServerSettings[0]);
            if (IsServerCountOfUserExceeding(serverSettings))
            {
                return new StartServerResponse(StartServerResult.SERVER_COUNT_OF_USER_EXCEEDING);
            }
            if (IsServerLimitOnMachineReached())
            {
                return new StartServerResponse(StartServerResult.SERVER_COUNT_OF_MACHINE_EXCEEDING);
            }
            else if (IsServerOfSettingsRunning(serverSettings))
            {
                return new StartServerResponse(StartServerResult.SERVER_ALREADY_RUNNING);
            }
            else
            {
                return StartServerByServerSettings(serverSettings);
            }
        }

        private static bool IsServerLimitOnMachineReached()
        {
            //todo get max servers on local machine from settings file
            return (SDaysTDieServersByUserIds.Count >= 2);
        }

        private static bool IsServerCountOfUserExceeding(ServerSettings serverSettings)
        {
            return SDaysTDieServersByUserIds.ContainsKey(serverSettings.UserUuid);
        }

        private static bool HasUserStartServerAndUseSettingsRights(WebSocketHandler webSocketHandler, ServerSetting serverSetting)
        {
            if (!ServerSettingsHandler.RightControl.IsGranted(webSocketHandler.ConnectedUserToken, "UseServerSettings", "ServerSetting", serverSetting.Id) &&
                !ServerSettingsHandler.RightControl.IsGranted(webSocketHandler.ConnectedUserToken, "UseServerSettings", "ServerSetting"))
            {
                return false;
            }

            if (!RightControl.IsGranted(webSocketHandler.ConnectedUserToken, "StartServer", "ServerHandler", null))
            {
                return false;
            }

            return true;
        }
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        private static StartServerResponse StartServerByServerSettings(ServerSettings serverSettings)
        {
            StartServerResponse response;
            try
            {
                //serverSettings.ServerConfigFilepath = serverSettings.ServerConfigFilepath.Replace(".xml", serverSettings.WorldGenSeed + ".xml");
                serverSettings.ServerConfigFilepath = serverSettings.ServerConfigFilepath.Replace(".xml", RandomString(12) + ".xml");
                //todo set ports range from settings
                serverSettings.ServerPort = GetFreePortInRange(26903, 26909);
                serverSettings.TelnetPort = GetFreePortInRange(8083, 8089);
                if (serverSettings.ServerPort == 0 || serverSettings.TelnetPort == 0)
                {
                    return new StartServerResponse(StartServerResult.NO_FREE_PORT);
                }
                serverSettings.SaveToFile(SDaysToDiePath + SEP + serverSettings.ServerConfigFilepath);
                SDaysTDieServersByUserIds.Add(serverSettings.UserUuid, new SDaysTDieServer(SDaysToDiePath, serverSettings));
                SDaysTDieServersByUserIds[serverSettings.UserUuid].Start();
                response = new StartServerResponse(StartServerResult.SERVER_STARTED);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                response = new StartServerResponse(StartServerResult.EXCEPTION);
            }
            return response;
        }


        private static StartServerResponse ExtractNotOKGetSettingsResult(StartServerResponse response, GetSettingsResponse getSettingsResponse)
        {
            switch (getSettingsResponse.Result)
            {
                case GetSettingsResult.RIGHT_VIOLATION:
                    response = new StartServerResponse(StartServerResult.RIGHT_VIOLATION_SERVERSETTING);
                    break;
                case GetSettingsResult.SETTING_NOT_FOUND:
                    response = new StartServerResponse(StartServerResult.SETTING_NOT_FOUND);
                    break;
                case GetSettingsResult.EXCEPTION:
                    response = new StartServerResponse(StartServerResult.EXCEPTION);
                    break;
                default:
                    response = new StartServerResponse(StartServerResult.UNKNOWN_SETTINGS_RESPONSE);
                    break;
            }
            return response;
        }

        #endregion

        #region "StopServer"
        internal async static Task StopServer(WebSocketHandler webSocketHandler)
        {
            ServerSetting serverSetting = JsonConvert.DeserializeObject<ServerSetting>(webSocketHandler.ReceivedCommand.CommandData.ToString());

            StopServerResponse response = null;
            if (!IsServerOfSettingsRunning(serverSetting))
            {
                response = new StopServerResponse(StopServerResult.SERVER_NOT_RUNNING);
            }
            else if (!HasUserStartServerAndUseSettingsRights(webSocketHandler, serverSetting))
            {
                response = new StopServerResponse(StopServerResult.RIGHT_VIOLATION);
            }
            else
            {
                response = await TryStopServer(serverSetting);
            }

            Command command = new Command("StopServerResponse", response);
            await webSocketHandler.Send(command);
        }
        internal async static Task<StopServerResponse> TryStopServer(ServerSetting serverSetting)
        {
            StopServerResponse response = null;
            KeyValuePair<string, SDaysTDieServer> keyValuePair = SDaysTDieServersByUserIds.FirstOrDefault(x => x.Value.ServerSettings.WorldGenSeed == serverSetting.WorldGenSeed);
            SDaysTDieServer sDaysTDieServer = keyValuePair.Value;
            StopServerResult stopServerResult = StopServerResult.STOPPING;
            void SDaysTDieServer_OnSDaysTDieServerStoppedHandler(SDaysTDieServer sender, SDaysTDieServer.OnSDaysTDieServerStoppedEventArgs e)
            {
                if (e.Message == "*** Shutdown successful ***" || e.Message == "*** Server already down! ***")
                    stopServerResult = StopServerResult.SERVER_STOPPED;
                else if (e.Message == "*** Shutdown done with killing process ***")
                    stopServerResult = StopServerResult.SERVER_KILLED;
            }
            sDaysTDieServer.OnSDaysTDieServerStoppedHandler += SDaysTDieServer_OnSDaysTDieServerStoppedHandler;
            keyValuePair.Value.Stop();

            async Task<StopServerResponse> CheckServerStopped()
            {
                for (int tryShutDownCounter = 40; tryShutDownCounter > 0; tryShutDownCounter--)
                {
                    if (stopServerResult != StopServerResult.STOPPING)
                    {
                        SDaysTDieServersByUserIds.Remove(keyValuePair.Key);
                        return new StopServerResponse(stopServerResult);
                    }
                    await Task.Delay(1000);
                }
                return new StopServerResponse(StopServerResult.FAILED_UNKNOWN_REASON);
            }
            response = await CheckServerStopped();
            sDaysTDieServer.OnSDaysTDieServerStoppedHandler -= SDaysTDieServer_OnSDaysTDieServerStoppedHandler;

            return response;
        }
        #endregion

        #region "Telnet"
        internal async static Task ConnectTelnet(WebSocketHandler webSocketHandler)
        {

            ConnectTelnetResponse response = TryConnectTelnet(webSocketHandler);
            Command command = new Command("ConnectTelnetResponse", response);
            await webSocketHandler.Send(command);
        }
        private static ConnectTelnetResponse TryConnectTelnet(WebSocketHandler webSocketHandler)
        {
            ServerSetting serverSetting = JsonConvert.DeserializeObject<ServerSetting>(webSocketHandler.ReceivedCommand.CommandData.ToString());

            if (HasUserStartServerAndUseSettingsRights(webSocketHandler, serverSetting) == false)
            {
                return new ConnectTelnetResponse(ConnectTelnetResult.RIGHT_VIOLATION);
            }

            if (IsServerOfSettingsRunning(serverSetting))
            {
                SDaysTDieServer sDaysTDieServer = SDaysTDieServersByUserIds[serverSetting.UserUuid];
                sDaysTDieServer.ConnectWebsocketHandler(webSocketHandler);
                return new ConnectTelnetResponse(ConnectTelnetResult.OK);
            }
            else
            {
                return new ConnectTelnetResponse(ConnectTelnetResult.SERVER_NOT_RUNNING);
            }
        }

        private static bool IsServerOfSettingsRunning(ServerSetting serverSettings)
        {
            KeyValuePair<string, SDaysTDieServer> keyValuePair = SDaysTDieServersByUserIds.FirstOrDefault(x => x.Value.ServerSettings.WorldGenSeed == serverSettings.WorldGenSeed);
            return keyValuePair.Key != "" && keyValuePair.Key != null;
        }
        #endregion
        private static int GetFreePortInRange(int portFrom, int portTo)
        {
            //todo block available channels so we do not need to iterate through all applications
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
            List<int> blockedPorts = new List<int>();
            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
            {
                blockedPorts.Add(tcpi.LocalEndPoint.Port);
            }
            SDaysTDieServersByUserIds.ForEach((userId, server) =>
            {
                blockedPorts.Add(server.ServerSettings.TelnetPort);
                blockedPorts.Add(server.ServerSettings.ServerPort);
            });
            for (int port = portFrom; port <= portTo; port++)
            {
                if (!blockedPorts.Contains(port))
                    return port;
            }
            return 0;
        }

        private static bool IsServerOfSettingsRunning(ServerSettings serverSettings)
        {
            KeyValuePair<string, SDaysTDieServer> keyValuePair = SDaysTDieServersByUserIds.FirstOrDefault(x => x.Value.ServerSettings.WorldGenSeed == serverSettings.WorldGenSeed);
            return keyValuePair.Key != "" && keyValuePair.Key != null;
        }
        private static bool IsPortBlockedBySDaysTDieServer(int port)
        {
            KeyValuePair<string, SDaysTDieServer> keyValuePair = SDaysTDieServersByUserIds.FirstOrDefault(x =>
            x.Value.ServerSettings.ServerPort == port || x.Value.ServerSettings.TelnetPort == port);
            return keyValuePair.Key != "" && keyValuePair.Key != null;
        }

        private SDaysTDieServer GetServerUserInstance(string userId)
        {
            return !SDaysTDieServersByUserIds.ContainsKey(userId) ? SDaysTDieServersByUserIds[userId] : null;
        }

        internal static bool IsServerRunningBySeed(string worldGenSeed)
        {
            KeyValuePair<string, SDaysTDieServer> keyValuePair = SDaysTDieServersByUserIds.FirstOrDefault(x => x.Value.ServerSettings.WorldGenSeed == worldGenSeed);
            return keyValuePair.Key != "" && keyValuePair.Key != null;
        }
    }
}
