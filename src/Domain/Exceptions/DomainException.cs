namespace Domain.Exceptions
{
    public abstract class DomainException(string code, string message) : Exception(message)
    {
        public string Code { get; } = code;
    }

    /// <summary>The requested entity doesn't exist. Maps to 404.</summary>
    public sealed class NotFoundException(string code, string message) : DomainException(code, message);

    /// <summary>
    /// An input or state doesn't satisfy a domain invariant (a required field
    /// is missing, a value is out of range, a date policy isn't met). Maps to
    /// 400. Distinct from FluentValidation's own ValidationException by design
    /// — FluentValidation checks request *shape* at the API boundary before a
    /// command is even dispatched; this is the domain's own, independent
    /// enforcement of its rules, reachable regardless of caller.
    /// </summary>
    public sealed class DomainValidationException(string code, string message) : DomainException(code, message);

    /// <summary>
    /// The request is individually well-formed but conflicts with the current
    /// state of the system (not enough tickets remain, an event with sold
    /// tickets can't be deleted). Maps to 409.
    /// </summary>
    public sealed class ConflictException(string code, string message) : DomainException(code, message);
}
