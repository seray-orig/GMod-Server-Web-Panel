import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import './LoginPage.css';

function LoginPage() {
    const [login, setLogin] = useState('');
    const [password, setPassword] = useState('');
    const [rememberMe, setRememberMe] = useState(false);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState('');
    const [pageTitle, setPageTitle] = useState('Вход');
    const [welcomeMessage, setWelcomeMessage] = useState('Welcome');
    const [showPassword, setShowPassword] = useState(false);
    const navigate = useNavigate();

    useEffect(() => {
        const fetchTitle = async () => {
            try {
                const response = await fetch('http://localhost:5000/api/titles/login-page');
                if (response.ok) {
                    const data = await response.json();
                    setPageTitle(data.titleH1);
                    setWelcomeMessage(data.titleH3);
                }
            } catch {
                setPageTitle('Вход');
                setWelcomeMessage('Welcome');
            }
        };
        fetchTitle();
    }, []);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsLoading(true);
        setError('');

        try {
            const response = await fetch('http://localhost:5000/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ login, password, rememberMe }),
            });

            if (!response.ok) {
                if (response.status === 401) throw new Error('Неверный логин или пароль');
                throw new Error(`Заполните все поля`);
            }

            const data = await response.json();
            localStorage.setItem('token', data.token);
            navigate('/home', { replace: true });
        } catch (err: unknown) {
            if (err instanceof Error) setError(err.message);
            else setError('Произошла ошибка');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="login-container">
            <div className="login-card">
                <h1 className="login-title">{pageTitle}</h1>
                <h3 className="login-welcome">{welcomeMessage}</h3>
                <form onSubmit={handleSubmit}>
                    <div className="input-group">
                        <label htmlFor="login">Логин</label>
                        <input
                            type="text"
                            id="login"
                            value={login}
                            onChange={(e) => setLogin(e.target.value)}
                            maxLength={30}
                        />
                    </div>

                    <div className="input-group">
                        <label htmlFor="password">Пароль</label>
                        <div className="password-wrapper">
                            <input
                                type={showPassword ? 'text' : 'password'}
                                id="password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                maxLength={64}
                            />
                            <button
                                type="button"
                                className="toggle-password"
                                onClick={() => setShowPassword(!showPassword)}
                                aria-label={showPassword ? 'Скрыть пароль' : 'Показать пароль'}
                            >
                                {showPassword ? '!' : '?'}
                            </button>
                        </div>
                    </div>

                    <div className="checkbox-group">
                        <input
                            type="checkbox"
                            id="remember"
                            checked={rememberMe}
                            onChange={(e) => setRememberMe(e.target.checked)}
                        />
                        <label htmlFor="remember">Запомнить меня</label>
                    </div>

                    <button type="submit" className="login-button" disabled={isLoading}>
                        {isLoading ? 'Вход...' : 'Войти'}
                    </button>

                    <div className="error-message">{error || '\u00A0'}</div>
                </form>
            </div>
        </div>
    );
}

export default LoginPage;