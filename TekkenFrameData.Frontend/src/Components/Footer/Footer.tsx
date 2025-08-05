import React from 'react';
import { Heart, Github, ExternalLink } from 'lucide-react';
import './Footer.css';

const Footer: React.FC = () => {
  return (
    <footer className="footer">
      <div className="footer-content">
        <div className="footer-section">
          <h3>Tekken Frame Data</h3>
          <p>Comprehensive frame data and character guides for Tekken 8</p>
        </div>
        
        <div className="footer-section">
          <h4>Quick Links</h4>
          <ul>
            <li><a href="/characters">Characters</a></li>
            <li><a href="/moves">Moves</a></li>
            <li><a href="/">Home</a></li>
          </ul>
        </div>
        
        <div className="footer-section">
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
      
      <div className="footer-bottom">
        <p>
          Made with <Heart size={16} className="heart-icon" /> by Tekken Frame Data Team
        </p>
        <p>&copy; 2024 Tekken Frame Data. All rights reserved.</p>
      </div>
    </footer>
  );
};

export default Footer;
