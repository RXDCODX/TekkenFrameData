namespace TekkenFrameData.Library.Models.Identity;

public static class Roles
{
    public const string Owner = "Owner";
    public const string Administrator = "Administrator";
    public const string Moderator = "Moderator";
    public const string Editor = "Editor";
    public const string User = "User";

    public static readonly string[] AllRoles = [Owner, Administrator, Moderator, Editor, User];

    public static readonly Dictionary<string, RoleInfo> RoleDefinitions = new()
    {
        [Owner] = new RoleInfo
        {
            Name = Owner,
            DisplayName = "Владелец системы",
            Description =
                "Полный доступ ко всем функциям системы. Может управлять всеми пользователями, ролями и настройками.",
            Permissions = RolePermissions.RolePermissionMap[Owner],
            Priority = 100,
            IsSystemRole = true,
        },
        [Administrator] = new RoleInfo
        {
            Name = Administrator,
            DisplayName = "Администратор",
            Description =
                "Расширенные права администратора. Может управлять пользователями, ролями и большинством функций системы.",
            Permissions = RolePermissions.RolePermissionMap[Administrator],
            Priority = 80,
            IsSystemRole = true,
        },
        [Moderator] = new RoleInfo
        {
            Name = Moderator,
            DisplayName = "Модератор",
            Description =
                "Права модератора. Может управлять контентом, одобрять изменения и модерировать пользователей.",
            Permissions = RolePermissions.RolePermissionMap[Moderator],
            Priority = 60,
            IsSystemRole = true,
        },
        [Editor] = new RoleInfo
        {
            Name = Editor,
            DisplayName = "Редактор",
            Description =
                "Права редактора. Может создавать и редактировать контент, включая данные о кадрах.",
            Permissions = RolePermissions.RolePermissionMap[Editor],
            Priority = 40,
            IsSystemRole = true,
        },
        [User] = new RoleInfo
        {
            Name = User,
            DisplayName = "Пользователь",
            Description =
                "Базовые права пользователя. Может просматривать контент и данные о кадрах.",
            Permissions = RolePermissions.RolePermissionMap[User],
            Priority = 20,
            IsSystemRole = true,
        },
    };

    public static RoleInfo GetRoleInfo(string roleName)
    {
        return RoleDefinitions.TryGetValue(roleName, out var roleInfo)
            ? roleInfo
            : new RoleInfo
            {
                Name = roleName,
                DisplayName = roleName,
                Description = "Пользовательская роль",
                Priority = 0,
                IsSystemRole = false,
            };
    }

    public static List<string> GetRolePermissions(string roleName)
    {
        return RolePermissions.RolePermissionMap.TryGetValue(roleName, out var permissions)
            ? permissions
            : [];
    }

    public static bool HasPermission(string roleName, string permission)
    {
        var permissions = GetRolePermissions(roleName);
        return permissions.Contains(permission);
    }
}
