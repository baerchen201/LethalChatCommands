namespace ChatCommandAPI;

/// <summary>
///     An exception to signify an error parsing a commands arguments
/// </summary>
public sealed class InvalidArgumentsException() : CommandException("Invalid arguments");
