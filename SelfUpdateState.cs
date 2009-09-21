public enum SelfUpdateState
{
    None = 0,
    WillUpdate = 1,
    FullUpdate = 2,
    ContinuingRegularUpdate = 3,
    
    // for automatic updates only
    Downloaded = 4,
    Extracted = 5
}