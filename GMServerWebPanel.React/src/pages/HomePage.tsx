import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

function HomePage() {
    const [message, setMessage] = useState('');
    const navigate = useNavigate();

    useEffect(() => {
        const fetchData = async () => {
            try {
                const token = localStorage.getItem('token');
                const response = await fetch('/api/home/data', {
                    headers: { Authorization: `Bearer ${token}` },
                });

                if (response.status === 401) {
                    localStorage.removeItem('token');
                    navigate('/login', { replace: true });
                    return;
                }

                if (!response.ok) throw new Error('Ошибка загрузки данных');
                const data = await response.json();
                setMessage(data.message);
            } catch (error) {
                console.error(error);
            }
        };

        fetchData();
    }, [navigate]);

    const handleLogout = () => {
        localStorage.removeItem('token');
        navigate('/login', { replace: true });
    };

    return (
        <div>
            <h1>Домашняя страница</h1>
            <p>{message || 'Загрузка...'}</p>
            <button onClick={handleLogout}>Выйти</button>
        </div>
    );
}

export default HomePage;