import React from 'react';
import type { Channel } from '../types';

interface ChannelSidebarProps {
  viewMode: 'server' | 'dm';
  activeServerName: string | null;
  channels: Channel[];
  activeChannelId: string | null;
  onSelectChannel: (id: string) => void;
  onCreateChannelClick: () => void;
  activePrivateChats: string[];
  activeDMUsername: string;
  onSelectDMUsername: (username: string) => void;
  username: string;
  onLogout: () => void;
  onInviteClick: () => void;
  isAdmin: boolean;
  activeServerId: string | null;
  onDeleteServerClick: (serverId: string) => void;
  onDeleteChannelClick: (channelId: string) => void;
  onNewDmClick: () => void;
}

export const ChannelSidebar: React.FC<ChannelSidebarProps> = ({
  viewMode,
  activeServerName,
  channels,
  activeChannelId,
  onSelectChannel,
  onCreateChannelClick,
  activePrivateChats,
  activeDMUsername,
  onSelectDMUsername,
  username,
  onLogout,
  onInviteClick,
  isAdmin,
  activeServerId,
  onDeleteServerClick, 
  onDeleteChannelClick,
  onNewDmClick,
}) => {
  const getDisplayName = (chatKey: string) => {
    const names = chatKey.split('_');
    return names[0] === username ? names[1] : names[0];
  };

  return (
    <div className="channel-sidebar">
      <div className="server-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', paddingRight: '12px' }}>
        <h4>{viewMode === 'server' && activeServerName ? activeServerName : 'Direct Messages'}</h4>

        {viewMode === 'server' && activeServerName && isAdmin && (
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <button
              onClick={onInviteClick}
              style={{
                background: 'none',
                border: 'none',
                color: '#23a55a',
                cursor: 'pointer',
                fontSize: '16px',
                fontWeight: 'bold',
                padding: '4px'
              }}
              title="Invite User to Server"
            >
              ➕👤
            </button>

            <button
              onClick={() => activeServerId && onDeleteServerClick(activeServerId)}
              style={{
                background: 'none',
                border: 'none',
                color: '#f23f43',
                cursor: 'pointer',
                fontSize: '16px',
                padding: '4px',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                borderRadius: '4px',
                transition: 'background-color 0.2s'
              }}
              title="Delete Server"
              onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'rgba(242, 63, 67, 0.1)')}
              onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
            >
              🗑️
            </button>
          </div>
        )}
      </div>

      <div className="channel-list">
        {viewMode === 'server' && activeServerName ? (
          <>
            <div className="section-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', paddingRight: '8px' }}>
              <span className="section-title">Text Channels</span>
              {isAdmin && (
                <button
                  className="add-channel-btn-small"
                  onClick={onCreateChannelClick}
                  style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer', fontSize: '18px' }}
                  title="Create Channel"
                >
                  +
                </button>
              )}
            </div>
            {channels.map((channel) => (
              <div key={channel.id} className="channel-item-wrapper" style={{ display: 'flex', alignItems: 'center' }}>
                <button
                  className={`channel-item ${activeChannelId === channel.id ? 'active' : ''}`}
                  onClick={() => onSelectChannel(channel.id)}
                  style={{ flexGrow: 1 }}
                >
                  <span className="hashtag">#</span> {channel.name}
                </button>
                {isAdmin && (
                  <button
                    onClick={(e) => { e.stopPropagation(); onDeleteChannelClick(channel.id); }}
                    style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#f23f43' }}
                    title="Delete Channel"
                  >
                    🗑️
                  </button>
                )}
              </div>
            ))}
          </>
        ) : viewMode === 'dm' ? (
          <>
            <span className="section-title">Direct Messages</span>
            <button
              onClick={onNewDmClick}
              style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer', fontSize: '18px' }}
              title="Start New Chat"
            >
              +
            </button>
            {activePrivateChats.map((chatKey) => {
              const displayName = getDisplayName(chatKey);
              return (
                <button
                  key={chatKey}
                  className={`channel-item ${activeDMUsername === displayName ? 'active' : ''}`}
                  onClick={() => onSelectDMUsername(displayName)}
                  style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
                >
                  <span>💬</span> {displayName}
                </button>
              );
            })}
          </>
        ) : null}
      </div>

      <div className="user-footer" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '0 8px' }}>
        <div className="user-info" style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span className="user-avatar">👤</span>
          <span className="user-name" style={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', maxWidth: '100px' }}>
            @{username}
          </span>
        </div>

        <button
          onClick={onLogout}
          className="logout-btn"
          style={{
            background: 'none',
            border: 'none',
            color: '#f23f43',
            cursor: 'pointer',
            fontSize: '16px',
            padding: '4px 8px',
            borderRadius: '4px',
            transition: 'background-color 0.2s',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center'
          }}
          title="Log Out"
          onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'rgba(242, 63, 67, 0.1)')}
          onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
        >
          Log Out
        </button>
      </div>
    </div>
  );
};