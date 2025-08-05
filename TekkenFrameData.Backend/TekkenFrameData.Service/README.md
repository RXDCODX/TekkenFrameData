# Tekken Frame Data Service

This is the backend service for the Tekken Frame Data application, providing REST APIs for authentication, user management, and frame data operations.

## Features

- **Authentication & Authorization**
  - JWT-based authentication
  - Role-based access control (RBAC)
  - OAuth integration with Twitch and Google
  - Custom permission system

- **User Management**
  - User registration and login
  - Role assignment and management
  - Permission management
  - User profile management

- **Frame Data Management**
  - Character CRUD operations
  - Move CRUD operations
  - Search and filtering capabilities
  - Statistics and analytics

## Setup Instructions

### Prerequisites

- .NET 9.0 SDK
- PostgreSQL database
- Twitch Developer Account (for OAuth)
- Google Cloud Console Account (for OAuth)

### Database Setup

1. Create a PostgreSQL database
2. Update the connection string in `appsettings.json`
3. Run the migrations:
   ```bash
   dotnet ef database update
   ```

### OAuth Configuration

#### Twitch OAuth Setup

1. Go to [Twitch Developer Console](https://dev.twitch.tv/console)
2. Create a new application
3. Set the OAuth Redirect URLs:
   - `https://localhost:5000/api/v1/auth/twitch-callback` (for development)
   - `https://yourdomain.com/api/v1/auth/twitch-callback` (for production)
4. Copy the Client ID and Client Secret
5. Update `appsettings.json`:

```json
{
  "OAuth": {
    "Twitch": {
      "ClientId": "your-twitch-client-id",
      "ClientSecret": "your-twitch-client-secret",
      "RedirectUri": "https://localhost:5000/api/v1/auth/twitch-callback"
    }
  }
}
```

#### Google OAuth Setup

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing one
3. Enable the Google+ API
4. Go to Credentials → Create Credentials → OAuth 2.0 Client IDs
5. Set the authorized redirect URIs:
   - `https://localhost:5000/api/v1/auth/google-callback` (for development)
   - `https://yourdomain.com/api/v1/auth/google-callback` (for production)
6. Copy the Client ID and Client Secret
7. Update `appsettings.json`:

```json
{
  "OAuth": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret",
      "RedirectUri": "https://localhost:5000/api/v1/auth/google-callback"
    }
  }
}
```

### JWT Configuration

Update the JWT settings in `appsettings.json`:

```json
{
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-with-at-least-32-characters-long",
    "Issuer": "TekkenFrameData",
    "Audience": "TekkenFrameData",
    "ExpiresInMinutes": 60
  }
}
```

**Important**: Generate a strong secret key for production use.

### Running the Application

1. Install dependencies:
   ```bash
   dotnet restore
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. The service will be available at:
   - Development: `https://localhost:5000`
   - API Documentation: `https://localhost:5000/swagger`

## API Endpoints

### Authentication

- `POST /api/v1/auth/login` - User login
- `POST /api/v1/auth/register` - User registration
- `GET /api/v1/auth/twitch-login` - Initiate Twitch OAuth
- `GET /api/v1/auth/google-login` - Initiate Google OAuth
- `GET /api/v1/auth/twitch-callback` - Twitch OAuth callback
- `GET /api/v1/auth/google-callback` - Google OAuth callback
- `GET /api/v1/auth/me` - Get current user info
- `POST /api/v1/auth/logout` - User logout

### User Management

- `GET /api/v1/UserManagement/users` - Get all users
- `POST /api/v1/UserManagement/users` - Create user
- `PUT /api/v1/UserManagement/users/{id}` - Update user
- `DELETE /api/v1/UserManagement/users/{id}` - Delete user
- `GET /api/v1/UserManagement/permissions` - Get all permissions

### Role Management

- `GET /api/v1/RoleManagement/roles` - Get all roles
- `POST /api/v1/RoleManagement/roles` - Create role
- `PUT /api/v1/RoleManagement/roles/{name}` - Update role
- `DELETE /api/v1/RoleManagement/roles/{name}` - Delete role

### Frame Data

- `GET /api/v1/Character` - Get all characters
- `GET /api/v1/Character/{name}` - Get character by name
- `POST /api/v1/Character` - Create character
- `PUT /api/v1/Character/{name}` - Update character
- `DELETE /api/v1/Character/{name}` - Delete character

- `GET /api/v1/Move` - Get all moves
- `GET /api/v1/Move/character/{characterName}` - Get moves by character
- `GET /api/v1/Move/{characterName}/{command}` - Get specific move
- `POST /api/v1/Move` - Create move
- `PUT /api/v1/Move/{characterName}/{command}` - Update move
- `DELETE /api/v1/Move/{characterName}/{command}` - Delete move

## Permissions

The application uses a granular permission system:

### User Management
- `users.view` - View users
- `users.manage` - Create, update, delete users

### Role Management
- `roles.view` - View roles
- `roles.manage` - Create, update, delete roles

### Character Management
- `characters.view` - View characters
- `characters.manage` - Create, update, delete characters

### Move Management
- `moves.view` - View moves
- `moves.manage` - Create, update, delete moves

## Roles

Default roles are automatically created:

- **Owner** - Full access to all features
- **Administrator** - Full access except owner-specific features
- **Moderator** - User and content moderation
- **Editor** - Content editing capabilities
- **User** - Basic access to view content

## Security

- All sensitive endpoints require authentication
- Role-based access control for all operations
- JWT tokens with configurable expiration
- HTTPS enforcement in production
- Input validation and sanitization

## Development

### Project Structure

```
TekkenFrameData.Service/
├── API/
│   └── v1/
│       ├── AuthController/
│       ├── UserManagementController/
│       ├── RoleManagementController/
│       ├── CharacterController/
│       └── MoveController/
├── Program.cs
├── appsettings.json
└── README.md
```

### Adding New Features

1. Create new models in `TekkenFrameData.Library/Models/`
2. Add new permissions to `RolePermissions` class
3. Create new controllers in `API/v1/`
4. Update role permission mappings
5. Add frontend components as needed

## Deployment

### Docker

1. Build the image:
   ```bash
   docker build -t tekken-frame-data-service .
   ```

2. Run the container:
   ```bash
   docker run -p 5000:5000 tekken-frame-data-service
   ```

### Production Considerations

- Use strong JWT secret keys
- Configure HTTPS
- Set up proper CORS policies
- Use environment variables for sensitive configuration
- Set up monitoring and logging
- Configure database connection pooling
- Set up backup strategies

## Troubleshooting

### Common Issues

1. **OAuth redirect errors**: Ensure redirect URIs match exactly
2. **Database connection**: Check connection string and database availability
3. **JWT errors**: Verify secret key configuration
4. **CORS issues**: Configure CORS policies for frontend domain

### Logs

Check application logs for detailed error information:
```bash
dotnet run --environment Production
```

## Support

For issues and questions:
1. Check the logs for error details
2. Verify configuration settings
3. Test OAuth endpoints individually
4. Ensure database migrations are up to date 