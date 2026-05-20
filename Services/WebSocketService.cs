using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace OrdersManagement.Services
{
    public class WebSocketService
    {
        private static List<WebSocket> _clients = new List<WebSocket>();

        public async Task HandleWebSocketAsync(HttpContext context, WebSocket webSocket)
        {
            _clients.Add(webSocket);
            var buffer = new byte[1024 * 4];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _clients.Remove(webSocket);
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                    }
                }
            }
            catch
            {
                _clients.Remove(webSocket);
            }
        }

        public async Task SendAlertToClientsAsync(string message)
        {
            var alertMessage = JsonSerializer.Serialize(new { type = "alert", message = message });
            var bytes = Encoding.UTF8.GetBytes(alertMessage);

            foreach (var client in _clients.Where(c => c.State == WebSocketState.Open).ToList())
            {
                try
                {
                    await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch
                {
                    _clients.Remove(client);
                }
            }
        }
    }
}