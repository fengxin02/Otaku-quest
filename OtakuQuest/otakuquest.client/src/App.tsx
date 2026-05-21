import { useEffect, useState } from 'react';
import Auth from './components/Auth';
import Dashboard from './components/Dashboard';
import MainMenu from './components/MainMenu';
import { PlayerProfileService, type PlayerStatsDto } from './api/generated';
import Shop from './components/Shop';
import BossFight from './components/BossFight';

function App() {
    const [playerStats, setPlayerStats] = useState<PlayerStatsDto | null>(null);
    const [isStatsLoading, setIsStatsLoading] = useState<boolean>(true);

    const [currentScreen, setCurrentScreen] = useState<string>('login');

    const fetchStats = async (isSilent: boolean = false) => {
        if (!isSilent){
            setIsStatsLoading(true);
        }
        try {
            const response = await PlayerProfileService.getApiPlayerProfileMyStats();
            setPlayerStats(response);
        } catch (error) {
            console.error("Failed to fetch player stats:", error);
        } finally {
            setIsStatsLoading(false); 
        }
    };

    useEffect( ()=>{

        const token = localStorage.getItem('token');
        if(token) {
            fetchStats();
            if(!playerStats){
                localStorage.removeItem('token');
                setPlayerStats(null);
                setCurrentScreen('login');
            }
            else{
                setCurrentScreen('mainmenu');
            }
        }
    }, []);

    const handleLoginSuccess = () => {
        fetchStats();
        setCurrentScreen('mainmenu');
    };
    
    // Clear token and update authentication state on logout
    const handleLogout = () => {
        localStorage.removeItem('token');
        setPlayerStats(null);
        setCurrentScreen('login');
    };
    
    const renderScreen = () => {
        switch (currentScreen) {
            case 'login':
                return <Auth onLoginSuccess={handleLoginSuccess} />;
            
            case 'mainmenu':
                return <MainMenu 
                          onNavigate={(screenName) => setCurrentScreen(screenName)} 
                          stats= {playerStats}
                          loading={isStatsLoading}
                          refreshStats={() => fetchStats(true)}
                       />;
            
            case 'character':
                return <Dashboard 
                          onBackToMenu={() => setCurrentScreen('mainmenu')} 
                          onLogout={handleLogout} 
                          stats={playerStats}
                          refreshStats={() => fetchStats(true)}
                       />;

            case 'shop':
                return<Shop 
                          onBackToMenu={() => setCurrentScreen('mainmenu')} 
                          stats={playerStats}
                          refreshStats={() => fetchStats(true)}
                       />;

            case 'boss':
                return <BossFight
                          onBack={() => setCurrentScreen('mainmenu')}
                          pstats={playerStats}
                          refreshStats={() => fetchStats(true)}
                       />;
            
            default:
                return <Auth onLoginSuccess={handleLoginSuccess} />;
        }
    };

    return (
        <div>
            {renderScreen()}
        </div>
    );
}

export default App;