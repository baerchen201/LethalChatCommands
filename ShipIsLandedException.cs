namespace ChatCommandAPI;

/// <summary>
///     An exception for when a command expects the ship to be in orbit, but it isn't.
/// </summary>
public sealed class ShipIsLandedException() : CommandException("The ship must be in orbit");
