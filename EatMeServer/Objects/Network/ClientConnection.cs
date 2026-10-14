using EatMeLibrary;
using System;
using System.Net.Sockets;

namespace EatMeServer
{
    /// <summary>
    /// A connected client: a MessageConnection plus the id the game knows it by.
    /// </summary>
    public class ClientConnection : IClientConnection
    {
        private readonly MessageConnection _Connection;

        public int Id { get; }

        public string RemoteAddress => _Connection.RemoteAddress;

        public ClientConnection(int id, TcpClient client)
        {
            Id = id;
            _Connection = new MessageConnection(client);
        }

        /// <summary>
        /// Starts the read and write tasks. onMessage and onClosed are called from background threads.
        /// </summary>
        public void Start(Action<IClientConnection, INetworkMessage> onMessage, Action<IClientConnection> onClosed)
        {
            _Connection.Start(
                (_, message) => onMessage(this, message),
                connection =>
                {
                    if (connection.CloseReason != null)
                        Console.WriteLine($"Client {Id} ({RemoteAddress}): {connection.CloseReason}");

                    onClosed(this);
                });
        }

        public void Send(byte[] frame) => _Connection.Send(frame);

        public void CloseAfterSending() => _Connection.CloseAfterSending();

        public void Close() => _Connection.Close();
    }
}
