import React, { useEffect, useState } from 'react';
import { Navigate } from 'react-router-dom';

interface ProtectedRouteProps {
    children: React.ReactNode;
}

const ProtectedRoute = ({ children }: ProtectedRouteProps) => {
    const [isValid, setIsValid] = useState<boolean | null>(null);

    useEffect(() => {
        const verifyToken = async () => {
            try {
                const token = localStorage.getItem('token');
                const response = await fetch('/api/auth/verify', {
                    headers: token ? { Authorization: `Bearer ${token}` } : {},
                });

                if (response.ok) {
                    setIsValid(true);
                } else {
                    localStorage.removeItem('token');
                    setIsValid(false);
                }
            } catch {
                localStorage.removeItem('token');
                setIsValid(false);
            }
        };

        verifyToken();
    }, []);

    if (isValid === null) {
        return <div>Проверка авторизации...</div>;
    }

    if (!isValid) {
        return <Navigate to="/login" replace />;
    }

    return <>{children}</>;
};

export default ProtectedRoute;