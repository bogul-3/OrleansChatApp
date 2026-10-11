import React from 'react';
import type { Server } from '../types';

interface ServerSidebarProps {
  servers: Server[];
  viewMode: 'server' | 'dm';
  activeServerId: string | null;
  onSelectDm: () => void;
  onSelectServer: (id: string) => void;
  onCreateServerClick: () => void;
}

export const ServerSidebar: React.FC<ServerSidebarProps> = ({
  servers,
  viewMode,
  activeServerId,
  onSelectDm,
  onSelectServer,
  onCreateServerClick,
}) => {
  return (
    <div className="server-sidebar">
      <button 
        className={`server-icon home-btn ${viewMode === 'dm' ? 'active' : ''}`} 
        onClick={onSelectDm}
        title="Direct Messages"
      >
        DM
      </button>
      <div className="sidebar-divider" />
      
      {servers.map((server) => (
        <button
          key={server.id}
          className={`server-icon ${viewMode === 'server' && activeServerId === server.id ? 'active' : ''}`}
          onClick={() => onSelectServer(server.id)}
          title={server.name}
        >
          {server.name.substring(0, 2).toUpperCase()}
        </button>
      ))}

      <button 
        className="server-icon add-server-btn"
        onClick={onCreateServerClick}
        title="Create a Server"
      >
        +
      </button>
    </div>
  );
};