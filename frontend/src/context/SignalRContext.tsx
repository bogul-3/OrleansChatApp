import React, { createContext, useContext, useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import type { Channel, Server, Response } from '../types';

interface SignalRContextType {
  authConnection: signalR.HubConnection | null;
  serverConnection: signalR.HubConnection | null;
  chatConnection: signalR.HubConnection | null;
  isReady: boolean;
  servers: Server[];
  connectUser: (username: string) => Promise<void>;
  disconnectUser: () => Promise<void>;
  createServer(serverName: string): Promise<Response>;
  deleteServer(serverId: string): Promise<Response>;
  createChannel: (serverId: string, channelName: string) => Promise<Response>;
  inviteToServer: (serverId: string, username: string) => Promise<Response>;
  deleteChannel: (serverId: string, channelId: string) => Promise<Response>;
}

const SignalRContext = createContext<SignalRContextType | undefined>(undefined);

const BACKEND_URL = 'http://localhost:5058';

export const SignalRProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [authConnection, setAuthConnection] = useState<signalR.HubConnection | null>(null);
  const [serverConnection, setServerConnection] = useState<signalR.HubConnection | null>(null);
  const [chatConnection, setChatConnection] = useState<signalR.HubConnection | null>(null);
  const [isReady, setIsReady] = useState(false);
  const [servers, setServers] = useState<Server[]>([]);

  useEffect(() => {
    const authConn = new signalR.HubConnectionBuilder()
      .withUrl(`${BACKEND_URL}/hub/auth`)
      .withAutomaticReconnect()
      .build();

    authConn.start()
      .then(() => {
        setIsReady(true);
      })
      .catch((err) => console.error('Error connecting to AuthHub:', err));

    setAuthConnection(authConn);

    return () => {
      authConn.stop();
    };
  }, []);

  useEffect(() => {
    const savedUsername = localStorage.getItem("chat_username");
    if (savedUsername && isReady) {
      connectUser(savedUsername);
    }
  }, [isReady]);

  const connectUser = async (username: string) => {
    if (serverConnection || chatConnection) return;

    const serverConn = new signalR.HubConnectionBuilder()
      .withUrl(`${BACKEND_URL}/hub/server?username=${username}`)
      .withAutomaticReconnect()
      .build();

    serverConn.on("ServerDeleted", (deletedServerId: string) => {
      setServers(prev => prev.filter(s => s.id !== deletedServerId));
    });

    serverConn.on("ServerAdded", (newServer: Server) => {
      setServers(prev => {
        const exists = prev.some(s => s.id === newServer.id);

        if (exists) {
          return prev;
        }

        return [...prev, newServer];
      });
    });

    serverConn.on("ChannelAdded", (data: { serverId: string, channel: Channel }) => {
      setServers(prevServers => prevServers.map(server => {
        if (server.id === data.serverId) {
          const exists = server.channels.some(c => c.id === data.channel.id);
          if (exists) return server;
          return {
            ...server,
            channels: [...server.channels, data.channel]
          };
        }
        return server;
      }));
    });

    serverConn.on("ChannelDeleted", (data: { serverId: string, channelId: string }) => {
      setServers(prevServers => prevServers.map(server => {
        if (server.id === data.serverId) {
          return {
            ...server,
            channels: server.channels.filter(c => c.id !== data.channelId)
          };
        }
        return server;
      }));
    });

    const chatConn = new signalR.HubConnectionBuilder()
      .withUrl(`${BACKEND_URL}/hub/chat?username=${username}`)
      .withAutomaticReconnect()
      .build();

    try {
      await Promise.all([serverConn.start(), chatConn.start()]);

      localStorage.setItem("chat_username", username);

      setServerConnection(serverConn);
      setChatConnection(chatConn);

      const myServers = await serverConn.invoke<Server[]>("GetMyServers");
      setServers(myServers);
    } catch (err) {
      console.error('Failed to establish full user connections:', err);
    }
  };

  const disconnectUser = async () => {
    if (serverConnection) await serverConnection.stop();
    if (chatConnection) await chatConnection.stop();

    localStorage.removeItem("chat_username");

    setServerConnection(null);
    setChatConnection(null);
    setServers([]);
  };

  const createServer = async (serverName: string): Promise<Response> => {
    if (!serverConnection) return { success: false, message: "Konekcija nije uspostavljena" };

    try {
      const response = await serverConnection.invoke<Response>("CreateServer", serverName);
      return response;
    } catch (err: any) {
      return { success: false, message: err.message || "Sistemska greška" };
    }
  };

  const deleteServer = async (serverId: string): Promise<Response> => {
    if (!serverConnection) return { success: false, message: "Konekcija nije uspostavljena" };

    try {
      const response = await serverConnection.invoke<Response>("DeleteServer", serverId);
      return response;
    } catch (err: any) {
      return { success: false, message: "Sistemska greška pri brisanju" };
    }
  };

  const createChannel = async (serverId: string, channelName: string): Promise<Response> => {
    if (!serverConnection) return { success: false, message: "Konekcija nije uspostavljena" };

    try {
      const response = await serverConnection.invoke<Response>("AddChannel", serverId, channelName);
      return response;
    } catch (err: any) {
      return { success: false, message: "Sistemska greška" };
    }
  };

  const inviteToServer = async (serverId: string, username: string): Promise<Response> => {
    if (!serverConnection) return { success: false, message: "Konekcija nije uspostavljena" };

    try {
      const response = await serverConnection.invoke<Response>("InviteToServer", serverId, username);
      return response;
    } catch (err: any) {
      return { success: false, message: "Sistemska greška" };
    }
  };

  const deleteChannel = async (serverId: string, channelId: string): Promise<Response> => {
    if (!serverConnection) return { success: false, message: "Konekcija nije uspostavljena" };

    try {
      const response = await serverConnection.invoke<Response>("DeleteChannel", serverId, channelId);
      return response;
    } catch (err: any) {
      return { success: false, message: "Sistemska greška pri brisanju" };
    }
  };


  return (
    <SignalRContext.Provider value={{
      authConnection,
      serverConnection,
      chatConnection,
      isReady,
      connectUser,
      disconnectUser,
      servers,
      createServer,
      deleteServer,
      createChannel,
      inviteToServer,
      deleteChannel
    }}>
      {children}
    </SignalRContext.Provider>
  );
};

export const useSignalR = () => {
  const context = useContext(SignalRContext);
  if (!context) throw new Error('useSignalR must be used within a SignalRProvider');
  return context;
};