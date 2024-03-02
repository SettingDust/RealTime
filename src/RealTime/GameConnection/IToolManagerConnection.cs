// IToolManagerConnection.cs

namespace RealTime.GameConnection
{
    /// <summary>
    /// An interface for the game logic related to the tool management.
    /// </summary>
    internal interface IToolManagerConnection
    {
        /// <summary>Gets the current game mode.</summary>
        /// <returns>The current game mode.</returns>
        ItemClass.Availability GetCurrentMode();
    }
}
