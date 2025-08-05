import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { 
  Users, 
  Shield, 
  UserPlus, 
  Edit, 
  Trash2, 
  Save, 
  X, 
  Eye, 
  EyeOff,
  Search,
  Filter,
  MoreVertical,
  Check,
  AlertCircle
} from 'lucide-react';
import './UserManagementPage.css';

interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  roles: string[];
  lastLoginTime?: string;
  registrationDate?: string;
}

interface Role {
  name: string;
  displayName: string;
  description: string;
  permissions: string[];
  priority: number;
  isSystemRole: boolean;
}

interface Permission {
  name: string;
  displayName: string;
  category: string;
  description: string;
}

const UserManagementPage: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [roles, setRoles] = useState<Role[]>([]);
  const [permissions, setPermissions] = useState<Permission[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  // User management states
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [showUserModal, setShowUserModal] = useState(false);
  const [showRoleModal, setShowRoleModal] = useState(false);
  
  // Search and filter states
  const [searchTerm, setSearchTerm] = useState('');
  const [roleFilter, setRoleFilter] = useState<string>('all');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  
  // Form states
  const [newUser, setNewUser] = useState({
    email: '',
    firstName: '',
    lastName: '',
    password: '',
    roles: [] as string[]
  });
  
  const [newRole, setNewRole] = useState({
    name: '',
    displayName: '',
    description: '',
    permissions: [] as string[]
  });

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [usersResponse, rolesResponse, permissionsResponse] = await Promise.all([
        axios.get('/api/v1/UserManagement/users'),
        axios.get('/api/v1/RoleManagement/roles'),
        axios.get('/api/v1/UserManagement/permissions')
      ]);
      
      setUsers(usersResponse.data);
      setRoles(rolesResponse.data);
      setPermissions(permissionsResponse.data);
    } catch (err) {
      setError('Failed to load data');
      console.error('Error fetching data:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleUserEdit = (user: User) => {
    setEditingUser({ ...user });
    setShowUserModal(true);
  };

  const handleUserSave = async () => {
    if (!editingUser) return;
    
    try {
      await axios.put(`/api/v1/UserManagement/users/${editingUser.id}`, editingUser);
      setUsers(users.map(u => u.id === editingUser.id ? editingUser : u));
      setShowUserModal(false);
      setEditingUser(null);
    } catch (err) {
      setError('Failed to update user');
    }
  };

  const handleUserDelete = async (userId: string) => {
    if (!confirm('Are you sure you want to delete this user?')) return;
    
    try {
      await axios.delete(`/api/v1/UserManagement/users/${userId}`);
      setUsers(users.filter(u => u.id !== userId));
    } catch (err) {
      setError('Failed to delete user');
    }
  };

  const handleCreateUser = async () => {
    try {
      const response = await axios.post('/api/v1/UserManagement/users', newUser);
      setUsers([...users, response.data]);
      setNewUser({ email: '', firstName: '', lastName: '', password: '', roles: [] });
      setShowUserModal(false);
    } catch (err) {
      setError('Failed to create user');
    }
  };

  const handleCreateRole = async () => {
    try {
      const response = await axios.post('/api/v1/RoleManagement/roles', newRole);
      setRoles([...roles, response.data]);
      setNewRole({ name: '', displayName: '', description: '', permissions: [] });
      setShowRoleModal(false);
    } catch (err) {
      setError('Failed to create role');
    }
  };

  const filteredUsers = users.filter(user => {
    const matchesSearch = user.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         user.firstName.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         user.lastName.toLowerCase().includes(searchTerm.toLowerCase());
    
    const matchesRole = roleFilter === 'all' || user.roles.includes(roleFilter);
    const matchesStatus = statusFilter === 'all' || 
                         (statusFilter === 'active' && user.isActive) ||
                         (statusFilter === 'inactive' && !user.isActive);
    
    return matchesSearch && matchesRole && matchesStatus;
  });

  if (loading) {
    return (
      <div className="user-management-page">
        <div className="loading-spinner">Loading...</div>
      </div>
    );
  }

  return (
    <div className="user-management-page">
      <div className="page-header">
        <div className="header-content">
          <h1><Users className="header-icon" /> User Management</h1>
          <p>Manage users, roles, and permissions for the Tekken Frame Data application</p>
        </div>
        <div className="header-actions">
          <button 
            className="btn btn-primary"
            onClick={() => setShowUserModal(true)}
          >
            <UserPlus size={16} />
            Add User
          </button>
          <button 
            className="btn btn-secondary"
            onClick={() => setShowRoleModal(true)}
          >
            <Shield size={16} />
            Add Role
          </button>
        </div>
      </div>

      {error && (
        <div className="error-message">
          <AlertCircle size={16} />
          {error}
          <button onClick={() => setError(null)}>×</button>
        </div>
      )}

      <div className="filters-section">
        <div className="search-box">
          <Search size={16} />
          <input
            type="text"
            placeholder="Search users..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
        
        <div className="filter-controls">
          <select 
            value={roleFilter} 
            onChange={(e) => setRoleFilter(e.target.value)}
            className="filter-select"
          >
            <option value="all">All Roles</option>
            {roles.map(role => (
              <option key={role.name} value={role.name}>{role.displayName}</option>
            ))}
          </select>
          
          <select 
            value={statusFilter} 
            onChange={(e) => setStatusFilter(e.target.value)}
            className="filter-select"
          >
            <option value="all">All Status</option>
            <option value="active">Active</option>
            <option value="inactive">Inactive</option>
          </select>
        </div>
      </div>

      <div className="content-grid">
        <div className="users-section">
          <h2>Users ({filteredUsers.length})</h2>
          <div className="users-table">
            <table>
              <thead>
                <tr>
                  <th>User</th>
                  <th>Email</th>
                  <th>Roles</th>
                  <th>Status</th>
                  <th>Last Login</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredUsers.map(user => (
                  <tr key={user.id}>
                    <td>
                      <div className="user-info">
                        <div className="user-avatar">
                          {user.firstName.charAt(0)}{user.lastName.charAt(0)}
                        </div>
                        <div className="user-details">
                          <div className="user-name">{user.firstName} {user.lastName}</div>
                        </div>
                      </div>
                    </td>
                    <td>{user.email}</td>
                    <td>
                      <div className="user-roles">
                        {user.roles.map(role => (
                          <span key={role} className="role-badge">
                            {roles.find(r => r.name === role)?.displayName || role}
                          </span>
                        ))}
                      </div>
                    </td>
                    <td>
                      <span className={`status-badge ${user.isActive ? 'active' : 'inactive'}`}>
                        {user.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td>{user.lastLoginTime ? new Date(user.lastLoginTime).toLocaleDateString() : 'Never'}</td>
                    <td>
                      <div className="action-buttons">
                        <button 
                          className="btn-icon"
                          onClick={() => handleUserEdit(user)}
                          title="Edit User"
                        >
                          <Edit size={14} />
                        </button>
                        <button 
                          className="btn-icon btn-danger"
                          onClick={() => handleUserDelete(user.id)}
                          title="Delete User"
                        >
                          <Trash2 size={14} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        <div className="roles-section">
          <h2>Roles ({roles.length})</h2>
          <div className="roles-grid">
            {roles.map(role => (
              <div key={role.name} className="role-card">
                <div className="role-header">
                  <h3>{role.displayName}</h3>
                  {role.isSystemRole && <span className="system-badge">System</span>}
                </div>
                <p className="role-description">{role.description}</p>
                <div className="role-permissions">
                  <h4>Permissions ({role.permissions.length})</h4>
                  <div className="permissions-list">
                    {role.permissions.map(permission => (
                      <span key={permission} className="permission-badge">
                        {permissions.find(p => p.name === permission)?.displayName || permission}
                      </span>
                    ))}
                  </div>
                </div>
                <div className="role-priority">
                  Priority: {role.priority}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* User Modal */}
      {showUserModal && (
        <div className="modal-overlay">
          <div className="modal">
            <div className="modal-header">
              <h3>{editingUser ? 'Edit User' : 'Add New User'}</h3>
              <button onClick={() => {
                setShowUserModal(false);
                setEditingUser(null);
              }}>
                <X size={20} />
              </button>
            </div>
            <div className="modal-body">
              <div className="form-group">
                <label>Email</label>
                <input
                  type="email"
                  value={editingUser?.email || newUser.email}
                  onChange={(e) => editingUser 
                    ? setEditingUser({...editingUser, email: e.target.value})
                    : setNewUser({...newUser, email: e.target.value})
                  }
                />
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label>First Name</label>
                  <input
                    type="text"
                    value={editingUser?.firstName || newUser.firstName}
                    onChange={(e) => editingUser 
                      ? setEditingUser({...editingUser, firstName: e.target.value})
                      : setNewUser({...newUser, firstName: e.target.value})
                    }
                  />
                </div>
                <div className="form-group">
                  <label>Last Name</label>
                  <input
                    type="text"
                    value={editingUser?.lastName || newUser.lastName}
                    onChange={(e) => editingUser 
                      ? setEditingUser({...editingUser, lastName: e.target.value})
                      : setNewUser({...newUser, lastName: e.target.value})
                    }
                  />
                </div>
              </div>
              {!editingUser && (
                <div className="form-group">
                  <label>Password</label>
                  <input
                    type="password"
                    value={newUser.password}
                    onChange={(e) => setNewUser({...newUser, password: e.target.value})}
                  />
                </div>
              )}
              <div className="form-group">
                <label>Roles</label>
                <div className="roles-checkboxes">
                  {roles.map(role => (
                    <label key={role.name} className="checkbox-label">
                      <input
                        type="checkbox"
                        checked={editingUser 
                          ? editingUser.roles.includes(role.name)
                          : newUser.roles.includes(role.name)
                        }
                        onChange={(e) => {
                          if (editingUser) {
                            const updatedRoles = e.target.checked
                              ? [...editingUser.roles, role.name]
                              : editingUser.roles.filter(r => r !== role.name);
                            setEditingUser({...editingUser, roles: updatedRoles});
                          } else {
                            const updatedRoles = e.target.checked
                              ? [...newUser.roles, role.name]
                              : newUser.roles.filter(r => r !== role.name);
                            setNewUser({...newUser, roles: updatedRoles});
                          }
                        }}
                      />
                      <span>{role.displayName}</span>
                    </label>
                  ))}
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button 
                className="btn btn-secondary"
                onClick={() => {
                  setShowUserModal(false);
                  setEditingUser(null);
                }}
              >
                Cancel
              </button>
              <button 
                className="btn btn-primary"
                onClick={editingUser ? handleUserSave : handleCreateUser}
              >
                <Save size={16} />
                {editingUser ? 'Save Changes' : 'Create User'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Role Modal */}
      {showRoleModal && (
        <div className="modal-overlay">
          <div className="modal">
            <div className="modal-header">
              <h3>Add New Role</h3>
              <button onClick={() => setShowRoleModal(false)}>
                <X size={20} />
              </button>
            </div>
            <div className="modal-body">
              <div className="form-group">
                <label>Role Name</label>
                <input
                  type="text"
                  value={newRole.name}
                  onChange={(e) => setNewRole({...newRole, name: e.target.value})}
                  placeholder="e.g., moderator"
                />
              </div>
              <div className="form-group">
                <label>Display Name</label>
                <input
                  type="text"
                  value={newRole.displayName}
                  onChange={(e) => setNewRole({...newRole, displayName: e.target.value})}
                  placeholder="e.g., Moderator"
                />
              </div>
              <div className="form-group">
                <label>Description</label>
                <textarea
                  value={newRole.description}
                  onChange={(e) => setNewRole({...newRole, description: e.target.value})}
                  placeholder="Describe the role's purpose and responsibilities"
                />
              </div>
              <div className="form-group">
                <label>Permissions</label>
                <div className="permissions-grid">
                  {permissions.map(permission => (
                    <label key={permission.name} className="checkbox-label">
                      <input
                        type="checkbox"
                        checked={newRole.permissions.includes(permission.name)}
                        onChange={(e) => {
                          const updatedPermissions = e.target.checked
                            ? [...newRole.permissions, permission.name]
                            : newRole.permissions.filter(p => p !== permission.name);
                          setNewRole({...newRole, permissions: updatedPermissions});
                        }}
                      />
                      <div className="permission-info">
                        <span className="permission-name">{permission.displayName}</span>
                        <span className="permission-category">{permission.category}</span>
                      </div>
                    </label>
                  ))}
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button 
                className="btn btn-secondary"
                onClick={() => setShowRoleModal(false)}
              >
                Cancel
              </button>
              <button 
                className="btn btn-primary"
                onClick={handleCreateRole}
              >
                <Save size={16} />
                Create Role
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default UserManagementPage; 