namespace Logistics.Server.Domain.Exceptions;

/// <summary>
/// Thrown when an invalid status or state transition is attempted on an entity.
/// </summary>
public class InvalidStateTransitionException : DomainException
{
    public string CurrentState { get; }
    public string TargetState { get; }

    public InvalidStateTransitionException(string entityName, string currentState, string targetState)
        : base($"Cannot transition '{entityName}' from state '{currentState}' to state '{targetState}'.", "INVALID_STATE_TRANSITION")
    {
        CurrentState = currentState;
        TargetState = targetState;
    }
}
