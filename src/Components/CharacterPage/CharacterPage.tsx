import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Search, Filter, User, Calendar, Zap } from 'lucide-react';
import axios from 'axios';
import styles from './CharacterPage.module.scss';

interface Character {
  name: string;
  description?: string;
  linkToImage?: string;
  lastUpdateTime: string;
  strengths?: string[];
  weaknesses?: string[];
  movelist: any[];
}

const CharacterPage: React.FC = () => {
  const [characters, setCharacters] = useState<Character[]>([]);
  const [filteredCharacters, setFilteredCharacters] = useState<Character[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [sortBy, setSortBy] = useState<'name' | 'moves' | 'updated'>('name');

  useEffect(() => {
    fetchCharacters();
  }, []);

  useEffect(() => {
    filterAndSortCharacters();
  }, [characters, searchTerm, sortBy]);

  const fetchCharacters = async () => {
    try {
      setLoading(true);
      const response = await axios.get('/api/v1/Character');
      setCharacters(response.data);
      setError(null);
    } catch (err) {
      setError('Failed to load characters. Please try again later.');
      console.error('Error fetching characters:', err);
    } finally {
      setLoading(false);
    }
  };

  const filterAndSortCharacters = () => {
    let filtered = characters.filter(character =>
      character.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      (character.description && character.description.toLowerCase().includes(searchTerm.toLowerCase()))
    );

    // Sort characters
    filtered.sort((a, b) => {
      switch (sortBy) {
        case 'name':
          return a.name.localeCompare(b.name);
        case 'moves':
          return b.movelist.length - a.movelist.length;
        case 'updated':
          return new Date(b.lastUpdateTime).getTime() - new Date(a.lastUpdateTime).getTime();
        default:
          return 0;
      }
    });

    setFilteredCharacters(filtered);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  if (loading) {
    return (
      <div className={styles.characterPage}>
        <div className={styles.loadingContainer}>
          <div className={styles.loadingSpinner}></div>
          <p>Loading characters...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.characterPage}>
        <div className={styles.errorContainer}>
          <p className={styles.errorMessage}>{error}</p>
          <button onClick={fetchCharacters} className={`${styles.btn} ${styles.btnPrimary}`}>
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.characterPage}>
      <div className={styles.pageHeader}>
        <h1>Tekken Characters</h1>
        <p>Explore all characters and their frame data</p>
      </div>

      <div className={styles.controlsSection}>
        <div className={styles.searchContainer}>
          <Search size={20} className={styles.searchIcon} />
          <input
            type="text"
            placeholder="Search characters..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className={styles.searchInput}
          />
        </div>

        <div className={styles.sortContainer}>
          <Filter size={20} />
          <select
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value as 'name' | 'moves' | 'updated')}
            className={styles.sortSelect}
          >
            <option value="name">Sort by Name</option>
            <option value="moves">Sort by Moves Count</option>
            <option value="updated">Sort by Last Updated</option>
          </select>
        </div>
      </div>

      <div className={styles.statsBar}>
        <div className={styles.statItem}>
          <User size={16} />
          <span>{characters.length} Characters</span>
        </div>
        <div className={styles.statItem}>
          <Zap size={16} />
          <span>{characters.reduce((total, char) => total + char.movelist.length, 0)} Total Moves</span>
        </div>
        <div className={styles.statItem}>
          <Calendar size={16} />
          <span>Last updated: {characters.length > 0 ? formatDate(characters[0].lastUpdateTime) : 'N/A'}</span>
        </div>
      </div>

      <div className={styles.charactersGrid}>
        {filteredCharacters.map((character) => (
          <Link
            key={character.name}
            to={`/characters/${encodeURIComponent(character.name)}`}
            className={styles.characterCard}
          >
            <div className={styles.characterImage}>
              {character.linkToImage ? (
                <img src={character.linkToImage} alt={character.name} />
              ) : (
                <div className={styles.characterPlaceholder}>
                  <User size={40} />
                </div>
              )}
            </div>
            
            <div className={styles.characterInfo}>
              <h3 className={styles.characterName}>{character.name}</h3>
              
              {character.description && (
                <p className={styles.characterDescription}>
                  {character.description.length > 100
                    ? `${character.description.substring(0, 100)}...`
                    : character.description}
                </p>
              )}
              
              <div className={styles.characterStats}>
                <div className={styles.stat}>
                  <Zap size={16} />
                  <span>{character.movelist.length} moves</span>
                </div>
                
                <div className={styles.stat}>
                  <Calendar size={16} />
                  <span>{formatDate(character.lastUpdateTime)}</span>
                </div>
              </div>

              {character.strengths && character.strengths.length > 0 && (
                <div className={styles.characterTags}>
                  <span className={`${styles.tag} ${styles.strength}`}>Strengths: {character.strengths.slice(0, 2).join(', ')}</span>
                </div>
              )}
            </div>
          </Link>
        ))}
      </div>

      {filteredCharacters.length === 0 && (
        <div className={styles.noResults}>
          <p>No characters found matching your search criteria.</p>
          <button onClick={() => setSearchTerm('')} className={`${styles.btn} ${styles.btnSecondary}`}>
            Clear Search
          </button>
        </div>
      )}
    </div>
  );
};

export default CharacterPage; 

