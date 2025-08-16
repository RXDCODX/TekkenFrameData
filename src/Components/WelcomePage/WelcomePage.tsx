import React from 'react';
import { Link } from 'react-router-dom';
import { Sword, Users, Database, Zap, Shield, Target } from 'lucide-react';
import styles from './WelcomePage.module.scss';

const WelcomePage: React.FC = () => {
  return (
    <div className={styles.welcomePage}>
      {/* Hero Section */}
      <section className={styles.heroSection}>
        <div className={styles.heroContent}>
          <h1 className={styles.heroTitle}>
            Tekken Frame Data
            <span className={styles.heroSubtitle}>Master the Art of Combat</span>
          </h1>
          <p className={styles.heroDescription}>
            Comprehensive frame data, character guides, and move analysis for Tekken 8. 
            Perfect your gameplay with detailed statistics and strategic insights.
          </p>
          <div className={styles.heroButtons}>
            <Link to="/characters" className={`${styles.btn} ${styles.btnPrimary}`}>
              <Sword size={20} />
              Explore Characters
            </Link>
            <Link to="/moves" className={`${styles.btn} ${styles.btnSecondary}`}>
              <Database size={20} />
              Browse Moves
            </Link>
          </div>
        </div>
        <div className={styles.heroVisual}>
          <div className={styles.heroImagePlaceholder}>
            <Target size={80} className={styles.heroIcon} />
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className={styles.featuresSection}>
        <h2 className={styles.sectionTitle}>Why Choose Tekken Frame Data?</h2>
        <div className={styles.featuresGrid}>
          <div className={styles.featureCard}>
            <div className={styles.featureIcon}>
              <Database size={32} />
            </div>
            <h3>Comprehensive Database</h3>
            <p>Access detailed frame data for every character and move in Tekken 8, including startup, block, hit, and counter-hit frames.</p>
          </div>
          
          <div className={styles.featureCard}>
            <div className={styles.featureIcon}>
              <Users size={32} />
            </div>
            <h3>Character Guides</h3>
            <p>Learn character strengths, weaknesses, and optimal strategies with our detailed character analysis and move breakdowns.</p>
          </div>
          
          <div className={styles.featureCard}>
            <div className={styles.featureIcon}>
              <Zap size={32} />
            </div>
            <h3>Real-time Updates</h3>
            <p>Stay current with the latest game patches and balance changes. Our database is constantly updated with new information.</p>
          </div>
          
          <div className={styles.featureCard}>
            <div className={styles.featureIcon}>
              <Shield size={32} />
            </div>
            <h3>Advanced Search</h3>
            <p>Find exactly what you need with our powerful search and filter system. Search by character, move type, frame advantage, and more.</p>
          </div>
        </div>
      </section>

      {/* Stats Section */}
      <section className={styles.statsSection}>
        <div className={styles.statsContainer}>
          <div className={styles.statItem}>
            <h3 className={styles.statNumber}>32+</h3>
            <p className={styles.statLabel}>Characters</p>
          </div>
          <div className={styles.statItem}>
            <h3 className={styles.statNumber}>2000+</h3>
            <p className={styles.statLabel}>Moves</p>
          </div>
          <div className={styles.statItem}>
            <h3 className={styles.statNumber}>24/7</h3>
            <p className={styles.statLabel}>Availability</p>
          </div>
          <div className={styles.statItem}>
            <h3 className={styles.statNumber}>100%</h3>
            <p className={styles.statLabel}>Accuracy</p>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className={styles.ctaSection}>
        <div className={styles.ctaContent}>
          <h2>Ready to Level Up Your Game?</h2>
          <p>Join thousands of players who use Tekken Frame Data to improve their gameplay and dominate the competition.</p>
          <Link to="/characters" className={`${styles.btn} ${styles.btnPrimary} ${styles.btnLarge}`}>
            Get Started Now
          </Link>
        </div>
      </section>
    </div>
  );
};

export default WelcomePage; 

