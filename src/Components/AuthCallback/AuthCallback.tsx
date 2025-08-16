import React, { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { CheckCircle, AlertCircle, Loader } from 'lucide-react';
import styles from './AuthCallback.module.scss';

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
    <div className={styles.authCallback}>
      <div className={styles.callbackContainer}>
        <div className={styles.callbackContent}>
          {status === 'loading' && (
            <>
              <Loader className={`${styles.callbackIcon} ${styles.loading}`} size={48} />
              <h2>Authenticating...</h2>
              <p>{message}</p>
            </>
          )}
          
          {status === 'success' && (
            <>
              <CheckCircle className={`${styles.callbackIcon} ${styles.success}`} size={48} />
              <h2>Success!</h2>
              <p>{message}</p>
            </>
          )}
          
          {status === 'error' && (
            <>
              <AlertCircle className={`${styles.callbackIcon} ${styles.error}`} size={48} />
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

