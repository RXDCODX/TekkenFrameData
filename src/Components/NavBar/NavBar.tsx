import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Home, Users, Zap, Settings, LogIn } from 'lucide-react';
import styles from './NavBar.module.scss';

const NavBar: React.FC = () => {
  const location = useLocation();

  const isActive = (path: string) => {
    return location.pathname === path;
  };

  return (
    <nav className={styles.navbar}>
      <div className={styles.navContainer}>
        <div className={styles.navBrand}>
          <Link to="/" className={styles.brandLink}>
            <span className={styles.brandText}>Tekken Frame Data</span>
          </Link>
        </div>
        
        <div className={styles.navLinks}>
          <Link 
            to="/" 
            className={`${styles.navLink} ${isActive('/') ? styles.active : ''}`}
          >
            <Home size={18} />
            <span>Home</span>
          </Link>
          
          <Link 
            to="/characters" 
            className={`${styles.navLink} ${isActive('/characters') ? styles.active : ''}`}
          >
            <Users size={18} />
            <span>Characters</span>
          </Link>
          
          <Link 
            to="/moves" 
            className={`${styles.navLink} ${isActive('/moves') ? styles.active : ''}`}
          >
            <Zap size={18} />
            <span>Moves</span>
          </Link>
          
          <Link 
            to="/admin/users" 
            className={`${styles.navLink} ${isActive('/admin/users') ? styles.active : ''}`}
          >
            <Settings size={18} />
            <span>Admin</span>
          </Link>
          
          <Link 
            to="/login" 
            className={`${styles.navLink} ${isActive('/login') ? styles.active : ''}`}
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


