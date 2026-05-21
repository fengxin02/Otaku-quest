import React, { useState, useEffect, useCallback } from 'react';
import './BossFight.css';
import { BossService, PlayerProfileService, type CurrentBossResponseDto, type PlayerStatsDto } from '../api/generated';
import { AllAssets, AvatarAssets, BackgroundAssets } from '../assets';

interface BossFightProps {
    onBack: () => void;
    pstats: PlayerStatsDto | null;
    refreshStats: () => void;
}

const BossFight: React.FC<BossFightProps> = ({ onBack, pstats, refreshStats }) => {
    const [bossData, setBossData] = useState<CurrentBossResponseDto | null>(null);
    const [combatLog, setCombatLog] = useState<string[]>(["Fight started! Prepare for battle!"]);
    const [isAttacking, setIsAttacking] = useState(false);
    const [stats, setStats] = useState<PlayerStatsDto | null>(pstats);

    const handleBack = () => {
        onBack();
        refreshStats();
    };

    const fetchCombatData = useCallback(async () => {
        try {
            const bossRes = await BossService.getApiBossCurrent();
            setBossData(bossRes);
            const statsRes = await PlayerProfileService.getApiPlayerProfileMyStats();
            setStats(statsRes);
        } catch (error) {
            console.error("Error during combat data fetch:", error);
        }
    }, []);

   useEffect(() => {
        const initFight = async () => {
            await fetchCombatData();
        };
        initFight();
    }, [fetchCombatData]);

    // Attack logic
    const handleAttack = async () => {
        if (isAttacking || !stats || !bossData) return;
        setIsAttacking(true);

        try {
            const result = await BossService.postApiBossAttack();
            
            // Update combat log
            setCombatLog(prev => [result.message || "Attack landed!", ...prev]);

            await fetchCombatData();

            // Delay to allow animations to play before allowing next attack
            setTimeout(() => setIsAttacking(false), 1000);

        } catch (error: unknown) {
            console.error("Error during the attack:", error);
            // If the player has low HP or another combat error happens, log the backend message.
            const errorMessage =
                typeof error === 'object' &&
                error !== null &&
                'body' in error &&
                typeof (error as { body?: unknown }).body === 'string'
                    ? (error as { body: string }).body
                    : "Disconnected or unknown error during attack.";
            setCombatLog(prev => [errorMessage, ...prev]);
            setIsAttacking(false);
        }
    };

    if (!stats || !bossData) {
        return <div className="boss-loading">Loading battle...</div>;
    }

    const boss = bossData.boss;
    const bossCurrentHp = bossData.currentHp ?? 0;

    if (!boss) {
        return <div className="boss-loading">Can't find the boss!</div>;
    }

    // Calculate percentages for the HP bars.
    const playerHpPercent = Math.max(0, (stats.currentHP / stats.maxHP) * 100);
    const bossHpPercent = Math.max(0, (bossCurrentHp / boss.maxHp) * 100);

    return (
        <div className="boss-fight-wrapper" style={{ 
            backgroundImage: `url(${BackgroundAssets[stats.backgroundImage || 'Default']})`, 
        }}>
            <button className="boss-back-btn" onClick={handleBack}>⬅ Escape</button>

            <div className="arena-container">
                
                {/* Left site player */}
                <div className={`fighter-card ${isAttacking ? 'attack-anim-player' : ''}`}>
                    <h2 className="fighter-name">{stats.username}</h2>
                    
                    <div className="hp-bar-container">
                        <div className="hp-bar-fill player-hp" style={{ width: `${playerHpPercent}%` }}></div>
                        <span className="hp-text">{stats.currentHP} / {stats.maxHP}</span>
                    </div>

                    <div className="fighter-image-box">
                        <img 
                            src={AvatarAssets[stats.avatarImage || 'DefaultAvatar']} 
                            alt="Player" 
                            className="fighter-sprite" 
                        />
                        {/* Draw the weapon */}
                        {stats.weaponImage && (
                            <img src={AllAssets[stats.weaponImage]} alt="Weapon" className="fighter-weapon" />
                        )}
                    </div>
                </div>

                {/* Middle: VS and button */}
                <div className="center-action-area">
                    <h1 className="vs-text">VS</h1>
                    <button 
                        className={`attack-btn ${isAttacking ? 'disabled' : ''}`} 
                        onClick={handleAttack}
                        disabled={isAttacking || stats.currentHP <= 0 || bossCurrentHp <= 0}
                    >
                         Attack
                    </button>
                </div>

                {/* Right site: BOSS */}
                <div className={`fighter-card ${isAttacking ? 'take-damage-anim' : ''}`}>
                    <h2 className="fighter-name boss-name">{boss.name}</h2>
                    
                    <div className="hp-bar-container">
                        <div className="hp-bar-fill boss-hp" style={{ width: `${bossHpPercent}%` }}></div>
                        <span className="hp-text">{bossCurrentHp} / {boss.maxHp}</span>
                    </div>

                    <div className="fighter-image-box">
                        <img 
                            src={AllAssets[boss.imageAsset] || AllAssets['Default']} 
                            alt={boss.name} 
                            className="fighter-sprite boss-sprite" 
                        />
                    </div>
                </div>

            </div>

            {/* Battle log */}
            <div className="combat-log">
                <h3>Battle Log</h3>
                <ul>
                    {combatLog.map((log, index) => (
                        <li key={index} className={index === 0 ? 'latest-log' : ''}>{log}</li>
                    ))}
                </ul>
            </div>
        </div>
    );
};

export default BossFight;