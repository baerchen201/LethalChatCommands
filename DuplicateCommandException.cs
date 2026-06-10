using System;

namespace ChatCommandAPI;

internal sealed class DuplicateCommandException(BaseCommand command)
    : Exception(
        $"Duplicate command registry: {command.FullName} in {command.GetType().Assembly.FullName}"
    );
