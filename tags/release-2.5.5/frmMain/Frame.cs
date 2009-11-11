public enum Frame
{
    Checking = 1,
    UpdateInfo = 2,
    InstallUpdates = 3,
    UpdatedSuccessfully = 4,
    AlreadyUpToDate = 5,
    NoUpdatePathAvailable = 6,
    Uninstall = 7,
    Error = -1
}

public static class FrameIs
{
    /// <summary>
    /// Checks if we're on an error or a finish page.
    /// </summary>
    /// <param name="frame">The frame you want to check.</param>
    /// <returns>Returns true if we're on an error or finish page.</returns>
    public static bool ErrorFinish(Frame frame)
    {
        return frame == Frame.UpdatedSuccessfully
            || frame == Frame.AlreadyUpToDate
            || frame == Frame.NoUpdatePathAvailable
            || frame == Frame.Error;
    }

    /// <summary>
    /// Check if the frame is an interaction frame (e.g. Update info screen)
    /// </summary>
    /// <param name="frame">The frame you want to check.</param>
    /// <returns>Return true if the frame is require user interaction.</returns>
    public static bool Interaction(Frame frame)
    {
        return frame == Frame.UpdateInfo
            || frame == Frame.UpdatedSuccessfully
            || frame == Frame.AlreadyUpToDate
            || frame == Frame.NoUpdatePathAvailable
            || frame == Frame.Error;
    }
}