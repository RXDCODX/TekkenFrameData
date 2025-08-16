import React, { useState, useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import axios from 'axios';
import { 
  Eye, 
  EyeOff, 
  Mail, 
  Lock, 
  User, 
  AlertCircle,
  CheckCircle,
} from 'lucide-react';
import styles from './LoginPage.module.scss';

interface LoginForm {
  email: string;
  password: string;
}

interface RegisterForm {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
}

const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [isLogin, setIsLogin] = useState(true);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const [loginForm, setLoginForm] = useState<LoginForm>({
    email: '',
    password: ''
  });

  const [registerForm, setRegisterForm] = useState<RegisterForm>({
    email: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: ''
  });

  useEffect(() => {
    // Check if user is already logged in
    const token = localStorage.getItem('token');
    if (token) {
      navigate('/');
      return;
    }

    // Check for OAuth callback
    const tokenParam = searchParams.get('token');
    const refreshTokenParam = searchParams.get('refreshToken');
    
    if (tokenParam && refreshTokenParam) {
      localStorage.setItem('token', tokenParam);
      localStorage.setItem('refreshToken', refreshTokenParam);
      setSuccess('Successfully logged in!');
      setTimeout(() => {
        navigate('/');
      }, 1500);
    }
  }, [navigate, searchParams]);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const response = await axios.post('/api/v1/auth/login', loginForm);
      const { token, refreshToken } = response.data;
      
      localStorage.setItem('token', token);
      localStorage.setItem('refreshToken', refreshToken);
      
      setSuccess('Successfully logged in!');
      setTimeout(() => {
        navigate('/');
      }, 1500);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Login failed');
    } finally {
      setLoading(false);
    }
  };

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    if (registerForm.password !== registerForm.confirmPassword) {
      setError('Passwords do not match');
      setLoading(false);
      return;
    }

    try {
      const response = await axios.post('/api/v1/auth/register', {
        email: registerForm.email,
        password: registerForm.password,
        firstName: registerForm.firstName,
        lastName: registerForm.lastName
      });
      
      const { token, refreshToken } = response.data;
      
      localStorage.setItem('token', token);
      localStorage.setItem('refreshToken', refreshToken);
      
      setSuccess('Account created successfully!');
      setTimeout(() => {
        navigate('/');
      }, 1500);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Registration failed');
    } finally {
      setLoading(false);
    }
  };

  const handleOAuthLogin = async (provider: 'twitch' | 'google') => {
    try {
      const response = await axios.get(`/api/v1/auth/${provider}-login`);
      window.location.href = response.data.authUrl;
    } catch (err) {
      setError(`Failed to initiate ${provider} login`);
    }
  };

  const handleInputChange = (form: 'login' | 'register', field: string, value: string) => {
    if (form === 'login') {
      setLoginForm(prev => ({ ...prev, [field]: value }));
    } else {
      setRegisterForm(prev => ({ ...prev, [field]: value }));
    }
  };

  return (
    <div className={styles.login-page}>
      <div className={styles.login-container}>
        <div className={styles.login-header}>
          <h1>Welcome to Tekken Frame Data</h1>
          <p>{isLogin ? 'Sign in to your account' : 'Create a new account'}</p>
        </div>

        {error && (
          <div className={styles.error-message}>
            <AlertCircle size={16} />
            {error}
            <button onClick={() => setError(null)}>×</button>
          </div>
        )}

        {success && (
          <div className={styles.success-message}>
            <CheckCircle size={16} />
            {success}
          </div>
        )}

        <div className={styles.oauth-section}>
          <button 
            className={styles.oauth-btn twitch-btn}
            onClick={() => handleOAuthLogin('twitch')}
            disabled={loading}
          >
            <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
              <path d="M11.571 4.714h1.715v5.143H11.57zm4.715 0H18v5.143h-1.714zM6 0L1.714 4.286v15.428h5.143V24l4.286-4.286h3.428L22.286 12V0zm14.571 11.143l-3.428 3.428h-3.429l-3 3v-3H6.857V1.714h13.714Z"/>
            </svg>
            Continue with Twitch
          </button>
          
          <button 
            className={styles.oauth-btn google-btn}
            onClick={() => handleOAuthLogin('google')}
            disabled={loading}
          >
            <svg width="20" height="20" viewBox="0 0 24 24">
              <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
              <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
              <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
              <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
            </svg>
            Continue with Google
          </button>
        </div>

        <div className={styles.divider}>
          <span>or</span>
        </div>

        <form onSubmit={isLogin ? handleLogin : handleRegister} className={styles.auth-form}>
          {!isLogin && (
            <div className={styles.form-row}>
              <div className={styles.form-group}>
                <label>First Name</label>
                <div className={styles.input-wrapper}>
                  <User size={16} />
                  <input
                    type="text"
                    value={registerForm.firstName}
                    onChange={(e) => handleInputChange('register', 'firstName', e.target.value)}
                    required
                    placeholder="Enter your first name"
                  />
                </div>
              </div>
              <div className={styles.form-group}>
                <label>Last Name</label>
                <div className={styles.input-wrapper}>
                  <User size={16} />
                  <input
                    type="text"
                    value={registerForm.lastName}
                    onChange={(e) => handleInputChange('register', 'lastName', e.target.value)}
                    required
                    placeholder="Enter your last name"
                  />
                </div>
              </div>
            </div>
          )}

          <div className={styles.form-group}>
            <label>Email</label>
            <div className={styles.input-wrapper}>
              <Mail size={16} />
              <input
                type="email"
                value={isLogin ? loginForm.email : registerForm.email}
                onChange={(e) => handleInputChange(isLogin ? 'login' : 'register', 'email', e.target.value)}
                required
                placeholder="Enter your email"
              />
            </div>
          </div>

          <div className={styles.form-group}>
            <label>Password</label>
            <div className={styles.input-wrapper}>
              <Lock size={16} />
              <input
                type={showPassword ? 'text' : 'password'}
                value={isLogin ? loginForm.password : registerForm.password}
                onChange={(e) => handleInputChange(isLogin ? 'login' : 'register', 'password', e.target.value)}
                required
                placeholder="Enter your password"
              />
              <button
                type="button"
                className={styles.password-toggle}
                onClick={() => setShowPassword(!showPassword)}
              >
                {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
          </div>

          {!isLogin && (
            <div className={styles.form-group}>
              <label>Confirm Password</label>
              <div className={styles.input-wrapper}>
                <Lock size={16} />
                <input
                  type={showConfirmPassword ? 'text' : 'password'}
                  value={registerForm.confirmPassword}
                  onChange={(e) => handleInputChange('register', 'confirmPassword', e.target.value)}
                  required
                  placeholder="Confirm your password"
                />
                <button
                  type="button"
                  className={styles.password-toggle}
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                >
                  {showConfirmPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              </div>
            </div>
          )}

          <button 
            type="submit" 
            className={styles.submit-btn}
            disabled={loading}
          >
            {loading ? 'Loading...' : (isLogin ? 'Sign In' : 'Create Account')}
          </button>
        </form>

        <div className={styles.auth-footer}>
          <p>
            {isLogin ? "Don't have an account? " : "Already have an account? "}
            <button 
              className={styles.toggle-btn}
              onClick={() => {
                setIsLogin(!isLogin);
                setError(null);
                setSuccess(null);
              }}
            >
              {isLogin ? 'Sign up' : 'Sign in'}
            </button>
          </p>
        </div>
      </div>
    </div>
  );
};

export default LoginPage; 

