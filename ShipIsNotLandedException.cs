namespace ChatCommandAPI;

/// <summary>
///     An exception for when a command expects the ship to be on a moon, but it isn't.
/// </summary>
public sealed class ShipIsNotLandedException() : CommandException("The ship must be on a moon");
