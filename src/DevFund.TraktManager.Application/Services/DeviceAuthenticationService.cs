namespace DevFund.TraktManager.Application.Services;

/// <summary>
/// Marker type retained to preserve binary compatibility after relocating
/// authentication behaviour to the infrastructure layer. New code should
/// depend on <see cref="IDeviceAuthenticationService" />.
/// </summary>
internal static class ApplicationAuthMarker
{
}
