import React from 'react';
import type { Message } from '../types';

interface ChatAreaProps {
  chatHeaderTitle: string;
  currentMessages: Message[];
  username: string;
  viewMode: 'server' | 'dm';
  activeChannel: any;
  activeDMUsername: any;
  inputText: string;
  onInputChange: (text: string) => void;
  onSendMessage: (e: React.FormEvent) => void;
}

export const ChatArea: React.FC<ChatAreaProps> = ({
  chatHeaderTitle,
  currentMessages,
  username,
  viewMode,
  activeChannel,
  activeDMUsername,
  inputText,
  onInputChange,
  onSendMessage,
}) => {
  return (
    <div className="chat-main">
      <div className="chat-header">
        <h3>{chatHeaderTitle}</h3>
      </div>

      <div className="chat-messages">
        {currentMessages.length > 0 ? (
          currentMessages.map((msg, index) => {
            const isMe = msg.senderId === username;
            return (
              <div key={index} className={`message-row ${isMe ? 'me' : ''}`}>
                <div className="message-content">
                  <div className="message-meta" style={{ display: 'flex', alignItems: 'baseline', gap: '8px' }}>
                    <span className="message-author">{msg.senderId}</span>
                    {msg.timestamp && (
                      <span className="message-time" style={{ fontSize: '0.75rem', color: '#888' }}>
                        {new Date(msg.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      </span>
                    )}
                  </div>
                  <div className="message-bubble">{msg.text}</div>
                </div>
              </div>
            );
          })
        ) : (
          <div className="empty-chat-placeholder">
            {viewMode === 'server' && activeChannel 
              ? `Welcome to the beginning of the #${activeChannel.name} channel!`
              : `This is the start of your direct message history.`}
          </div>
        )}
      </div>

      <form onSubmit={onSendMessage} className="chat-input-area">
        <input
          type="text"
          className="input-field"
          value={inputText}
          onChange={(e) => onInputChange(e.target.value)}
          disabled={viewMode === 'server' && !activeChannel}
          placeholder={
            viewMode === 'server' && activeChannel
              ? `Message #${activeChannel.name}` 
              : viewMode === 'dm'
              ? `Message @${activeDMUsername}`
              : "Select a channel to start chatting"
          }
        />
        <button 
          type="submit" 
          className="submit-btn inline-btn" 
          disabled={viewMode === 'server' && !activeChannel}
        >
          Send
        </button>
      </form>
    </div>
  );
};