# TekkenFrameData.Library - Рефакторинг

## Обзор

Библиотека классов была полностью отрефакторена для улучшения архитектуры, добавления расширенной системы ролей и разрешений, а также повышения безопасности и масштабируемости.

## Основные изменения

### 1. Система ролей и разрешений

#### Новые модели:
- `RoleInfo` - расширенная информация о ролях
- `RolePermissions` - константы разрешений и их маппинг
- `UserManagementInfo` - расширенная информация о пользователях

#### Роли системы:
1. **Owner** (Владелец системы) - полный доступ ко всем функциям
2. **Administrator** (Администратор) - расширенные права администратора
3. **Moderator** (Модератор) - права модератора
4. **Editor** (Редактор) - права редактора
5. **User** (Пользователь) - базовые права

#### Категории разрешений:
- **User Management** - управление пользователями
- **Role Management** - управление ролями
- **Frame Data Management** - управление данными о кадрах
- **System Management** - управление системой
- **Content Management** - управление контентом
- **Analytics** - аналитика

### 2. Архитектура сервисов

#### Интерфейсы (Services/Interfaces/):
- `IUserService` - управление пользователями
- `IRoleService` - управление ролями
- `IPermissionService` - управление разрешениями

#### Реализации:
- `UserService` - реализация управления пользователями
- `RoleService` - реализация управления ролями
- `PermissionService` - реализация управления разрешениями

### 3. Атрибуты авторизации

#### Новые атрибуты:
- `RequirePermissionAttribute` - проверка одного разрешения
- `RequireAnyPermissionAttribute` - проверка любого из разрешений
- `RequireAllPermissionsAttribute` - проверка всех разрешений

### 4. Исключения

#### Новые исключения:
- `UserNotFoundException`
- `RoleNotFoundException`
- `PermissionDeniedException`
- `InvalidRoleException`
- `SystemRoleModificationException`
- `UserAlreadyInRoleException`
- `UserNotInRoleException`

## Структура проекта

```
TekkenFrameData.Library/
├── Attributes/
│   └── RequirePermissionAttribute.cs
├── Exceptions/
│   ├── UserManagementExceptions.cs
│   └── TekkenCharacterNotFoundException.cs
├── Models/
│   └── Identity/
│       ├── ApplicationUser.cs
│       ├── AuthModels.cs
│       ├── RoleInfo.cs
│       ├── Roles.cs
│       └── UserManagementModels.cs
├── Services/
│   ├── Interfaces/
│   │   ├── IUserService.cs
│   │   ├── IRoleService.cs
│   │   └── IPermissionService.cs
│   ├── UserService.cs
│   ├── RoleService.cs
│   └── PermissionService.cs
└── README.md
```

## Использование

### Регистрация сервисов

```csharp
// В Program.cs или Startup.cs
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
```

### Использование атрибутов авторизации

```csharp
[RequirePermission(RolePermissions.ViewUsers)]
public class UserController : ControllerBase
{
    [RequirePermission(RolePermissions.CreateUsers)]
    public async Task<IActionResult> CreateUser()
    {
        // Логика создания пользователя
    }

    [RequireAnyPermission(RolePermissions.EditUsers, RolePermissions.ManageUserRoles)]
    public async Task<IActionResult> UpdateUser()
    {
        // Логика обновления пользователя
    }
}
```

### Работа с ролями и разрешениями

```csharp
// Проверка разрешений
var hasPermission = await _permissionService.HasPermissionAsync(userId, RolePermissions.ViewUsers);

// Получение разрешений пользователя
var permissions = await _permissionService.GetUserPermissionsAsync(userId);

// Получение информации о роли
var roleInfo = Roles.GetRoleInfo("Administrator");
var rolePermissions = Roles.GetRolePermissions("Administrator");
```

## API Endpoints

### User Management
- `GET /api/v1/UserManagement/users` - получить всех пользователей
- `GET /api/v1/UserManagement/users/{userId}` - получить пользователя по ID
- `POST /api/v1/UserManagement/users/{userId}/roles` - добавить роль пользователю
- `DELETE /api/v1/UserManagement/users/{userId}/roles` - удалить роль у пользователя
- `PUT /api/v1/UserManagement/users/{userId}/activate` - активировать пользователя
- `PUT /api/v1/UserManagement/users/{userId}/deactivate` - деактивировать пользователя
- `GET /api/v1/UserManagement/roles` - получить все роли
- `GET /api/v1/UserManagement/permissions` - получить все разрешения
- `GET /api/v1/UserManagement/users/{userId}/permissions` - получить разрешения пользователя

### Role Management
- `GET /api/v1/RoleManagement/roles` - получить все роли
- `GET /api/v1/RoleManagement/roles/{roleName}` - получить роль по имени
- `POST /api/v1/RoleManagement/roles` - создать роль
- `PUT /api/v1/RoleManagement/roles/{roleName}` - обновить роль
- `DELETE /api/v1/RoleManagement/roles/{roleName}` - удалить роль
- `GET /api/v1/RoleManagement/roles/{roleName}/permissions` - получить разрешения роли
- `POST /api/v1/RoleManagement/roles/{roleName}/permissions` - добавить разрешение к роли
- `DELETE /api/v1/RoleManagement/roles/{roleName}/permissions` - удалить разрешение у роли
- `GET /api/v1/RoleManagement/permissions` - получить все разрешения

## Безопасность

1. **Системные роли защищены** - нельзя удалить или изменить разрешения системных ролей
2. **Владелец системы защищен** - нельзя удалить роль Owner у пользователя-владельца
3. **Гранулярные разрешения** - каждый endpoint защищен конкретными разрешениями
4. **Валидация данных** - все входные данные валидируются

## Миграция

При обновлении существующего кода:

1. Замените `[Authorize(Policy = "RequireAdminRole")]` на `[RequirePermission(RolePermissions.ViewUsers)]`
2. Обновите вызовы сервисов для использования новых интерфейсов
3. Добавьте обработку новых исключений
4. Обновите модели для использования новых типов данных

## Тестирование

Для тестирования новой функциональности:

1. Создайте пользователей с разными ролями
2. Проверьте работу атрибутов авторизации
3. Протестируйте API endpoints
4. Убедитесь в корректной работе системы разрешений 