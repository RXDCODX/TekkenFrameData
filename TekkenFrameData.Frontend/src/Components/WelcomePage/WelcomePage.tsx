import React from 'react';
import { Link } from 'react-router-dom';
import { Sword, Users, Database, Zap, Shield, Target } from 'lucide-react';
import './WelcomePage.css';

const WelcomePage: React.FC = () => {
  return (
    <div className="welcome-page">
      {/* Hero Section */}
      <section className="hero-section">
        <div className="hero-content">
          <h1 className="hero-title">
            Tekken Frame Data
            <span className="hero-subtitle">Master the Art of Combat</span>
          </h1>
          <p className="hero-description">
            Comprehensive frame data, character guides, and move analysis for Tekken 8. 
            Perfect your gameplay with detailed statistics and strategic insights.
          </p>
          <div className="hero-buttons">
            <Link to="/characters" className="btn btn-primary">
              <Sword size={20} />
              Explore Characters
            </Link>
            <Link to="/moves" className="btn btn-secondary">
              <Database size={20} />
              Browse Moves
            </Link>
          </div>
        </div>
        <div className="hero-visual">
          <div className="hero-image-placeholder">
            <Target size={80} className="hero-icon" />
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="features-section">
        <h2 className="section-title">Why Choose Tekken Frame Data?</h2>
        <div className="features-grid">
          <div className="feature-card">
            <div className="feature-icon">
              <Database size={32} />
            </div>
            <h3>Comprehensive Database</h3>
            <p>Access detailed frame data for every character and move in Tekken 8, including startup, block, hit, and counter-hit frames.</p>
          </div>
          
          <div className="feature-card">
            <div className="feature-icon">
              <Users size={32} />
            </div>
            <h3>Character Guides</h3>
            <p>Learn character strengths, weaknesses, and optimal strategies with our detailed character analysis and move breakdowns.</p>
          </div>
          
          <div className="feature-card">
            <div className="feature-icon">
              <Zap size={32} />
            </div>
            <h3>Real-time Updates</h3>
            <p>Stay current with the latest game patches and balance changes. Our database is constantly updated with new information.</p>
          </div>
          
          <div className="feature-card">
            <div className="feature-icon">
              <Shield size={32} />
            </div>
            <h3>Advanced Search</h3>
            <p>Find exactly what you need with our powerful search and filter system. Search by character, move type, frame advantage, and more.</p>
          </div>
        </div>
      </section>

      {/* Stats Section */}
      <section className="stats-section">
        <div className="stats-container">
          <div className="stat-item">
            <h3 className="stat-number">32+</h3>
            <p className="stat-label">Characters</p>
          </div>
          <div className="stat-item">
            <h3 className="stat-number">2000+</h3>
            <p className="stat-label">Moves</p>
          </div>
          <div className="stat-item">
            <h3 className="stat-number">24/7</h3>
            <p className="stat-label">Availability</p>
          </div>
          <div className="stat-item">
            <h3 className="stat-number">100%</h3>
            <p className="stat-label">Accuracy</p>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="cta-section">
        <div className="cta-content">
          <h2>Ready to Level Up Your Game?</h2>
          <p>Join thousands of players who use Tekken Frame Data to improve their gameplay and dominate the competition.</p>
          <Link to="/characters" className="btn btn-primary btn-large">
            Get Started Now
          </Link>
        </div>
      </section>
    </div>
  );
};

export default WelcomePage; 