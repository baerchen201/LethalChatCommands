namespace ChatCommandAPI;

public abstract class BaseCommand
{
    /// <summary>
    ///     The command's display name
    /// </summary>
    public virtual string Name => GetType().Name;

    /// <summary>
    ///     A short description of the command
    /// </summary>
    public virtual string Description => string.Empty;

    /// <summary>
    ///     The primary command to use by default
    /// </summary>
    public virtual string Command => Name.ToLowerInvariant();

    /// <summary>
    ///     Alternative commands (can also become primary in case of duplicates)
    /// </summary>
    public virtual string[] Aliases => [GetType().Name.ToLowerInvariant()];

    /// <summary>
    ///     An array of syntax strings
    /// </summary>
    /// <remarks>This is not validated or used in any way by the API, only displayed to the user</remarks>
    public virtual string[] Syntax => [];

    /// <summary>
    ///     Hides the command from listings
    /// </summary>
    public virtual bool Hidden => false;

    /// <summary>
    ///     The command's full name, used to resolve conflicts when multiple commands have the same display name / aliases
    /// </summary>
    /// <seealso cref="System.Type.FullName" />
    public string FullName => GetType().FullName!;

    public sealed override string ToString()
    {
        return $"{GetType().AssemblyQualifiedName}";
    }
}
