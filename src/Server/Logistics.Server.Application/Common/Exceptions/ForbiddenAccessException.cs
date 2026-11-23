namespace Logistics.Server.Application.Common.Exceptions;

/// <summary>
/// Thrown when an authenticated user does not have permission or branch scope to perform an action.
/// </summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException()
        : base("Access to this resource or operation is forbidden.")
    {
    }

    public ForbiddenAccessException(string message)
        : base(message)
    {
    }
}
