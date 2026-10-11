import React, { useState } from 'react';
import { useSignalR } from '../context/SignalRContext';
import '../App.css';
import type {Response} from '../types';

interface AuthFormProps {
  onAuthSuccess: (username: string) => void;
}

export const AuthForm: React.FC<AuthFormProps> = ({ onAuthSuccess }) => {
  const { authConnection, connectUser } = useSignalR();
  
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [isLogin, setIsLogin] = useState(true);
  const [message, setMessage] = useState('');
  const [isError, setIsError] = useState(false);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!authConnection) return;
    
    setIsError(false);
    setMessage(isLogin ? 'Logging in...' : 'Registering...');

    try {
      if (isLogin) {
        const response = await authConnection.invoke<Response>('Login', username, password);
        
        if (response.success) {
          setIsError(false);
          setMessage(response.message || 'Login successful!');
          
          await connectUser(username);
          onAuthSuccess(username);
        } else {
          setIsError(true);
          setMessage(response.message || 'Login failed. Please check your username and password.');
        }
      } else {
        const response = await authConnection.invoke<Response>('Register', username, password);
        
        if (response.success) {
          setIsError(false);
          setMessage(response.message || 'Registration successful! You can now log in.');
          setIsLogin(true);
        } else {
          setIsError(true);
          setMessage(response.message || 'Username is already taken.');
        }
      }
    } catch (err: any) {
      setIsError(true);
      setMessage(`Error: ${err.message || err}`);
    }
  };

  return (
    <div className="auth-wrapper">
      <div className="auth-box">
        <h3>{isLogin ? 'Login' : 'Register'}</h3>
        <p className="auth-subtitle">
          {isLogin ? 'Enter your credentials to access your account' : 'Create an account to continue'}
        </p>

        <form onSubmit={handleSubmit}>
          <div className="input-group">
            <label htmlFor="username">Username</label>
            <input 
              id="username"
              type="text" 
              value={username} 
              onChange={(e) => setUsername(e.target.value)} 
              required 
              placeholder="Enter your username"
              className="input-field"
            />
          </div>

          <div className="input-group">
            <label htmlFor="password">Password</label>
            <input 
              id="password"
              type="password" 
              value={password} 
              onChange={(e) => setPassword(e.target.value)} 
              required 
              placeholder="••••••••"
              className="input-field"
            />
          </div>

          <button type="submit" className="submit-btn">
            {isLogin ? 'Login' : 'Register'}
          </button>
        </form>

        <p className="toggle-text">
          {isLogin ? 'Don\'t have an account?' : 'Already have an account?'}
          <span 
            onClick={() => { setIsLogin(!isLogin); setMessage(''); }} 
            className="toggle-link"
          >
            {isLogin ? 'Register' : 'Login'}
          </span>
        </p>

        {message && (
          <div className={`status-msg ${isError ? 'error' : 'info'}`}>
            {message}
          </div>
        )}
      </div>
    </div>
  );
};