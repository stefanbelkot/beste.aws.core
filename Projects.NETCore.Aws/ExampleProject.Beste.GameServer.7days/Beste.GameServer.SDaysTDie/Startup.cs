using Beste.GameServer.SDaysTDie.Connections;
using Beste.GameServer.SDaysTDie.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Beste.GameServer.SDaysTDie
{
    internal class Startup
    {
        private static readonly char SEP = Path.DirectorySeparatorChar;
        public void ConfigureServices(IServiceCollection services)
        {
        }
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
            


            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(20),
                ReceiveBufferSize = 4 * 1024
            };
            app.UseWebSockets(webSocketOptions);

            app.Use(async (context, next) =>
            {
                try
                {


                    Console.WriteLine("Received request to Path: " + context.Request.Path);
                    if (context.Request.Path == "/ws")
                    {
                        if (context.WebSockets.IsWebSocketRequest)
                        {
                            Console.WriteLine("context.WebSockets.IsWebSocketRequest");
                            try
                            {
                                Console.WriteLine("context.WebSockets.AcceptWebSocketAsync()");
                                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                                Console.WriteLine("finished context.WebSockets.AcceptWebSocketAsync()");

                                Console.WriteLine("OpenWebsocketConnection(context, webSocket);");
                                await OpenWebsocketConnection(context, webSocket);
                                Console.WriteLine("finished OpenWebsocketConnection(context, webSocket);");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        }
                    }
                    else if (context.Request.Path.Value.StartsWith("/.well-known/acme-challenge/"))
                    {
                        FileInfo file = new FileInfo(Directory.GetCurrentDirectory() + SEP + "www" + SEP + context.Request.Path.Value);
                        if (file.Exists)
                        {
                            context.Response.Headers["Content-Length"] = file.Length.ToString();
                            context.Response.ContentType = "text/json";


                            await context.Response.SendFileAsync(Directory.GetCurrentDirectory() + SEP + "www" + SEP + context.Request.Path.Value);
                        }
                        else
                        {
                            file = new FileInfo(Directory.GetCurrentDirectory() + SEP + "www" + SEP + "filenotexists" + SEP + "filenotexists");
                            context.Response.Headers["Content-Length"] = file.Length.ToString();
                            context.Response.ContentType = "text/json";
                            await context.Response.SendFileAsync(Directory.GetCurrentDirectory() + SEP + "www" + SEP + "filenotexists" + SEP + "filenotexists");
                        }
                    }
                    else if (context.Request.Path.Value.StartsWith("/files"))
                    {
                        FileInfo file = new FileInfo(Directory.GetCurrentDirectory() + SEP + "www" + SEP + context.Request.Path.Value);
                        if (file.Exists)
                        {
                            context.Response.Headers["Content-Disposition"] = "inline;filename=\"" + file.Name + "\"";

                            context.Response.Headers["Content-Length"] = file.Length.ToString();
                            context.Response.ContentType = "image/jpeg";
                            await context.Response.SendFileAsync(Directory.GetCurrentDirectory() + SEP + "www" + context.Request.Path.Value);

                            //string imgAsString = Convert.ToBase64String(File.ReadAllBytes(file.FullName));
                            //context.Response.Headers["Content-Length"] = System.Text.ASCIIEncoding.Unicode.GetByteCount(imgAsString).ToString();
                            //await context.Response.WriteAsync(imgAsString);
                            //ctx.Response.OutputStream.Write(myImage); 
                            Console.WriteLine("[INFO] Picture sent!");


                        }
                        else
                        {
                            string ipOfInvalidFileAccess = context.Connection.RemoteIpAddress.ToString();
                            file = new FileInfo(Directory.GetCurrentDirectory() + SEP + "www" + SEP + "filenotexists" + SEP + "filenotexists");
                            context.Response.Headers["Content-Length"] = file.Length.ToString();
                            context.Response.ContentType = "text/json";
                            await context.Response.SendFileAsync(Directory.GetCurrentDirectory() + SEP + "www" + SEP + "filenotexists" + SEP + "filenotexists");
                        }
                    }
                    else
                    {
                        FileInfo file = new FileInfo(Directory.GetCurrentDirectory() + ".." + SEP + context.Request.Path.Value);

                        if (file.Exists)
                        {
                            var fileContents = File.ReadAllText(Directory.GetCurrentDirectory() + ".." + SEP + context.Request.Path.Value);
                            context.Response.Headers["Content-Length"] = file.Length.ToString();
                            context.Response.ContentType = "text/html";
                            //response.Content = new StringContent(fileContents);
                            //response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                            //return response;
                        }
                        else
                        {
                            await next();
                        }

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });
        }

        private async Task OpenWebsocketConnection(HttpContext context, WebSocket webSocket)
        {
            Console.WriteLine(DateTime.Now + " | Connected: " + context.Connection.RemoteIpAddress + ":" + context.Connection.RemotePort + " connected");
            var buffer = new byte[1024 * 4];
            WebSocketHandler webSocketHandler = new WebSocketHandler(webSocket);
            await webSocketHandler.Send(new Command("Connected", null));
            try
            {
                await webSocketHandler.ExtractCompleteMessage(buffer);

                if (webSocketHandler.ReceivedCommand.CommandName == "Login")
                {
                    await UserManager.HandleLogin(webSocketHandler);
                }
            }
            catch (OperationCanceledException ex)
            {
                await webSocketHandler.Send(new Command("LoginTimeout", null));
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.ToString());
            }

            try
            {
                if (webSocketHandler.ConnectedUserToken != "")
                {
                    await ServerSettingsHandler.RegisterUser(webSocketHandler.User, webSocketHandler.ConnectedUserToken);
                    await SDaysTDieServerHandler.RegisterUser(webSocketHandler.User, webSocketHandler.ConnectedUserToken);
                    while (!webSocketHandler.WebSocket.CloseStatus.HasValue)
                    {
                        await webSocketHandler.ExtractCompleteMessage(buffer);
                        if (webSocketHandler.ReceivedCommand?.CommandName == "GetUser")
                        {
                            await UserManager.GetUser(webSocketHandler);
                        }
                        else if (webSocketHandler.ReceivedCommand?.CommandName == "GetUsers")
                        {
                            await UserManager.GetUsers(webSocketHandler);
                        }
                        else if(webSocketHandler.ReceivedCommand?.CommandName == "ChangePassword")
                        {
                            await UserManager.ChangePassword(webSocketHandler);
                        }
                        else if (webSocketHandler.ReceivedCommand?.CommandName == "CreateUser")
                        {
                            await UserManager.CreateUser(webSocketHandler);
                        }
                        else if (webSocketHandler.ReceivedCommand?.CommandName == "EditUser")
                        {
                            await UserManager.EditUser(webSocketHandler);
                        }
                        else if (webSocketHandler.ReceivedCommand?.CommandName == "DeleteUser")
                        {
                            await UserManager.DeleteUser(webSocketHandler);
                        }
                        else if (webSocketHandler.ReceivedCommand?.CommandName == "LoggedInUserHasRights")
                        {
                            await UserManager.LoggedInUserHasRights(webSocketHandler);
                        }
                        else if (webSocketHandler.ReceivedCommand?.CommandName == "AddServerSetting")
                        {
                            await ServerSettingsHandler.AddServerSettings(webSocketHandler);
                        }
                        else if (webSocketHandler.ReceivedCommand?.CommandName == "EditServerSettings")
                        {
                            await ServerSettingsHandler.EditServerSettings(webSocketHandler);
                        }
                        else if (webSocketHandler.ReceivedCommand?.CommandName == "DeleteServerSettings")
                        {
                            await ServerSettingsHandler.DeleteServerSettings(webSocketHandler);
                        }
                        else if (webSocketHandler.ReceivedCommand?.CommandName == "GetServerSettingsOfLoggedInUser")
                        {
                            await ServerSettingsHandler.GetServerSettingsOfLoggedInUser(webSocketHandler);
                        }
                        else if (webSocketHandler.ReceivedCommand?.CommandName == "StartServer")
                        {
                            await SDaysTDieServerHandler.StartServer(webSocketHandler);
                        }
                        else if (webSocketHandler.ReceivedCommand?.CommandName == "StopServer")
                        {
                            await SDaysTDieServerHandler.StopServer(webSocketHandler);
                        }
                        else if (webSocketHandler.ReceivedCommand?.CommandName == "ConnectTelnet")
                        {
                            await SDaysTDieServerHandler.ConnectTelnet(webSocketHandler);
                        }
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                await webSocketHandler.Send(new Command("ConnectionTimeout", null));
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            try
            {
                await webSocketHandler.Send(new Command("Disconnected", null));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not send Disconnected message, Exception: " + ex.Message);
            }
            Console.WriteLine(DateTime.Now + " | " + context.Connection.RemoteIpAddress + ":" + context.Connection.RemotePort + " disconnected");
            await webSocket.CloseAsync(webSocketHandler.Result.CloseStatus.Value, webSocketHandler.Result.CloseStatusDescription, CancellationToken.None);
        }
    }
}