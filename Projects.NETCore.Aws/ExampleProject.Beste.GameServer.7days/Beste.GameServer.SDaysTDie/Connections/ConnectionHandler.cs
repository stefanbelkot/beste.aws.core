using Beste.Databases.User;
using Beste.GameServer.SDaysTDie.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beste.GameServer.SDaysTDie.Connections
{
    public class Command
    {
        public string CommandName { get; set; }
        public object CommandData { get; set; }
        public Command(string CommandName, object CommandData)
        {
            this.CommandName = CommandName;
            this.CommandData = CommandData;
        }
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            //return JsonConvert.SerializeObject(this);
        }
        public byte[] ToByteArray()
        {
            return System.Text.UTF8Encoding.UTF8.GetBytes(ToJson());
        }
    }
 
    public class WebSocketHandler
    {
        public WebSocketReceiveResult Result { get; set; }
        public WebSocket WebSocket { get; set; }
        public User User { get; set; }
        public string ConnectedUserToken { get; set; } = "";
        public Command ReceivedCommand { get; set; }
        public Command ResponseCommand { get; set; }
        public CancellationTokenSource ReceiveAsyncTokenSource { get; set; }
        private byte[] ResponseBytes { get; set; }

        public WebSocketHandler(WebSocket webSocket)
        {
            this.WebSocket = webSocket;
        }

        private void CancelNotification()
        {
            // do something if needed on timeout
        }
        public async Task ExtractCompleteMessage(byte[] buffer, int timeOut = 900)
        {
            try
            {
                ReceiveAsyncTokenSource = new CancellationTokenSource();
                ReceiveAsyncTokenSource.Token.Register(CancelNotification);
                ReceiveAsyncTokenSource.CancelAfter(TimeSpan.FromSeconds(timeOut));
                Result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ReceiveAsyncTokenSource.Token).WithCancellation(ReceiveAsyncTokenSource.Token);
            }
            catch (OperationCanceledException ex)
            {
                throw ex;
            }

            List<byte> messageList = new List<byte>(buffer.SubArray(0, Result.Count));
            while (Result.EndOfMessage == false)
            {
                Result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                messageList.AddRange(buffer.SubArray(0, Result.Count));
            }
            string message = System.Text.Encoding.UTF8.GetString(messageList.ToArray());
            ReceivedCommand = JsonConvert.DeserializeObject<Command>(message);
        }
        
        public async Task Send(Command CommandToSend)
        {
            this.ResponseCommand = CommandToSend;
            this.ResponseBytes = CommandToSend.ToByteArray();
            await WebSocket.SendAsync(new ArraySegment<byte>(ResponseBytes, 0, ResponseBytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

    }

}
