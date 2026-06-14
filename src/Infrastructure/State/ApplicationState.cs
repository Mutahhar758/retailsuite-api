namespace Retailer.Infrastructure.State;

/// <summary>
/// Static class to track application startup state
/// </summary>
public static class ApplicationState
{
    private static volatile bool _isStarted = false;

    /// <summary>
    /// Gets a value indicating whether the application has started
    /// </summary>
    public static bool IsStarted => _isStarted;

    /// <summary>
    /// Sets the application as started
    /// </summary>
    public static void SetStarted()
    {
        _isStarted = true;
    }

    /// <summary>
    /// Resets the application state (for testing purposes)
    /// </summary>
    public static void Reset()
    {
        _isStarted = false;
    }
}