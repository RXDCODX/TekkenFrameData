import React, { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { CheckCircle, AlertCircle, Loader } from 'lucide-react';
import './AuthCallback.css';

const AuthCallback: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');
  const [message, setMessage] = useState('Processing authentication...');

  useEffect(() => {
    const token = searchParams.get('token');
    const refreshToken = searchParams.get('refreshToken');
    const error = searchParams.get('error');

    if (error) {
      setStatus('error');
      setMessage('Authentication failed. Please try again.');
      setTimeout(() => {
        navigate('/login');
      }, 3000);
      return;
    }

    if (token && refreshToken) {
      localStorage.setItem('token', token);
      localStorage.setItem('refreshToken', refreshToken);
      setStatus('success');
      setMessage('Successfully authenticated! Redirecting...');
      setTimeout(() => {
        navigate('/');
      }, 2000);
    } else {
      setStatus('error');
      setMessage('Invalid authentication response. Please try again.');
      setTimeout(() => {
        navigate('/login');
      }, 3000);
    }
  }, [searchParams, navigate]);

  return (
    <div className="auth-callback">
      <div className="callback-container">
        <div className="callback-content">
          {status === 'loading' && (
            <>
              <Loader className="callback-icon loading" size={48} />
              <h2>Authenticating...</h2>
              <p>{message}</p>
            </>
          )}
          
          {status === 'success' && (
            <>
              <CheckCircle className="callback-icon success" size={48} />
              <h2>Success!</h2>
              <p>{message}</p>
            </>
          )}
          
          {status === 'error' && (
            <>
              <AlertCircle className="callback-icon error" size={48} />
              <h2>Authentication Failed</h2>
              <p>{message}</p>
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default AuthCallback; 