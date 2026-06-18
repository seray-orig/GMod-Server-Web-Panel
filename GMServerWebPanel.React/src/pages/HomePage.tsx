import { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';
import './HomePage.css';

function HomePage() {
    const { logout } = useAuth();
    const navigate = useNavigate();

    const [logs, setLogs] = useState<string[]>([]);
    const [command, setCommand] = useState('');
    const [isSending, setIsSending] = useState(false);

    // Токен берём из localStorage, чтобы добавлять к запросам
    const token = localStorage.getItem('token');

    // Функция для выхода
    const handleLogout = () => {
        logout();
        navigate('/login', { replace: true });
    };

    // Обёртка для fetch с авторизацией и обработкой 401
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

    // Получение логов (периодически)
    const fetchLogs = useCallback(async () => {
        try {
            const res = await apiFetch('/api/server/logs');
            if (res.ok) {
                const data = await res.json();
                setLogs(data.logs); // ожидаем { logs: string[] }
            }
        } catch {
            // игнорируем
        }
    }, [apiFetch]);

    // Первоначальная загрузка и интервал опроса
    useEffect(() => {
        fetchLogs();
        const interval = setInterval(fetchLogs, 2000);
        return () => clearInterval(interval);
    }, [fetchLogs]);

    // Отправка произвольной команды
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
            // Подождём немного и обновим логи
            setTimeout(fetchLogs, 300);
        } catch (err) {
            console.error(err);
        } finally {
            setIsSending(false);
        }
    };

    // Управляющие кнопки
    const handleStart = async () => {
        await apiFetch('/api/server/start', { method: 'POST' });
        fetchLogs();
    };
    const handleStop = async () => {
        await apiFetch('/api/server/stop', { method: 'POST' });
        fetchLogs();
    };
    const handleUpdate = async () => {
        await apiFetch('/api/server/update', { method: 'POST' });
        fetchLogs();
    };

    return (
        <div className="home-container">
            <div className="home-panel">
                {/* Заголовок и кнопка выхода */}
                <div className="home-header">
                    <h1 className="home-title">Управление сервером</h1>
                    <button className="logout-btn" onClick={handleLogout}>Выйти</button>
                </div>

                {/* Окно логов */}
                <div className="log-window">
                    {logs.length === 0 ? (
                        <div className="log-empty">Логи пока пусты...</div>
                    ) : (
                        logs.map((line, idx) => (
                            <div key={idx} className="log-line">{line}</div>
                        ))
                    )}
                </div>

                {/* Ввод команды */}
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

                {/* Кнопки управления */}
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