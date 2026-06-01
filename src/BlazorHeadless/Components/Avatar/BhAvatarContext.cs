namespace BlazorHeadless;

/// <summary>
/// The loading status of an <see cref="BhAvatarImage"/>, mirroring Radix UI's
/// <c>ImageLoadingStatus</c>. Drives whether the image or the
/// <see cref="BhAvatarFallback"/> is shown.
/// </summary>
public enum BhAvatarImageLoadingStatus
{
    /// <summary>No image source has been resolved yet (initial / server-render state).</summary>
    Idle,

    /// <summary>The image is downloading.</summary>
    Loading,

    /// <summary>The image finished downloading successfully and is displayed.</summary>
    Loaded,

    /// <summary>The image failed to load (or no source was provided).</summary>
    Error,
}

/// <summary>
/// Cascading context shared by <see cref="BhAvatar"/> with its
/// <see cref="BhAvatarImage"/> and <see cref="BhAvatarFallback"/> children.
/// Carries the current image loading status and the setter the image uses to
/// report transitions, so the fallback can coordinate without prop-drilling.
/// </summary>
public sealed class BhAvatarContext
{
    /// <summary>The current image loading status.</summary>
    public required BhAvatarImageLoadingStatus Status { get; init; }

    /// <summary>
    /// Called by <see cref="BhAvatarImage"/> to report a loading-status change.
    /// Updates the root's state and re-renders the avatar subtree.
    /// </summary>
    public required Action<BhAvatarImageLoadingStatus> SetStatus { get; init; }
}
