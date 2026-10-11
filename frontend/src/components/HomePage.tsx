import React, { useState, useEffect, useRef } from 'react';
import { useSignalR } from '../context/SignalRContext';
import { useChat } from '../context/ChatContext';
import { ServerSidebar } from './ServerSidebar';
import { ChannelSidebar } from './ChannelSidebar';
import { ChatArea } from './ChatArea';
import { Modals } from './Modals';

interface HomepageProps {
  username: string;
}

export const HomePage: React.FC<HomepageProps> = ({ username }) => {
  const { disconnectUser, servers, createServer, deleteServer, createChannel, inviteToServer, deleteChannel } = useSignalR();
  const { messages, activePrivateChats, sendMessage, fetchHistory, joinChannel, leaveChannel } = useChat();

  const [viewMode, setViewMode] = useState<'server' | 'dm'>('server');
  const [showCreateServerModal, setShowCreateServerModal] = useState(false);
  const [showCreateChannelModal, setShowCreateChannelModal] = useState(false);
  const [showInviteModal, setShowInviteModal] = useState(false);
  const [showNewDmModal, setShowNewDmModal] = useState(false);
  const [targetUsername, setTargetUsername] = useState('');
  const [firstDmMessage, setFirstDmMessage] = useState('');

  const [newServerName, setNewServerName] = useState('');
  const [newChannelName, setNewChannelName] = useState('');
  const [inviteUsername, setInviteUsername] = useState('');

  const [serverError, setServerError] = useState('');
  const [channelError, setChannelError] = useState('');
  const [inviteError, setInviteError] = useState('');
  const [inviteSuccess, setInviteSuccess] = useState('');
  const [dmError, setDmError] = useState('');


  const [activeServerId, setActiveServerId] = useState<string | null>(null);
  const [activeChannelId, setActiveChannelId] = useState<string | null>(null);
  const [activeDMUsername, setActiveDMUsername] = useState<string>('');
  const [inputText, setInputText] = useState('');

  const activeServer = servers.find((s) => s.id === activeServerId) || null;
  const activeChannel = activeServer ? (activeServer.channels.find((c) => c.id === activeChannelId) || null) : null;

  const getChatKey = (user1: string, user2: string) => {
    const participants = [user1, user2].sort();
    return `${participants[0]}_${participants[1]}`;
  };

  const activeChatKey = viewMode === 'dm' && activeDMUsername ? getChatKey(username, activeDMUsername) : null;
  const currentChatId = viewMode === 'server' ? activeChannelId : activeChatKey;
  const currentMessages = currentChatId ? (messages[currentChatId] || []) : [];

  const prevChannelIdRef = useRef<string | null>(null);

  let chatHeaderTitle = 'No Channel Selected';
  if (viewMode === 'server' && activeChannel) {
    chatHeaderTitle = `# ${activeChannel.name}`;
  } else if (viewMode === 'dm' && activeDMUsername) {
    chatHeaderTitle = `${activeDMUsername}`;
  }

  useEffect(() => {
    if (viewMode === 'dm' && activeChatKey) {
      fetchHistory(activeChatKey, true);
    }
  }, [activeChatKey, viewMode]);

  useEffect(() => {
    const manageChannelTransition = async () => {
      const previousId = prevChannelIdRef.current;
      const currentId = activeChannelId;

      if (previousId && previousId !== currentId) {
        await leaveChannel(previousId);
      }

      if (currentId) {
        await joinChannel(currentId);
      }

      prevChannelIdRef.current = currentId;
    };

    manageChannelTransition();
  }, [activeChannelId]);

  useEffect(() => {
    if (servers.length > 0 && !activeServerId) {
      setActiveServerId(servers[0].id);
      setActiveChannelId(servers[0].channels[0]?.id || null);
    }
  }, [servers, activeServerId]);

  useEffect(() => {
    if (viewMode === 'server' && activeChannelId) {
      fetchHistory(activeChannelId, false);
    }
  }, [activeChannelId, viewMode]);

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!inputText.trim()) return;

    if (viewMode === 'dm' && activeDMUsername) {
      await sendMessage(activeDMUsername, inputText.trim(), true);
    } else if (activeChannelId) {
      await sendMessage(activeChannelId, inputText.trim(), false);
    }
    setInputText('');
  };

  const handleCreateServerSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setServerError('');

    const response = await createServer(newServerName);

    if (response.success) {
      setShowCreateServerModal(false);
      setNewServerName('');
    } else {
      setServerError(response.message);
    }
  };

  const handleDeleteServer = async (serverId: string) => {
    if (window.confirm("Are you sure you want to delete this server?")) {
      try {
        const response = await deleteServer(serverId);
        if (!response?.success) {
          alert(response?.message || 'Server delete failed');
        }
      } catch (err: any) {
        alert('System error while deleting server');
      }
    }
  };

  const handleCreateChannelSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!activeServerId || !newChannelName.trim()) {
      setChannelError("Server ID and channel name are required.");
      return;
    }
    setChannelError('');

    const response = await createChannel(activeServerId, newChannelName);

    if (response.success) {
      setShowCreateChannelModal(false);
      setNewChannelName('');
    } else {
      setChannelError(response.message);
    }
  };

  const handleInviteSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!activeServerId || !inviteUsername.trim()) {
      setInviteError("Server ID and username are required.");
      return;
    }

    setInviteError('');
    const response = await inviteToServer(activeServerId, inviteUsername);

    if (response.success) {
      setInviteSuccess('User invited successfully!');
      setTimeout(() => setShowInviteModal(false), 1500);
    } else {
      setInviteError(response.message);
    }
  };

  const handleLogout = async () => { await disconnectUser(); window.location.reload(); };

  const handleServerClick = (id: string) => {
    setViewMode('server');
    setActiveServerId(id);
    const srv = servers.find(s => s.id === id);
    setActiveChannelId(srv?.channels[0]?.id || null);
  };

  const handleDeleteChannel = async (channelId: string) => {
    if (!activeServerId) return;

    if (window.confirm("Are you sure you want to delete this channel?")) {
      const response = await deleteChannel(activeServerId, channelId);

      if (!response.success) {
        alert(response.message || 'Error while deleting channel');
      }
    }
  };

  const handleStartNewDm = async () => {
    if (!targetUsername.trim() || !firstDmMessage.trim()) return;
    setDmError('');

    const response = await sendMessage(targetUsername, firstDmMessage.trim(), true);

    if (response.success) {
      setActiveDMUsername(targetUsername);
      setViewMode('dm');
      setShowNewDmModal(false);
      setTargetUsername('');
      setFirstDmMessage('');
    } else {
      setDmError(response.message);
    }
  };

  return (
    <div className="homepage-container">
      <ServerSidebar
        servers={servers}
        viewMode={viewMode}
        activeServerId={activeServerId}
        onSelectDm={() => setViewMode('dm')}
        onSelectServer={handleServerClick}
        onCreateServerClick={() => setShowCreateServerModal(true)}
      />
      <ChannelSidebar
        viewMode={viewMode}
        activeServerName={activeServer?.name || null}
        channels={activeServer?.channels || []}
        activeChannelId={activeChannelId}
        onSelectChannel={setActiveChannelId}
        onCreateChannelClick={() => setShowCreateChannelModal(true)}
        activePrivateChats={activePrivateChats}
        activeDMUsername={activeDMUsername}
        onSelectDMUsername={setActiveDMUsername}
        username={username}
        onLogout={handleLogout}
        onInviteClick={() => { setInviteError(''); setInviteSuccess(''); setShowInviteModal(true); }}
        isAdmin={activeServer?.adminId === username}
        activeServerId={activeServerId}
        onDeleteServerClick={handleDeleteServer}
        onDeleteChannelClick={handleDeleteChannel}
        onNewDmClick={() => setShowNewDmModal(true)}
      />
      <ChatArea
        chatHeaderTitle={chatHeaderTitle}
        currentMessages={currentMessages}
        username={username} viewMode={viewMode}
        activeChannel={activeChannel}
        activeDMUsername={activeDMUsername}
        inputText={inputText}
        onInputChange={setInputText}
        onSendMessage={handleSendMessage} />

      <Modals
        showCreateServer={showCreateServerModal} setShowCreateServer={setShowCreateServerModal}
        onServerSubmit={handleCreateServerSubmit} serverName={newServerName} setServerName={setNewServerName} serverError={serverError}

        showCreateChannel={showCreateChannelModal} setShowCreateChannel={setShowCreateChannelModal}
        onChannelSubmit={handleCreateChannelSubmit} channelName={newChannelName} setChannelName={setNewChannelName} channelError={channelError}

        showInvite={showInviteModal} setShowInvite={setShowInviteModal}
        onInviteSubmit={handleInviteSubmit} inviteUsername={inviteUsername} setInviteUsername={setInviteUsername}
        inviteError={inviteError} inviteSuccess={inviteSuccess}

        showNewDmModal={showNewDmModal}
        setShowNewDmModal={setShowNewDmModal}
        targetUsername={targetUsername}
        setTargetUsername={setTargetUsername}
        firstDmMessage={firstDmMessage}
        setFirstDmMessage={setFirstDmMessage}
        onNewDmSubmit={handleStartNewDm}
        dmError={dmError}
      />
    </div>
  );
};