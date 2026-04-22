namespace Alerto.Application.Common.Exceptions;

public sealed class ExternalDependencyException : Exception
{
    public ExternalDependencyException(string dependencyName, string message, Exception? innerException = null)
        : base($"Fallo la dependencia externa '{dependencyName}': {message}", innerException)
    {
        DependencyName = dependencyName;
    }

    public string DependencyName { get; }
}
