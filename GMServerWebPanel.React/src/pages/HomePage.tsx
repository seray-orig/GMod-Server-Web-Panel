import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';

function HomePage() {
    const { logout } = useAuth();
    const navigate = useNavigate();

    const handleLogout = () => {
        logout();
        navigate('/login', { replace: true });
    };

    return (
        <div>
            <h1>Домашняя страница</h1>
            <p>Добро пожаловать! Вы успешно авторизованы.</p>
            <button onClick={handleLogout}>Выйти</button>
        </div>
    );
}

export default HomePage;