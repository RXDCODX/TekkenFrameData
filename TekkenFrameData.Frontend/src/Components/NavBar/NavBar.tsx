import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Home, Users, Zap, Settings, LogIn } from 'lucide-react';
import './NavBar.css';

const NavBar: React.FC = () => {
  const location = useLocation();

  const isActive = (path: string) => {
    return location.pathname === path;
  };

  return (
    <nav className="navbar">
      <div className="nav-container">
        <div className="nav-brand">
          <Link to="/" className="brand-link">
            <span className="brand-text">Tekken Frame Data</span>
          </Link>
        </div>
        
        <div className="nav-links">
          <Link 
            to="/" 
            className={`nav-link ${isActive('/') ? 'active' : ''}`}
          >
            <Home size={18} />
            <span>Home</span>
          </Link>
          
          <Link 
            to="/characters" 
            className={`nav-link ${isActive('/characters') ? 'active' : ''}`}
          >
            <Users size={18} />
            <span>Characters</span>
          </Link>
          
          <Link 
            to="/moves" 
            className={`nav-link ${isActive('/moves') ? 'active' : ''}`}
          >
            <Zap size={18} />
            <span>Moves</span>
          </Link>
          
          <Link 
            to="/admin/users" 
            className={`nav-link ${isActive('/admin/users') ? 'active' : ''}`}
          >
            <Settings size={18} />
            <span>Admin</span>
          </Link>
          
          <Link 
            to="/login" 
            className={`nav-link ${isActive('/login') ? 'active' : ''}`}
          >
            <LogIn size={18} />
            <span>Login</span>
          </Link>
        </div>
      </div>
    </nav>
  );
};

export default NavBar;
