namespace TekkenFrameData.Library.Exceptions;

public class UserNotFoundException : Exception
{
    public string UserId { get; }

    public UserNotFoundException(string userId)
        : base($"User with ID '{userId}' not found.")
    {
        UserId = userId;
    }

    public UserNotFoundException(string userId, string message)
        : base(message)
    {
        UserId = userId;
    }
}

public class RoleNotFoundException : Exception
{
    public string RoleName { get; }

    public RoleNotFoundException(string roleName)
        : base($"Role '{roleName}' not found.")
    {
        RoleName = roleName;
    }

    public RoleNotFoundException(string roleName, string message)
        : base(message)
    {
        RoleName = roleName;
    }
}

public class PermissionDeniedException : Exception
{
    public string UserId { get; }
    public string Permission { get; }

    public PermissionDeniedException(string userId, string permission)
        : base($"User '{userId}' does not have permission '{permission}'.")
    {
        UserId = userId;
        Permission = permission;
    }

    public PermissionDeniedException(string userId, string permission, string message)
        : base(message)
    {
        UserId = userId;
        Permission = permission;
    }
}

public class InvalidRoleException : Exception
{
    public string RoleName { get; }

    public InvalidRoleException(string roleName)
        : base($"Role '{roleName}' is invalid or not allowed.")
    {
        RoleName = roleName;
    }

    public InvalidRoleException(string roleName, string message)
        : base(message)
    {
        RoleName = roleName;
    }
}

public class SystemRoleModificationException : Exception
{
    public string RoleName { get; }

    public SystemRoleModificationException(string roleName)
        : base($"Cannot modify system role '{roleName}'.")
    {
        RoleName = roleName;
    }

    public SystemRoleModificationException(string roleName, string message)
        : base(message)
    {
        RoleName = roleName;
    }
}

public class UserAlreadyInRoleException(string userId, string roleName)
    : Exception($"User '{userId}' is already in role '{roleName}'.")
{
    public string UserId { get; } = userId;
    public string RoleName { get; } = roleName;
}

public class UserNotInRoleException(string userId, string roleName)
    : Exception($"User '{userId}' is not in role '{roleName}'.")
{
    public string UserId { get; } = userId;
    public string RoleName { get; } = roleName;
}
