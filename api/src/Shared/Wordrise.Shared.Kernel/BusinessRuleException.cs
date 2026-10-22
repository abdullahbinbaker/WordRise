namespace Wordrise.Shared.Kernel;

/// <summary>
/// Thrown by a domain object when an operation would break one of its rules.
/// The host turns it into a 400 response carrying <see cref="Code"/>.
/// </summary>
public sealed class BusinessRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
