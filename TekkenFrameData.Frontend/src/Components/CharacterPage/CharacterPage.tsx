import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Search, Filter, User, Calendar, Zap } from 'lucide-react';
import axios from 'axios';
import './CharacterPage.css';

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
      <div className="character-page">
        <div className="loading-container">
          <div className="loading-spinner"></div>
          <p>Loading characters...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="character-page">
        <div className="error-container">
          <p className="error-message">{error}</p>
          <button onClick={fetchCharacters} className="btn btn-primary">
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="character-page">
      <div className="page-header">
        <h1>Tekken Characters</h1>
        <p>Explore all characters and their frame data</p>
      </div>

      <div className="controls-section">
        <div className="search-container">
          <Search size={20} className="search-icon" />
          <input
            type="text"
            placeholder="Search characters..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="search-input"
          />
        </div>

        <div className="sort-container">
          <Filter size={20} />
          <select
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value as 'name' | 'moves' | 'updated')}
            className="sort-select"
          >
            <option value="name">Sort by Name</option>
            <option value="moves">Sort by Moves Count</option>
            <option value="updated">Sort by Last Updated</option>
          </select>
        </div>
      </div>

      <div className="stats-bar">
        <div className="stat-item">
          <User size={16} />
          <span>{characters.length} Characters</span>
        </div>
        <div className="stat-item">
          <Zap size={16} />
          <span>{characters.reduce((total, char) => total + char.movelist.length, 0)} Total Moves</span>
        </div>
        <div className="stat-item">
          <Calendar size={16} />
          <span>Last updated: {characters.length > 0 ? formatDate(characters[0].lastUpdateTime) : 'N/A'}</span>
        </div>
      </div>

      <div className="characters-grid">
        {filteredCharacters.map((character) => (
          <Link
            key={character.name}
            to={`/characters/${encodeURIComponent(character.name)}`}
            className="character-card"
          >
            <div className="character-image">
              {character.linkToImage ? (
                <img src={character.linkToImage} alt={character.name} />
              ) : (
                <div className="character-placeholder">
                  <User size={40} />
                </div>
              )}
            </div>
            
            <div className="character-info">
              <h3 className="character-name">{character.name}</h3>
              
              {character.description && (
                <p className="character-description">
                  {character.description.length > 100
                    ? `${character.description.substring(0, 100)}...`
                    : character.description}
                </p>
              )}
              
              <div className="character-stats">
                <div className="stat">
                  <Zap size={16} />
                  <span>{character.movelist.length} moves</span>
                </div>
                
                <div className="stat">
                  <Calendar size={16} />
                  <span>{formatDate(character.lastUpdateTime)}</span>
                </div>
              </div>

              {character.strengths && character.strengths.length > 0 && (
                <div className="character-tags">
                  <span className="tag strength">Strengths: {character.strengths.slice(0, 2).join(', ')}</span>
                </div>
              )}
            </div>
          </Link>
        ))}
      </div>

      {filteredCharacters.length === 0 && (
        <div className="no-results">
          <p>No characters found matching your search criteria.</p>
          <button onClick={() => setSearchTerm('')} className="btn btn-secondary">
            Clear Search
          </button>
        </div>
      )}
    </div>
  );
};

export default CharacterPage; 