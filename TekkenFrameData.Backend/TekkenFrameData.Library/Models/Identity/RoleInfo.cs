namespace TekkenFrameData.Library.Models.Identity;

public class RoleInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = [];
    public int Priority { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class RolePermissions
{
    // User Management
    public const string ViewUsers = "users.view";
    public const string CreateUsers = "users.create";
    public const string EditUsers = "users.edit";
    public const string DeleteUsers = "users.delete";
    public const string ManageUserRoles = "users.manage_roles";
    public const string ActivateUsers = "users.activate";
    public const string DeactivateUsers = "users.deactivate";

    // Role Management
    public const string ViewRoles = "roles.view";
    public const string CreateRoles = "roles.create";
    public const string EditRoles = "roles.edit";
    public const string DeleteRoles = "roles.delete";

    // Frame Data Management
    public const string ViewFrameData = "framedata.view";
    public const string CreateFrameData = "framedata.create";
    public const string EditFrameData = "framedata.edit";
    public const string DeleteFrameData = "framedata.delete";
    public const string ApproveFrameData = "framedata.approve";

    // Character Management
    public const string ViewCharacters = "characters.view";
    public const string ManageCharacters = "characters.manage";

    // Move Management
    public const string ViewMoves = "moves.view";
    public const string ManageMoves = "moves.manage";

    // System Management
    public const string ViewSystemInfo = "system.view";
    public const string ManageSystem = "system.manage";
    public const string ViewLogs = "system.logs";
    public const string ManageConfiguration = "system.config";

    // Content Management
    public const string ViewContent = "content.view";
    public const string CreateContent = "content.create";
    public const string EditContent = "content.edit";
    public const string DeleteContent = "content.delete";
    public const string PublishContent = "content.publish";

    // Analytics
    public const string ViewAnalytics = "analytics.view";
    public const string ExportAnalytics = "analytics.export";

    public static readonly Dictionary<string, List<string>> RolePermissionMap = new()
    {
        [Roles.Owner] =
        [
            ViewUsers,
            CreateUsers,
            EditUsers,
            DeleteUsers,
            ManageUserRoles,
            ActivateUsers,
            DeactivateUsers,
            ViewRoles,
            CreateRoles,
            EditRoles,
            DeleteRoles,
            ViewFrameData,
            CreateFrameData,
            EditFrameData,
            DeleteFrameData,
            ApproveFrameData,
            ViewCharacters,
            ManageCharacters,
            ViewMoves,
            ManageMoves,
            ViewSystemInfo,
            ManageSystem,
            ViewLogs,
            ManageConfiguration,
            ViewContent,
            CreateContent,
            EditContent,
            DeleteContent,
            PublishContent,
            ViewAnalytics,
            ExportAnalytics,
        ],
        [Roles.Administrator] =
        [
            ViewUsers,
            CreateUsers,
            EditUsers,
            ManageUserRoles,
            ActivateUsers,
            DeactivateUsers,
            ViewRoles,
            CreateRoles,
            EditRoles,
            ViewFrameData,
            CreateFrameData,
            EditFrameData,
            DeleteFrameData,
            ApproveFrameData,
            ViewCharacters,
            ManageCharacters,
            ViewMoves,
            ManageMoves,
            ViewSystemInfo,
            ViewLogs,
            ManageConfiguration,
            ViewContent,
            CreateContent,
            EditContent,
            DeleteContent,
            PublishContent,
            ViewAnalytics,
            ExportAnalytics,
        ],
        [Roles.Moderator] =
        [
            ViewUsers,
            EditUsers,
            ActivateUsers,
            DeactivateUsers,
            ViewRoles,
            ViewFrameData,
            CreateFrameData,
            EditFrameData,
            ApproveFrameData,
            ViewCharacters,
            ManageCharacters,
            ViewMoves,
            ManageMoves,
            ViewSystemInfo,
            ViewLogs,
            ViewContent,
            CreateContent,
            EditContent,
            PublishContent,
            ViewAnalytics,
        ],
        [Roles.Editor] =
        [
            ViewUsers,
            ViewFrameData,
            CreateFrameData,
            EditFrameData,
            ViewCharacters,
            ManageCharacters,
            ViewMoves,
            ManageMoves,
            ViewContent,
            CreateContent,
            EditContent,
            ViewAnalytics,
        ],
        [Roles.User] = [ViewFrameData, ViewCharacters, ViewMoves, ViewContent],
    };
}
