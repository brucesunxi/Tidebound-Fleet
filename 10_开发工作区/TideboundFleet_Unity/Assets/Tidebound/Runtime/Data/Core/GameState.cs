namespace Tidebound.Core
{
    // Failed is reserved; a blocked board does not automatically fail the session.
    public enum GameState { Prepare = 0, Playing = 1, Paused = 2, Victory = 3, Failed = 4 }
}
