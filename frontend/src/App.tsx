import { useState, useEffect } from 'react';
import { useSignalR } from './context/SignalRContext';
import { AuthForm } from './components/AuthForm';
import { HomePage } from './components/HomePage';
import './app.css';

function App() {
  const { isReady, connectUser, serverConnection } = useSignalR();
  const [currentUser, setCurrentUser] = useState<string | null>(localStorage.getItem("chat_username"));
  const [isConnectingHubs, setIsConnectingHubs] = useState(false);

  useEffect(() => {
    if (serverConnection && !currentUser) {
      setCurrentUser(localStorage.getItem("chat_username"));
    }
  }, [serverConnection]);

  const handleAuthSuccess = async (username: string) => {
    setIsConnectingHubs(true);
    await connectUser(username);
    setCurrentUser(username);
    setIsConnectingHubs(false);
  };

  const handleRetry = () => window.location.reload();

  return (
    <>
      {isConnectingHubs ? (
        <div id="center">
          <div className="auth-wrapper" style={{ marginTop: '20px' }}>
            <div className="auth-box" style={{ textAlign: 'center', padding: '32px' }}>
              <div className="status-msg info" style={{ margin: '0 0 20px 0', backgroundColor: 'var(--primary-light)', color: 'var(--primary)' }}>
                Initializing services...
              </div>
              <p style={{ fontSize: '15px' }}>Setting up secure chat tunnels.</p>
            </div>
          </div>
        </div>
      ) : !isReady ? (
        <div id="center">
          <div className="auth-wrapper" style={{ marginTop: '20px' }}>
            <div className="auth-box" style={{ textAlign: 'center', padding: '32px' }}>
              <div className="status-msg error" style={{ margin: '0 0 20px 0' }}>Connection to server failed...</div>
              <p style={{ fontSize: '15px', marginBottom: '24px' }}>Please check if the back-end server is running.</p>
              <button onClick={handleRetry} className="submit-btn" style={{ backgroundColor: 'var(--accent)' }}>
                Retry Connection
              </button>
            </div>
          </div>
        </div>
      ) : currentUser && serverConnection ? (
        <HomePage username={currentUser} />
      ) : (
        <div id="center">
          <div className="auth-container-wrapper">
            <AuthForm onAuthSuccess={handleAuthSuccess} />
          </div>
        </div>
      )}
    </>
  );
}

export default App;