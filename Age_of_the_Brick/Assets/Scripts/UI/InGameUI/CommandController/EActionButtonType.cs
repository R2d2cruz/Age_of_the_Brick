/// <summary>
/// Defines the visual and functional categories of command buttons.
/// Used to determine appearance, behavior, and styling in the command panel UI.
/// </summary>
public enum EActionButtonType
{
    /// <summary>
    /// Primary square button used for common actions.
    /// Typically displayed using the default blue style.
    /// </summary>
    Standard,

    /// <summary>
    /// Alternative square button used for secondary actions.
    /// Typically displayed using the green style.
    /// </summary>
    Secondary,

    /// <summary>
    /// Square button used for destructive or cancellation actions.
    /// Typically displayed using the orange warning style.
    /// </summary>
    Warning,

    /// <summary>
    /// Rectangular button used for menu navigation actions,
    /// such as Next Page or Previous Page.
    /// </summary>
    Navigation
}