import React, { useState, useEffect } from 'react';
import { ApiError, TodoService, type DifficultyRank, type TodoTask } from '../api/generated'; 
import './QuestBoard.css';

interface QuestBoardProps {
    refreshStats: () => void;
    showCompletedTasks: boolean
}


 const borderLeftColor: Record<number, string> = {
    0: "oklch(0.58 0.04 255)",
    1: "oklch(0.62 0.14 155)",
    2: "oklch(0.62 0.14 245)",
    3: "oklch(0.58 0.18 292)",
    4: "oklch(0.72 0.15 75)",
    5: "oklch(0.58 0.2 25)"

};
const QuestBoard: React.FC<QuestBoardProps> = ({ refreshStats, showCompletedTasks }) => {
    const [quests, setQuests] = useState<TodoTask[]>([]);

    const [loading, setLoading] = useState(true);
    const [expandedQuestId, setExpandedQuestId] = useState<number | null>(null);
    const [isAdding, setIsAdding] = useState(false);
    const [newTaskTitle, setNewTaskTitle] = useState('');
    const [description, setDescription] = useState('');
    const [type, setType] = useState<TodoTask['type']>(0);
    const [difficultyRank, setDifficultyRank] = useState<DifficultyRank>(0);
    const [isRepeatable, setIsRepeatable] = useState(false);
    const [finishingQuestId, setFinishingQuestId] = useState<number | null>(null);
    const [finishError, setFinishError] = useState<string | null>(null);

    const fetchQuests = async () => {
        try {
            const response = await TodoService.getApiTodo(); 
            setQuests(response);
        } catch (error) {
            console.error("Can't fetch quests", error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchQuests();
    }, []);

    const handleCreateQuest = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!newTaskTitle.trim()) 
        {
            return;
        }

        try {
            await TodoService.postApiTodo({
                title: newTaskTitle,
                description: description,
                type: type,
                isRepeatable: isRepeatable,
                difficultyRank: difficultyRank
            });
            
            setNewTaskTitle(''); 
            setDescription('');
            setType(0);
            setDifficultyRank(0);
            setIsAdding(false);  
            setIsRepeatable(false);
            fetchQuests();       // fetching the quests again to show the new one
        } catch (error) {
            console.error("Problem creating quest", error);
        }
    };

    const handleCompleteQuest = async (id: number) => {
        try {
            await TodoService.postApiTodoComplete(id); 
            
            fetchQuests();  
            refreshStats(); // refresh the stats to reflect the completed quest's rewards
        } catch (error) {
            console.error("Problem completing quest", error);
        }
    };

    const toggleQuestDetails = (id: number) => {
        if(expandedQuestId === id) {
            setExpandedQuestId(null);
        }else {
            setExpandedQuestId(id);
        }
    }
    //convert type number to string for display
    const getTaskTypeName = (typeNum: number) => {
        const types = ['Study', 'Workout', 'Hobby', 'Social', 'Health'];
        return types[typeNum] || 'Unknown';
    }
    //convert difficulty rank number to string for display
    const getDifficultyName = (rankNum: number) => {
        const ranks = ['E Rank', 'D Rank', 'C Rank', 'B Rank', 'A Rank', 'S Rank'];
        return ranks[rankNum] || 'Unknown';
    }

    const getTaskTypeClass = (typeNum: number) => {
        const classes = ['type-study', 'type-workout', 'type-hobby', 'type-social', 'type-health'];
        return classes[typeNum] || 'type-unknown';
    };

    const getDifficultyClass = (rankNum: number) => {
        const classes = ['difficulty-e', 'difficulty-d', 'difficulty-c', 'difficulty-b', 'difficulty-a', 'difficulty-s'];
        return classes[rankNum] || 'difficulty-unknown';
    };

    const formatCompletedAt = (value?: string | null) => {
        if (!value) return 'Not recorded';

        const completedAt = new Date(value);
        if (Number.isNaN(completedAt.getTime()) || completedAt.getFullYear() < 2000) {
            return 'Not recorded';
        }

        return new Intl.DateTimeFormat(undefined, {
            dateStyle: 'medium',
            timeStyle: 'short'
        }).format(completedAt);
    };

    const handleFinishQuest = async (id: number) => {
        if (finishingQuestId !== null) return;

        setFinishingQuestId(id);
        setFinishError(null);

        try {
            await TodoService.postApiTodoFinish(id);

            // Update immediately so a successful finish never looks like a dead click.
            setQuests(currentQuests => currentQuests.map(quest =>
                quest.id === id
                    ? { ...quest, status: 2 as const, lastCompletedAt: new Date().toISOString() }
                    : quest
            ));
        } catch (error) {
            console.error("Problem finishing quest", error);

            if (error instanceof ApiError) {
                const message = typeof error.body === 'string'
                    ? error.body
                    : error.body?.message ?? error.body?.title;

                setFinishError(message || `Could not finish the quest (HTTP ${error.status}).`);
            } else {
                setFinishError('Could not reach the server. Check that the API is running and try again.');
            }
        } finally {
            setFinishingQuestId(null);
        }
    };

    if (loading) return <div className="quest-loading">Searching for quests... </div>;

    return (
        <div className="quest-board-container">
            {finishError && (
                <div className="quest-action-error" role="alert">
                    {finishError}
                </div>
            )}
             
            <div className="quest-list">
                {quests.length === 0 ? (
                    <p className="no-quests-msg">No active quests available. Create one!</p>
                ) : (
                    quests.map((quest) => (
                        (showCompletedTasks ? quest.status === 2 : quest.status === 1) && (
                            <React.Fragment key={quest.id}>
                            <div  
                            className={`quest-card ${expandedQuestId === quest.id ? 'expanded' : ''}`} 
                            style ={{borderLeft: `6px solid ${borderLeftColor[quest.difficultyRank]}`}}

                            onClick={() => toggleQuestDetails(quest.id)}>
                                <h4 className="quest-title">{quest.title}</h4>
                                {showCompletedTasks ? <span></span> :
                                <div className="quest-actions">
                                <button 
                                    className="complete-quest-btn" 
                                    onClick={(e) =>{
                                        e.stopPropagation(); 
                                        handleCompleteQuest(quest.id);}}
                                >
                                    {quest.isRepeatable ? '✓ Complete Once' : '✓ Complete'}
                                </button>
                                {quest.isRepeatable && (
                                    <button
                                        type="button"
                                        className="finish-quest-btn"
                                        disabled={finishingQuestId === quest.id}
                                        onClick={(e) => {
                                            e.stopPropagation();
                                            handleFinishQuest(quest.id);
                                        }}
                                    >
                                        {finishingQuestId === quest.id ? 'Finishing...' : 'Finish Quest'}
                                    </button>
                                )}
                                </div>
                                }
                            </div>
                            {expandedQuestId === quest.id && (
                                    <div className="quest-details">
                                        <p><strong>Description:</strong> {quest.description || ''}</p>
                                        <div className="quest-meta">
                                            <span className={`quest-badge type-badge ${getTaskTypeClass(quest.type)}`}>
                                                Type: {getTaskTypeName(quest.type)}
                                            </span>
                                            <span className={`quest-badge difficulty-badge ${getDifficultyClass(quest.difficultyRank)}`}>
                                                Difficulty: {getDifficultyName(quest.difficultyRank)}
                                            </span>
                                            {quest.isRepeatable && <span className="quest-badge repeatable-badge">Repeatable</span>}
                                        </div>
                                        {showCompletedTasks && (
                                            quest.isRepeatable ? (
                                                <div className="quest-completion-summary">
                                                    <div className="completion-stat">
                                                        <span className="completion-stat-icon" aria-hidden="true">✓</span>
                                                        <span className="completion-stat-copy">
                                                            <strong>{quest.completionCount}</strong>
                                                            <small>{quest.completionCount === 1 ? 'completion' : 'completions'}</small>
                                                        </span>
                                                    </div>
                                                    <div className="completion-stat completion-time">
                                                        <span className="completion-stat-icon" aria-hidden="true">◷</span>
                                                        <span className="completion-stat-copy">
                                                            <strong>{formatCompletedAt(quest.lastCompletedAt)}</strong>
                                                            <small>completed at</small>
                                                        </span>
                                                    </div>
                                                </div>
                                            ) : (
                                                <div className="quest-completion-summary">
                                                <div className="completion-stat quest-completed-state">
                                                    <span className="completion-stat-icon" aria-hidden="true">✓</span>
                                                    <span className="completion-stat-copy">
                                                        <strong>Completed</strong>
                                                        <small className="completion-spacer" aria-hidden="true">status</small>
                                                    </span>
                                                </div>
                                                 <div className="completion-stat completion-time">
                                                        <span className="completion-stat-icon" aria-hidden="true">◷</span>
                                                        <span className="completion-stat-copy">
                                                            <strong>{formatCompletedAt(quest.lastCompletedAt)}</strong>
                                                            <small>completed at</small>
                                                        </span>
                                                    </div>
                                                </div>
                                            )
                                        )}
                                    </div>
                                )}
                            </React.Fragment>
                    )
                        
                    ))
                )}
            </div>

            {isAdding ? (
                <form className="add-quest-form" onSubmit={handleCreateQuest}>
                    <label className='input-label'>Title</label>
                    <input 
                        type="text" 
                        className="add-quest-input"
                        placeholder="What is your new Challenge? ( >д<) "
                        value={newTaskTitle}
                        onChange={(e) => setNewTaskTitle(e.target.value)}
                        autoFocus
                        required
                    />
                    <div className="input-group">
                        <label className="input-label">Description</label>
                        <textarea 

                            className="add-quest-input textarea"
                            placeholder="Give me those deetailsssss!"
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                        />
                    </div>

                    <div className="select-row">
                        <div className="input-group">
                            <label className="input-label">Type</label>
                            <select 
                                className="add-quest-select"
                                value={type}
                                onChange={(e) => setType(Number(e.target.value) as TodoTask['type'])}
                            >
                                <option value={0}>Study</option>
                                <option value={1}>Workout</option>
                                <option value={2}>Hobby</option>
                                <option value={3}>Social</option>
                                <option value={4}>Health</option>
                            </select>
                        </div>

                        <div className="input-group">
                            <label className="input-label">Difficulty</label>
                            <select 
                                className="add-quest-select"
                                value={difficultyRank}
                                onChange={(e) => setDifficultyRank(Number(e.target.value) as DifficultyRank)}
                            >
                                <option value={0}>E Rank</option>
                                <option value={1}>D Rank</option>
                                <option value={2}>C Rank</option>
                                <option value={3}>B Rank</option>
                                <option value={4}>A Rank</option>
                                <option value={5}>S Rank</option>
                            </select>
                        </div>
                    </div>
                        <label className={`repeatable-option ${isRepeatable ? 'is-selected' : ''}`}>
                            <input
                                type="checkbox"
                                className="repeatable-checkbox"
                                checked={isRepeatable}
                                onChange={(e) => setIsRepeatable(e.target.checked)}
                                aria-describedby="repeatable-description"
                            />
                            <span className="repeatable-copy">
                                <strong className="repeatable-title">
                                    <span className="repeatable-icon" aria-hidden="true">↻</span>
                                    Repeatable quest
                                </strong>
                                <small id="repeatable-description" className="repeatable-description">
                                    Complete it many times for rewards, then finish it when the habit is done.
                                </small>
                            </span>
                        </label>
                    <div className="form-buttons">
                        <button type="submit" className="save-quest-btn">Save</button>
                        <button type="button" className="cancel-quest-btn" onClick={() => setIsAdding(false)}>Cancel</button>
                    </div>
                </form>
            ) : (
                <button className="add-todo-btn" onClick={() => setIsAdding(true)}>
                    + Add New Quest
                </button>
            )}
            
        </div>
    );
};

export default QuestBoard;
