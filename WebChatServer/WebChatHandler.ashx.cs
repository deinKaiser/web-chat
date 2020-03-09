using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;

namespace WebChat
{
    /// <summary>
    /// Сводное описание для WebChatHandler
    /// Handling WebChat requests
    /// </summary>
    public class WebChatHandler : IHttpHandler
    {
        public static MySqlConnection connection = DBUtils.GetDBConnection();
        private static readonly List<WebSocket> Clients = new List<WebSocket>();
        public void ProcessRequest(HttpContext context)
        {
            if (context.IsWebSocketRequest)
                context.AcceptWebSocketRequest(ClientRequest);
        }
        public static List<string> GetAllMessages()
        {
            connection.Open();
            MySqlCommand cmd = connection.CreateCommand();
            string sql = "SELECT * from message";
            cmd.CommandText = sql;
            MySqlDataReader rdr = cmd.ExecuteReader();
            List<string> messages = new List<string>();
            while (rdr.Read())
            {
                messages.Add(rdr.GetValue(1).ToString());
            }
            rdr.Close();
            connection.Close();
            return messages;
        }
        private async Task ClientRequest(AspNetWebSocketContext context)
        {
            var socket = context.WebSocket;
            try
            {
                Clients.Add(socket);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            var allMessages = GetAllMessages();
            foreach(string s in allMessages)
            {
                var buffer = new ArraySegment<byte>(Encoding.ASCII.GetBytes(s));
                await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            
            while (true)
            {
                var buffer = new ArraySegment<byte>(new byte[1024]);
                var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                byte[] arr = buffer.Array;
                var message = System.Text.Encoding.ASCII.GetString(arr, 0, result.Count);
                connection.Open();
                string sql = "INSERT into message (Message_Text) values (@messageText)";
                MySqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.Add("@messageText", MySqlDbType.VarChar).Value = message.ToString();
                var res = cmd.ExecuteNonQuery();
                connection.Close();
                for (int i = 0; i < Clients.Count; i++)
                {
                    WebSocket clientSocket = Clients[i];
                    try
                    {
                        if (clientSocket.State == WebSocketState.Open)
                        {
                            await clientSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }

                    catch (ObjectDisposedException)
                    {
                        try
                        {
                            Clients.Remove(clientSocket);
                            i--;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }

            }
        }
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}