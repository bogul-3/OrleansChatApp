import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.tsx'
import { SignalRProvider } from './context/SignalRContext.tsx'
import './index.css'
import { ChatProvider } from './context/ChatContext.tsx'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <SignalRProvider>
      <ChatProvider>
        <App />
      </ChatProvider>
    </SignalRProvider>
  </React.StrictMode>,
)