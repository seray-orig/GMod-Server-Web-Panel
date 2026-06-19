import { useState, useEffect, useCallback, useRef } from 'react';
import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import './HomePage.css';

function HomePage() {
    const { logout } = useAuth();
    const navigate = useNavigate();
    const [logs, setLogs] = useState<string[]>([]);
    const [command, setCommand] = useState('');
    const [isSending, setIsSending] = useState(false);
    const logWindowRef = useRef<HTMLDivElement>(null); // 👈 Добавляем ref

    const token = localStorage.getItem('token');

    // Автопрокрутка при изменении логов
    useEffect(() => {
        if (logWindowRef.current) {
            logWindowRef.current.scrollTop = logWindowRef.current.scrollHeight;
        }
    }, [logs]);

    const handleLogout = () => {
        logout();
        navigate('/login', { replace: true });
    };

    const apiFetch = useCallback(async (url: string, options: RequestInit = {}) => {
        const response = await fetch(url, {
            ...options,
            headers: {
                'Content-Type': 'application/json',
                ...(token ? { Authorization: `Bearer ${token}` } : {}),
            },
        });
        if (response.status === 401) {
            logout();
            navigate('/login', { replace: true });
            throw new Error('Unauthorized');
        }
        return response;
    }, [token, logout, navigate]);

    useEffect(() => {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/hub/logs', {
                accessTokenFactory: () => token || ""
            })
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveLog", (newLine: string) => {
            setLogs((prevLogs) => [...prevLogs, newLine]);
        });

        connection.start()
            .then(() => console.log("SignalR Connected!"))
            .catch(err => console.error("SignalR Connection Error:", err));

        return () => {
            connection.off("ReceiveLog");
            connection.stop();
        };
    }, [token]);

    const handleSendCommand = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!command.trim()) return;
        setIsSending(true);
        try {
            await apiFetch('/api/server/command', {
                method: 'POST',
                body: JSON.stringify({ command }),
            });
            setCommand('');
        } catch (err) {
            console.error(err);
        } finally {
            setIsSending(false);
        }
    };

    const handleStart = () => apiFetch('/api/server/start', { method: 'POST' });
    const handleStop = () => apiFetch('/api/server/stop', { method: 'POST' });
    const handleUpdate = () => apiFetch('/api/server/update', { method: 'POST' });

    return (
        <div className="home-container">
            <div className="home-panel">
                <div className="home-header">
                    <h1 className="home-title">Управление сервером</h1>
                    <button className="logout-btn" onClick={handleLogout}>Выйти</button>
                </div>

                <div className="log-window" ref={logWindowRef}> {/* 👈 Добавляем ref */}
                    {logs.length === 0 ? (
                        <div className="log-empty">Логи пока пусты...</div>
                    ) : (
                        logs.map((line, idx) => (
                            <div key={idx} className="log-line">{line}</div>
                        ))
                    )}
                </div>

                <form className="command-form" onSubmit={handleSendCommand}>
                    <input
                        type="text"
                        className="command-input"
                        placeholder="Введите команду..."
                        value={command}
                        onChange={(e) => setCommand(e.target.value)}
                    />
                    <button type="submit" className="command-btn" disabled={isSending}>
                        Отправить
                    </button>
                </form>

                <div className="control-buttons">
                    <button className="control-btn start" onClick={handleStart}>Запуск</button>
                    <button className="control-btn stop" onClick={handleStop}>Выключить</button>
                    <button className="control-btn update" onClick={handleUpdate}>Обновить</button>
                </div>
            </div>
        </div>
    );
}

export default HomePage;