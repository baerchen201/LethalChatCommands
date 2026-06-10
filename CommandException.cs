using System;

namespace ChatCommandAPI;

/// <summary>
///     An exception to signify a generic failure during the execution of a command
/// </summary>
/// <param name="message">The message to display</param>
public class CommandException(string message) : Exception(message);
