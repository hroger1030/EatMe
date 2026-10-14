namespace EatMeServer
{
    /// <summary>
    /// One connected client as the game sees it. Lets the game logic be tested without real sockets.
    /// </summary>
    public interface IClientConnection
    {
        /// <summary>
        /// Unique for the life of the server; also used as the player id.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Queues a complete frame from MessageSerializer.Serialize. Never blocks.
        /// </summary>
        void Send(byte[] frame);

        /// <summary>
        /// Sends whatever is already queued, then closes the connection.
        /// </summary>
        void CloseAfterSending();

        /// <summary>
        /// Closes the connection straight away, dropping anything still queued.
        /// </summary>
        void Close();
    }
}
