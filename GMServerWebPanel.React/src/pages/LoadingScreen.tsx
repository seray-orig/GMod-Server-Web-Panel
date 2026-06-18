import { useState, useEffect } from 'react';
import backgroundImage from './Strike Force Heroes.png'; // Замените bg.jpg на имя вашего файла

export default function LoadingScreen() {
    const [dots, setDots] = useState('');

    useEffect(() => {
        const interval = setInterval(() => {
            setDots((prev) => (prev.length >= 3 ? '' : prev + '.'));
        }, 500);

        return () => clearInterval(interval);
    }, []);

    return (
        <div style={styles.container}>
            <div style={styles.textWrapper}>
                <span>Сервер в разработке</span>
                <span style={styles.dots}>{dots}</span>
            </div>
        </div>
    );
}

const styles = {
    container: {
        position: 'fixed' as const,
        top: 0,
        left: 0,
        width: '100vw',
        height: '100vh',
        backgroundImage: `url(${backgroundImage})`,
        backgroundSize: 'cover',
        backgroundPosition: 'center',
        backgroundRepeat: 'no-repeat',
        zIndex: 9999,
    },
    textWrapper: {
        position: 'absolute' as const,
        bottom: '40px',
        left: '50%',
        transform: 'translateX(-50%)',
        display: 'flex', // Выстраивает текст и точки в одну линию
        alignItems: 'baseline',
        color: '#ffffff',
        fontSize: '24px',
        fontFamily: 'sans-serif',
        fontWeight: 'bold' as const,
        textShadow: '2px 2px 4px rgba(0, 0, 0, 0.8)',
    },
    dots: {
        position: 'absolute' as const,
        left: '100%', // Размещает точки сразу после окончания текста
        width: '30px', // Резервирует место под 3 точки, чтобы они не растягивали блок
        textAlign: 'left' as const,
    },
};
