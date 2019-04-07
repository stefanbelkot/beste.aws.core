using Beste.Aws.Module;
using Beste.Core.Models;
using Beste.Databases.User;
using Beste.GameServer.SDaysTDie.Connections;
using Beste.Module;
using Beste.Rights;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Beste.GameServer.SDaysTDie.Modules
{
    class UserManager
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum HasRightsResult
        {
            RIGHT_VIOLATION,
            EXCEPTION,
            SUCCESS
        }
        public class Right
        {
            public string Action { get; set; }
            public string Ressource { get; set; }
            public int? RessourceId { get; set; }
            public bool HasRight { get; set; }
            public Right(string action, string ressource, int? ressourceId)
            {
                Action = action;
                Ressource = ressource;
                RessourceId = ressourceId;
            }
        }
        public class HasRightsResponse : IResponse<HasRightsResult>
        {
            public HasRightsResult Result { get; private set; }
            public List<Right> Rights { get; private set; }

            public HasRightsResponse(HasRightsResult result, List<Right> rights)
            {
                Result = result;
                Rights = rights;
            }
        }
        static Beste.Aws.Module.BesteUser BesteUser { get; set; } = new Beste.Aws.Module.BesteUser();
        static RightControl RightControl { get; set; } = new RightControl("Beste.GameServer.SDaysTDie.User");

        internal async static Task HandleLogin(WebSocketHandler webSocketHandler)
        {
            User user = JsonConvert.DeserializeObject<User>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            BesteUserAuthentificationResponse response = await BesteUser.Authenticate(webSocketHandler.ReceivedCommand.CommandData.ToString());
            if (response.Result == BesteUserAuthentificationResult.SUCCESS ||
                response.Result == BesteUserAuthentificationResult.MUST_CHANGE_PASSWORT)
            {
                webSocketHandler.User = response.UserData;
                List<PureRight> pureRights = new List<PureRight>();
                pureRights.Add(new PureRight
                {
                    Authorized = true,
                    Operation = "ChangePassword_" + webSocketHandler.User.Username,
                    RecourceModule = "User"
                });
                pureRights.Add(new PureRight
                {
                    Authorized = true,
                    Operation = "EditUser_" + webSocketHandler.User.Username,
                    RecourceModule = "User"
                });
                pureRights.Add(new PureRight
                {
                    Authorized = true,
                    Operation = "DeleteUser_" + webSocketHandler.User.Username,
                    RecourceModule = "User"
                });
                pureRights.Add(new PureRight
                {
                    Authorized = true,
                    Operation = "GetUser_" + webSocketHandler.User.Username,
                    RecourceModule = "User"
                });
                webSocketHandler.ConnectedUserToken = await RightControl.Register(webSocketHandler.User.Uuid, pureRights);
            }
            Command resonseCommand = new Command("AuthentificationResponse", response);
            await webSocketHandler.Send(resonseCommand);
        }

        internal async static Task CreateUser(WebSocketHandler webSocketHandler)
        {
            Command resonseCommand = await ModifyUser(async () => {
                return await BesteUser.CreateUser(webSocketHandler.ReceivedCommand.CommandData.ToString());
                },
                "CreateUser",
                webSocketHandler);
            await webSocketHandler.Send(resonseCommand);
        }

        internal async static Task ChangePassword(WebSocketHandler webSocketHandler)
        {
            Command resonseCommand = await ModifyUser(async () => {
                return await BesteUser.ChangePasswordByUser(webSocketHandler.ReceivedCommand.CommandData.ToString());
                },
                "ChangePassword",
                webSocketHandler);
            await webSocketHandler.Send(resonseCommand);
        }

        internal async static Task DeleteUser(WebSocketHandler webSocketHandler)
        {
            Command resonseCommand = await ModifyUser(async () =>
                {
                    return await BesteUser.DeleteUser(webSocketHandler.ReceivedCommand.CommandData.ToString());
                },
                "DeleteUser",
                webSocketHandler);
            await webSocketHandler.Send(resonseCommand);
        }

        internal async static Task EditUser(WebSocketHandler webSocketHandler)
        {
            Command resonseCommand = await ModifyUser(async () =>
                {
                    return await BesteUser.EditUser(webSocketHandler.ReceivedCommand.CommandData.ToString());
                },
                "EditUser",
                webSocketHandler);
            await webSocketHandler.Send(resonseCommand);
        }

        private static async Task<Command> ModifyUser(Func<Task<ModifyUserResponse>> modifyAction, string actionName, WebSocketHandler webSocketHandler)
        {
            User user = JsonConvert.DeserializeObject<User>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            ModifyUserResponse response = null;
            if (RightControl.IsGranted(webSocketHandler.ConnectedUserToken, actionName, "User"))
            {
                response = await modifyAction();
            }
            else if(RightControl.IsGranted(webSocketHandler.ConnectedUserToken, actionName + "_" + user.Username, "User"))
            {
                response = await modifyAction();
            }
            else
            {
                response = new ModifyUserResponse(ModifyUserResult.RIGHT_VIOLATION, null, null, null);
            }
            return new Command(actionName + "Response", response);
        }

        private static async Task<Command> GetUsers(Func<Task<GetUsersResponse>> getAction, string actionName, WebSocketHandler webSocketHandler)
        {
            User user = JsonConvert.DeserializeObject<User>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            GetUsersResponse response = null;
            if (RightControl.IsGranted(webSocketHandler.ConnectedUserToken, actionName, "User"))
            {
                response = await getAction();
            }
            else if (RightControl.IsGranted(webSocketHandler.ConnectedUserToken, actionName + "_" + user.Username, "User"))
            {
                response = await getAction();
            }
            else
            {
                response = new GetUsersResponse(GetUsersResult.RIGHT_VIOLATION, null);
            }
            return new Command(actionName + "Response", response);
        }

        internal async static Task GetUsers(WebSocketHandler webSocketHandler)
        {
            Command resonseCommand = await GetUsers(async () =>
            {
                return await BesteUser.GetUsers(webSocketHandler.ReceivedCommand.CommandData.ToString());
            },
                "GetUsers",
                webSocketHandler);
            await webSocketHandler.Send(resonseCommand);
        }
        internal async static Task GetUser(WebSocketHandler webSocketHandler)
        {
            Command resonseCommand = await GetUsers(async () =>
            {
                return await BesteUser.GetUser(webSocketHandler.ReceivedCommand.CommandData.ToString());
            },
                "GetUser",
                webSocketHandler);
            await webSocketHandler.Send(resonseCommand);
        }
        internal async static Task LoggedInUserHasRights(WebSocketHandler webSocketHandler)
        {
            List<Right> requestedRights = JsonConvert.DeserializeObject<List<Right>>(webSocketHandler.ReceivedCommand.CommandData.ToString());
            HasRightsResponse response = null;
            requestedRights.ForEach((item) =>
            {
                item.HasRight = RightControl.IsGranted(webSocketHandler.ConnectedUserToken, item.Action, item.Ressource, item.RessourceId);
            });
            response = new HasRightsResponse(HasRightsResult.SUCCESS, requestedRights);
            Command resonseCommand = new Command("LoggedInUserHasRightsResponse", response);
            await webSocketHandler.Send(resonseCommand);
        }
    }
}
