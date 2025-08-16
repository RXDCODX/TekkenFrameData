import React from 'react';
import { Heart, Github, ExternalLink } from 'lucide-react';
import styles from './Footer.module.scss';

const Footer: React.FC = () => {
  return (
    <footer className={styles.footer}>
      <div className={styles.footerContent}>
        <div className={styles.footerSection}>
          <h3>Tekken Frame Data</h3>
          <p>Comprehensive frame data and character guides for Tekken 8</p>
        </div>
        
        <div className={styles.footerSection}>
          <h4>Quick Links</h4>
          <ul>
            <li><a href="/characters">Characters</a></li>
            <li><a href="/moves">Moves</a></li>
            <li><a href="/">Home</a></li>
          </ul>
        </div>
        
        <div className={styles.footerSection}>
          <h4>Resources</h4>
          <ul>
            <li>
              <a href="https://github.com" target="_blank" rel="noopener noreferrer">
                <Github size={16} />
                GitHub
              </a>
            </li>
            <li>
              <a href="https://tekken.com" target="_blank" rel="noopener noreferrer">
                <ExternalLink size={16} />
                Official Tekken
              </a>
            </li>
          </ul>
        </div>
      </div>
      
      <div className={styles.footerBottom}>
        <p>
          Made with <Heart size={16} className={styles.heartIcon} /> by Tekken Frame Data Team
        </p>
        <p>&copy; 2024 Tekken Frame Data. All rights reserved.</p>
      </div>
    </footer>
  );
};

export default Footer;


