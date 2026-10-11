import React from 'react';

interface ModalProps {
  showCreateServer: boolean;
  setShowCreateServer: (val: boolean) => void;
  showCreateChannel: boolean;
  setShowCreateChannel: (val: boolean) => void;
  showInvite: boolean;
  setShowInvite: (val: boolean) => void;

  onServerSubmit: (e: React.FormEvent) => void;
  onChannelSubmit: (e: React.FormEvent) => void;
  onInviteSubmit: (e: React.FormEvent) => void;

  serverName: string; setServerName: (val: string) => void;
  channelName: string; setChannelName: (val: string) => void;
  inviteUsername: string; setInviteUsername: (val: string) => void;

  serverError: string;
  channelError: string;
  inviteError: string;
  inviteSuccess: string;

  showNewDmModal: boolean;
  setShowNewDmModal: (val: boolean) => void;
  targetUsername: string;
  setTargetUsername: (val: string) => void;
  firstDmMessage: string;
  setFirstDmMessage: (val: string) => void;
  onNewDmSubmit: () => void;
  dmError: string;
}

// 2. Primeni interfejs na komponentu
export const Modals: React.FC<ModalProps> = ({
  showCreateServer, setShowCreateServer, onServerSubmit, serverName, setServerName, serverError,
  showCreateChannel, setShowCreateChannel, onChannelSubmit, channelName, setChannelName, channelError,
  showInvite, setShowInvite, onInviteSubmit, inviteUsername, setInviteUsername, inviteError, inviteSuccess,
  showNewDmModal, setShowNewDmModal, targetUsername, setTargetUsername, firstDmMessage, setFirstDmMessage, onNewDmSubmit, dmError
}) => {
  return (
    <>
      {showCreateServer && (
        <div className="modal-overlay">
          <div className="auth-box modal-box">
            <h3>Create Server</h3>
            <form onSubmit={onServerSubmit}>
              <div className="input-group">
                <label>Name</label>
                <input type="text" className="input-field" value={serverName} onChange={e => setServerName(e.target.value)} required />
              </div>
              {serverError && <div className="status-msg error">{serverError}</div>}
              <div className="modal-actions">
                <button type="submit" className="submit-btn">Create</button>
                <button type="button" className="submit-btn inline-btn" onClick={() => setShowCreateServer(false)}>Cancel</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {showCreateChannel && (
        <div className="modal-overlay">
          <div className="auth-box modal-box">
            <h3>Create Channel</h3>
            <form onSubmit={onChannelSubmit}>
              <div className="input-group">
                <label>Name</label>
                <input type="text" className="input-field" value={channelName} onChange={e => setChannelName(e.target.value)} required />
              </div>
              {channelError && <div className="status-msg error">{channelError}</div>}
              <div className="modal-actions">
                <button type="submit" className="submit-btn">Create</button>
                <button type="button" className="submit-btn inline-btn" onClick={() => setShowCreateChannel(false)}>Cancel</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {showInvite && (
        <div className="modal-overlay">
          <div className="auth-box modal-box">
            <h3>Invite</h3>
            <form onSubmit={onInviteSubmit}>
              <div className="input-group">
                <label>Username</label>
                <input type="text" className="input-field" value={inviteUsername} onChange={e => setInviteUsername(e.target.value)} required />
              </div>
              {inviteError && <div className="status-msg error">{inviteError}</div>}
              {inviteSuccess && <div className="status-msg success" style={{ color: '#23a55a' }}>{inviteSuccess}</div>}
              <div className="modal-actions">
                <button type="submit" className="submit-btn">Invite</button>
                <button type="button" className="submit-btn inline-btn" onClick={() => setShowInvite(false)}>Cancel</button>
              </div>
            </form>
          </div>
        </div>
      )}
      
      {showNewDmModal && (
        <div className="modal-overlay">
          <div className="auth-box modal-box">
            <h3>Start New Chat</h3>
            <div className="input-group">
              <label>Username</label>
              <input
                className="input-field"
                placeholder="Enter username..."
                value={targetUsername}
                onChange={(e) => setTargetUsername(e.target.value)}
              />
            </div>
            {dmError && <div className="status-msg error">{dmError}</div>}
            <div className="input-group">
              <label>Message</label>
              <textarea
                className="input-field"
                placeholder="Say hello!"
                value={firstDmMessage}
                onChange={(e) => setFirstDmMessage(e.target.value)}
                style={{ height: '80px', resize: 'none' }}
              />
            </div>
            <div className="modal-actions">
              <button type="button" className="submit-btn" onClick={onNewDmSubmit}>Send Message</button>
              <button type="button" className="submit-btn inline-btn" onClick={() => setShowNewDmModal(false)}>Cancel</button>
            </div>
          </div>
        </div>
      )}
    </>
  );
};