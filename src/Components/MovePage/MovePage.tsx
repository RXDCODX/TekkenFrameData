import React, { useState, useEffect } from 'react';
import { Search, Zap, Shield, Target, Flame, Eye } from 'lucide-react';
import axios from 'axios';
import styles from './MovePage.module.scss';

interface Move {
  characterName: string;
  command: string;
  hitLevel?: string;
  damage?: string;
  startUpFrame?: string;
  blockFrame?: string;
  hitFrame?: string;
  counterHitFrame?: string;
  notes?: string;
  heatEngage: boolean;
  heatSmash: boolean;
  powerCrush: boolean;
  throw: boolean;
  homing: boolean;
  tornado: boolean;
  heatBurst: boolean;
  requiresHeat: boolean;
  stanceCode?: string;
  stanceName?: string;
  isUserChanged: boolean;
}

const MovePage: React.FC = () => {
  const [moves, setMoves] = useState<Move[]>([]);
  const [filteredMoves, setFilteredMoves] = useState<Move[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCharacter, setSelectedCharacter] = useState('');
  const [selectedHitLevel, setSelectedHitLevel] = useState('');
  const [showHeatEngage, setShowHeatEngage] = useState(false);
  const [showPowerCrush, setShowPowerCrush] = useState(false);
  const [showThrows, setShowThrows] = useState(false);
  const [showHoming, setShowHoming] = useState(false);
  const [sortBy, setSortBy] = useState<'command' | 'startup' | 'block' | 'damage'>('command');

  useEffect(() => {
    fetchMoves();
  }, []);

  useEffect(() => {
    filterAndSortMoves();
  }, [moves, searchTerm, selectedCharacter, selectedHitLevel, showHeatEngage, showPowerCrush, showThrows, showHoming, sortBy]);

  const fetchMoves = async () => {
    try {
      setLoading(true);
      const response = await axios.get('/api/v1/Move');
      setMoves(response.data);
      setError(null);
    } catch (err) {
      setError('Failed to load moves. Please try again later.');
      console.error('Error fetching moves:', err);
    } finally {
      setLoading(false);
    }
  };

  const filterAndSortMoves = () => {
    let filtered = moves.filter(move => {
      // Search filter
      const matchesSearch = 
        move.command.toLowerCase().includes(searchTerm.toLowerCase()) ||
        move.characterName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (move.notes && move.notes.toLowerCase().includes(searchTerm.toLowerCase()));

      // Character filter
      const matchesCharacter = !selectedCharacter || move.characterName.toLowerCase() === selectedCharacter.toLowerCase();

      // Hit level filter
      const matchesHitLevel = !selectedHitLevel || (move.hitLevel && move.hitLevel.toLowerCase() === selectedHitLevel.toLowerCase());

      // Special move filters
      const matchesHeatEngage = !showHeatEngage || move.heatEngage;
      const matchesPowerCrush = !showPowerCrush || move.powerCrush;
      const matchesThrows = !showThrows || move.throw;
      const matchesHoming = !showHoming || move.homing;

      return matchesSearch && matchesCharacter && matchesHitLevel && 
             matchesHeatEngage && matchesPowerCrush && matchesThrows && matchesHoming;
    });

    // Sort moves
    filtered.sort((a, b) => {
      switch (sortBy) {
        case 'command':
          return a.command.localeCompare(b.command);
        case 'startup':
          const aStartup = parseInt(a.startUpFrame || '999');
          const bStartup = parseInt(b.startUpFrame || '999');
          return aStartup - bStartup;
        case 'block':
          const aBlock = parseInt(a.blockFrame || '999');
          const bBlock = parseInt(b.blockFrame || '999');
          return aBlock - bBlock;
        case 'damage':
          const aDamage = parseInt(a.damage || '0');
          const bDamage = parseInt(b.damage || '0');
          return bDamage - aDamage;
        default:
          return 0;
      }
    });

    setFilteredMoves(filtered);
  };

  const getUniqueCharacters = () => {
    return [...new Set(moves.map(move => move.characterName))].sort();
  };

  const getUniqueHitLevels = () => {
    return [...new Set(moves.map(move => move.hitLevel).filter(Boolean))].sort();
  };

  const getFrameColor = (frame: string) => {
    const num = parseInt(frame);
    if (isNaN(num)) return '';
    if (num <= -10) return 'frame-very-plus';
    if (num <= -5) return 'frame-plus';
    if (num >= 10) return 'frame-very-minus';
    if (num >= 5) return 'frame-minus';
    return 'frame-neutral';
  };

  if (loading) {
    return (
      <div className={styles.move-page}>
        <div className={styles.loading-container}>
          <div className={styles.loading-spinner}></div>
          <p>Loading moves...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.move-page}>
        <div className={styles.error-container}>
          <p className={styles.error-message}>{error}</p>
          <button onClick={fetchMoves} className={styles.btn btn-primary}>
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.move-page}>
      <div className={styles.page-header}>
        <h1>Tekken Moves</h1>
        <p>Comprehensive frame data for all character moves</p>
      </div>

      <div className={styles.controls-section}>
        <div className={styles.search-container}>
          <Search size={20} className={styles.search-icon} />
          <input
            type="text"
            placeholder="Search moves, characters, or notes..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className={styles.search-input}
          />
        </div>

        <div className={styles.filters-container}>
          <div className={styles.filter-group}>
            <label>Character:</label>
            <select
              value={selectedCharacter}
              onChange={(e) => setSelectedCharacter(e.target.value)}
              className={styles.filter-select}
            >
              <option value="">All Characters</option>
              {getUniqueCharacters().map(character => (
                <option key={character} value={character}>{character}</option>
              ))}
            </select>
          </div>

          <div className={styles.filter-group}>
            <label>Hit Level:</label>
            <select
              value={selectedHitLevel}
              onChange={(e) => setSelectedHitLevel(e.target.value)}
              className={styles.filter-select}
            >
              <option value="">All Hit Levels</option>
              {getUniqueHitLevels().map(hitLevel => (
                <option key={hitLevel} value={hitLevel}>{hitLevel}</option>
              ))}
            </select>
          </div>

          <div className={styles.filter-group}>
            <label>Sort by:</label>
            <select
              value={sortBy}
              onChange={(e) => setSortBy(e.target.value as 'command' | 'startup' | 'block' | 'damage')}
              className={styles.filter-select}
            >
              <option value="command">Command</option>
              <option value="startup">Startup Frames</option>
              <option value="block">Block Frames</option>
              <option value="damage">Damage</option>
            </select>
          </div>
        </div>

        <div className={styles.special-filters}>
          <label className={styles.checkbox-label}>
            <input
              type="checkbox"
              checked={showHeatEngage}
              onChange={(e) => setShowHeatEngage(e.target.checked)}
            />
            <Flame size={16} />
            Heat Engage Only
          </label>

          <label className={styles.checkbox-label}>
            <input
              type="checkbox"
              checked={showPowerCrush}
              onChange={(e) => setShowPowerCrush(e.target.checked)}
            />
            <Shield size={16} />
            Power Crush Only
          </label>

          <label className={styles.checkbox-label}>
            <input
              type="checkbox"
              checked={showThrows}
              onChange={(e) => setShowThrows(e.target.checked)}
            />
            <Target size={16} />
            Throws Only
          </label>

          <label className={styles.checkbox-label}>
            <input
              type="checkbox"
              checked={showHoming}
              onChange={(e) => setShowHoming(e.target.checked)}
            />
            <Eye size={16} />
            Homing Only
          </label>
        </div>
      </div>

      <div className={styles.stats-bar}>
        <div className={styles.stat-item}>
          <Zap size={16} />
          <span>{moves.length} Total Moves</span>
        </div>
        <div className={styles.stat-item}>
          <Target size={16} />
          <span>{getUniqueCharacters().length} Characters</span>
        </div>
        <div className={styles.stat-item}>
          <span>Showing: {filteredMoves.length} moves</span>
        </div>
      </div>

      <div className={styles.moves-table-container}>
        <table className={styles.moves-table}>
          <thead>
            <tr>
              <th>Character</th>
              <th>Command</th>
              <th>Hit Level</th>
              <th>Damage</th>
              <th>Startup</th>
              <th>Block</th>
              <th>Hit</th>
              <th>CH</th>
              <th>Properties</th>
              <th>Notes</th>
            </tr>
          </thead>
          <tbody>
            {filteredMoves.map((move, index) => (
              <tr key={`${move.characterName}-${move.command}-${index}`}>
                <td className={styles.character-name}>{move.characterName}</td>
                <td className={styles.command}>
                  <code>{move.command}</code>
                  {move.stanceCode && (
                    <span className={styles.stance-badge}>{move.stanceCode}</span>
                  )}
                </td>
                <td className={styles.hit-level}>{move.hitLevel || '-'}</td>
                <td className={styles.damage}>{move.damage || '-'}</td>
                <td className={styles.startup}>{move.startUpFrame || '-'}</td>
                <td className={`block ${getFrameColor(move.blockFrame || '')}`}>
                  {move.blockFrame || '-'}
                </td>
                <td className={`hit ${getFrameColor(move.hitFrame || '')}`}>
                  {move.hitFrame || '-'}
                </td>
                <td className={`counter-hit ${getFrameColor(move.counterHitFrame || '')}`}>
                  {move.counterHitFrame || '-'}
                </td>
                <td className={styles.properties}>
                  <div className={styles.property-badges}>
                    {move.heatEngage && <span className={styles.badge heat-engage}>HE</span>}
                    {move.heatSmash && <span className={styles.badge heat-smash}>HS</span>}
                    {move.powerCrush && <span className={styles.badge power-crush}>PC</span>}
                    {move.throw && <span className={styles.badge throw}>T</span>}
                    {move.homing && <span className={styles.badge homing}>H</span>}
                    {move.tornado && <span className={styles.badge tornado}>TD</span>}
                    {move.heatBurst && <span className={styles.badge heat-burst}>HB</span>}
                    {move.requiresHeat && <span className={styles.badge requires-heat}>RH</span>}
                  </div>
                </td>
                <td className={styles.notes}>
                  {move.notes && (
                    <span className={styles.notes-text} title={move.notes}>
                      {move.notes.length > 50 ? `${move.notes.substring(0, 50)}...` : move.notes}
                    </span>
                  )}
                  {move.isUserChanged && <span className={styles.user-changed-badge}>UC</span>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {filteredMoves.length === 0 && (
        <div className={styles.no-results}>
          <p>No moves found matching your search criteria.</p>
          <button 
            onClick={() => {
              setSearchTerm('');
              setSelectedCharacter('');
              setSelectedHitLevel('');
              setShowHeatEngage(false);
              setShowPowerCrush(false);
              setShowThrows(false);
              setShowHoming(false);
            }} 
            className={styles.btn btn-secondary}
          >
            Clear Filters
          </button>
        </div>
      )}
    </div>
  );
};

export default MovePage; 

