using PrimS.Telnet;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;

namespace Beste.GameServer.SDaysTDie.Modules
{
    internal class TelnetHandler
    {
        public delegate void OnTelnetReceived(object sender, OnTelnetReceivedEventArgs e);
        public event OnTelnetReceived OnTelnetReceivedHandler;


        public class OnTelnetReceivedEventArgs : EventArgs
        {
            public string Message { get; }

            public OnTelnetReceivedEventArgs(string message)
            {
                Message = message;
            }
        }

        public class TelnetCredentials
        {

            public string Username { get; set; }
            public string Password { get; set; }
            public int Port { get; set; }
            public TelnetCredentials(string username, string password, int port)
            {
                Username = username;
                Password = password;
                Port = port;
            }
        }
        Task<string> ClientReadAsync { get; set; } = null;
        Client TelnetClient { get; set; } = null;
        public BackgroundWorker BackgroundWorker { get; set; } = new BackgroundWorker();


        public TelnetHandler(TelnetCredentials telnetCredentials)
        {
            BackgroundWorker.WorkerSupportsCancellation = true;
            BackgroundWorker.DoWork += BackgroundWorker_DoWork;
            BackgroundWorker.RunWorkerAsync(telnetCredentials);
        }
        private void CancelNotification()
        {
            Console.WriteLine("CancelNotification");
            // do something if needed on timeout
        }
        private async void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while(!BackgroundWorker.CancellationPending)
            {
                try
                {
                    TelnetCredentials telnetCredentials = (TelnetCredentials)e.Argument;

                    while ((TelnetClient == null || TelnetClient.IsConnected == false) &&
                        !BackgroundWorker.CancellationPending)
                    {
                        try
                        {
                            Console.WriteLine("*** Start Telnet Connection ***");
                            IByteStream byteStream = new TcpByteStream("localhost", telnetCredentials.Port);
                            TelnetClient = new Client(byteStream,new CancellationToken(), TimeSpan.FromSeconds(1));

                            //CancellationTokenSource ReceiveAsyncTokenSource = new CancellationTokenSource();
                            //ReceiveAsyncTokenSource.CancelAfter(TimeSpan.FromSeconds(1));
                            //ReceiveAsyncTokenSource.Token.Register(CancelNotification);
                            //TelnetClient = new Client("localhost", telnetCredentials.Port, ReceiveAsyncTokenSource.Token);
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("Unable to connect to the host"))
                            {
                                Console.WriteLine("*** Unable to connect to the host - retry ***");
                            }
                            else
                            {
                                Console.WriteLine("*** Unknown error on connection - closing ***");
                                return;
                            }
                        }
                        if (BackgroundWorker.CancellationPending == true)
                            return;
                    }


                    while (TelnetClient.IsConnected == true && !BackgroundWorker.CancellationPending)
                    {
                        await ReadAndSendEvent();

                        if (!ClientReadAsync.Result.Contains("Connected with 7DTD server"))
                        {
                            Console.WriteLine("*** Sending Password: '" + telnetCredentials.Password + "'***");
                            await TelnetClient.WriteLine(telnetCredentials.Password);
                            await Task.Delay(1000);
                            await ReadAndSendEvent();

                            if (ClientReadAsync.Result.Contains("Password incorrect"))
                            {
                                Console.WriteLine("*** Password incorrect detected - closing connection ***");
                                return;
                            }
                        }
                        int counterUntilPing = 3;
                        while (TelnetClient.IsConnected == true && !BackgroundWorker.CancellationPending)
                        {
                            counterUntilPing--;
                            if(counterUntilPing <= 0)
                            {
                                counterUntilPing = 30;
                                await AsyncPingTelnetServer();
                            }

                            await ReadAndSendEvent();
                            await Task.Delay(2000);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine("*** Exception occured while connected - wait 10 seconds before going on ***");
                    TelnetClient = null;
                    await Task.Delay(10000);
                }
            }
        }

        private async Task AsyncPingTelnetServer()
        {
            await TelnetClient.WriteLine("ping");
            await Task.Delay(2000);
            await ReadAndSendEvent(true);
        }

        private async Task ReadAndSendEvent(bool FilterUnknownCommandOnPing = false)
        {
            ClientReadAsync = TelnetClient.ReadAsync();
            await Task.WhenAll(ClientReadAsync);
            if (ClientReadAsync.Result != "")
            {
                if(FilterUnknownCommandOnPing)
                {
                    string unknownCommandFilteredMessage = ClientReadAsync.Result.Replace("*** ERROR: unknown command 'ping'", "");
                    OnTelnetReceivedHandler?.Invoke(this, new OnTelnetReceivedEventArgs(unknownCommandFilteredMessage));
                }
                else
                {
                    OnTelnetReceivedHandler?.Invoke(this, new OnTelnetReceivedEventArgs(ClientReadAsync.Result));
                }
            }
            //Console.Write(ClientReadAsync.Result);
        }

        public void WriteLine(string line)
        {
            try
            {
                if (TelnetClient == null)
                {
                    Console.WriteLine("*** telnetClient not initialized ***");
                    return;
                }
                else if(TelnetClient.IsConnected == false)
                {
                    Console.WriteLine("*** telnetClient not connected ***");
                    return;
                }
                TelnetClient.WriteLine(line);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("*** Exception occured in WriteLine - wait 10 seconds before going on ***");
            }
        }


    }

}
