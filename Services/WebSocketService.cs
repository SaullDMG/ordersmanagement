using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace OrdersManagement.Services
{
    public class WebSocketService
    {
        private static readonly List<WebSocket> _clients = new List<WebSocket>();
        private static readonly object _lock = new object(); // 🔥 Candado para proteger la lista multihilo

        public async Task HandleWebSocketAsync(HttpContext context, WebSocket webSocket)
        {
            // 🔐 Bloqueamos de forma segura para añadir al cliente
            lock (_lock)
            {
                _clients.Add(webSocket);
            }

            var buffer = new byte[1024 * 4];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        lock (_lock) { _clients.Remove(webSocket); }
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                    }
                }
            }
            catch
            {
                lock (_lock) { _clients.Remove(webSocket); }
            }
        }

        public async Task SendAlertToClientsAsync(string message)
        {
            var alertMessage = JsonSerializer.Serialize(new { type = "alert", message = message });
            var bytes = Encoding.UTF8.GetBytes(alertMessage);

            // 🔐 Clonamos la lista rápido bajo el candado para poder iterar sin peligro
            List<WebSocket> openClients;
            lock (_lock)
            {
                openClients = _clients.Where(c => c.State == WebSocketState.Open).ToList();
            }

            foreach (var client in openClients)
            {
                try
                {
                    await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch
                {
                    lock (_lock) { _clients.Remove(client); }
                }
            }
        }
    }
}