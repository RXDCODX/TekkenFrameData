using System.ComponentModel.DataAnnotations;

namespace TekkenFrameData.Library.Models.Identity;

public class UserManagementInfo : UserInfo
{
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    public List<string> Permissions { get; set; } = [];
    public List<RoleInfo> RoleDetails { get; set; } = [];
}

public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = [];
}

public class UpdateUserRequest
{
    [StringLength(50)]
    public string? FirstName { get; set; }

    [StringLength(50)]
    public string? LastName { get; set; }

    public bool? IsActive { get; set; }
}

public class UserRoleRequest
{
    [Required]
    public string Role { get; set; } = string.Empty;
}

public class BulkUserRoleRequest
{
    [Required]
    public List<string> UserIds { get; set; } = [];

    [Required]
    public string Role { get; set; } = string.Empty;
}

public class UserSearchRequest
{
    public string? SearchTerm { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class UserSearchResult
{
    public List<UserManagementInfo> Users { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class UserStatistics
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public Dictionary<string, int> UsersByRole { get; set; } = [];
    public int NewUsersThisMonth { get; set; }
    public int ActiveUsersThisMonth { get; set; }
}
