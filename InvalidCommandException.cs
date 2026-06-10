using System;

namespace ChatCommandAPI;

internal class InvalidCommandException(
    string propertyName,
    string propertyValue,
    BaseCommand command
) : Exception($"Error registering {command.FullName}: Invalid {propertyName} '{propertyValue}'");
