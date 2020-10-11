using Beste.GameServer.SDaysTDie.Connections;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static Beste.GameServer.SDaysTDie.Modules.TelnetHandler;

namespace Beste.GameServer.SDaysTDie.Modules
{
    internal class SDaysTDieServer
    {
        #region "Events"
        public delegate void OnSDaysTDieServerStopped(SDaysTDieServer sender, OnSDaysTDieServerStoppedEventArgs e);
        public event OnSDaysTDieServerStopped OnSDaysTDieServerStoppedHandler;

        public delegate void OnSDaysTDieServerStopping(SDaysTDieServer sender, OnSDaysTDieServerStoppingEventArgs e);
        public event OnSDaysTDieServerStopping OnSDaysTDieServerStoppingHandler;

        public class OnSDaysTDieServerStoppedEventArgs
        {
            public string Message { get; set; }
            public OnSDaysTDieServerStoppedEventArgs(string message)
            {
                Message = message;
            }
        }
        public class OnSDaysTDieServerStoppingEventArgs
        {
            public string Message { get; set; }
            public OnSDaysTDieServerStoppingEventArgs(string message)
            {
                Message = message;
            }
        }
        #endregion

        private static readonly char SEP = Path.DirectorySeparatorChar;
        Process Process { get; set; } = null;
        string Folder { get; set; } = null;
        public TelnetHandler TelnetHandler { get; set; }
        List<WebSocketHandler> ConnectedWebsocketHandlers { get; set; } = new List<WebSocketHandler>();
        TelnetCredentials TelnetCredentials { get; set; }
        public ServerSettings ServerSettings { get; private set; }

        public SDaysTDieServer(string folder, ServerSettings serverSettings)
        {
            Folder = folder;
            ServerSettings = serverSettings;
            TelnetCredentials = new TelnetCredentials("", serverSettings.TelnetPassword, serverSettings.TelnetPort);
            if (serverSettings.ServerConfigFilepath.EndsWith("serverconfig.xml"))
                throw new ArgumentException("serverSettings.ServerConfigFilePath.EndsWith('serverconfig.xml')! Use of Default File is not allowed!");
        }

        public void Start()
        {
            string logDateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            Process = new Process();

            // Configure the process using the StartInfo properties.
            Process.StartInfo.FileName = Folder + SEP + "7daystodie.exe";
            string logFilePath = "\"" + Folder + SEP + "7DaysToDie_Data" + SEP + "output_log_dedi" + logDateTime + ".txt \"";
            if (!File.Exists(Folder + SEP + ServerSettings.ServerConfigFilepath))
                throw new FileNotFoundException("Config file not found at "  + Folder + SEP + ServerSettings.ServerConfigFilepath);
            //Process.StartInfo.Arguments = "-logfile 7DaysToDie_Data" + SEP + "output_log_dedi" + logDateTime + ".txt -quit -batchmode -nographics -configfile=" + ServerSettings.ServerConfigFilepath + " -dedicated";
            Process.StartInfo.Arguments = "-logfile " + logFilePath + " -quit -batchmode -nographics -configfile=" + ServerSettings.ServerConfigFilepath + " -dedicated";
            Process.Start();
            TelnetHandler = new TelnetHandler(TelnetCredentials);
            TelnetHandler.OnTelnetReceivedHandler += TelnetHandler_OnTelnetReceivedHandler;
        }

        private void TelnetHandler_OnTelnetReceivedHandler(object sender, OnTelnetReceivedEventArgs e)
        {
            for(int i = 0; i < ConnectedWebsocketHandlers.Count; i++)
            {
                WebSocketHandler webSocketHandler = ConnectedWebsocketHandlers[i];
                if (webSocketHandler.WebSocket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    Command command = new Command("OnTelnetReceived", e.Message);
                    Task.Run(async () => await webSocketHandler.Send(command));
                }
                else
                {
                    ConnectedWebsocketHandlers.Remove(webSocketHandler);
                    i--;
                }
            }
        }

        public void ConnectWebsocketHandler(WebSocketHandler webSocketHandler)
        {
            ConnectedWebsocketHandlers.Add(webSocketHandler);
        }

        public void Stop()
        {
            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += ShutdownServer;
            backgroundWorker.RunWorkerAsync();
        }

        private async void ShutdownServer(object sender, DoWorkEventArgs e)
        {
            if (Process.HasExited == true)
            {
                OnSDaysTDieServerStoppedHandler?.Invoke(this, new OnSDaysTDieServerStoppedEventArgs("*** Server already down! ***"));
                TelnetHandler.BackgroundWorker.CancelAsync();
                return;
            }
            TelnetHandler.WriteLine("shutdown");
            for(int tryShutDownCounter = 30; tryShutDownCounter > 0; tryShutDownCounter--)
            {
                TelnetHandler.WriteLine("shutdown");
                OnSDaysTDieServerStoppingHandler?.Invoke(this, new OnSDaysTDieServerStoppingEventArgs("*** Check if shutdown successfull try: '" + tryShutDownCounter + "' ***"));
                if (Process.HasExited == true)
                {
                    OnSDaysTDieServerStoppedHandler?.Invoke(this, new OnSDaysTDieServerStoppedEventArgs("*** Shutdown successful ***"));
                    TelnetHandler.BackgroundWorker.CancelAsync();
                    return;
                }
                await Task.Delay(1000);
            }
            OnSDaysTDieServerStoppingHandler?.Invoke(this, new OnSDaysTDieServerStoppingEventArgs("*** Proper Shutdown not successful - Killing 7days2die process ***"));
            Process.Kill();
            TelnetHandler.BackgroundWorker.CancelAsync();
            OnSDaysTDieServerStoppedHandler?.Invoke(this, new OnSDaysTDieServerStoppedEventArgs("*** Shutdown done with killing process ***"));
        }
    }

}
