import React, { createContext, useContext, useEffect, useRef, useState } from 'react';
import { useSignalR } from './SignalRContext';
import type { Message } from '../types';
import type { Response } from '../types';

interface ChatContextType {
    messages: Record<string, Message[]>;
    activePrivateChats: string[];
    sendMessage: (targetId: string, text: string, isDM: boolean) => Promise<Response>;
    fetchHistory: (channelId: string, isDM: boolean) => Promise<void>;
    joinChannel: (channelId: string) => Promise<void>;
    leaveChannel: (channelId: string) => Promise<void>;
}

const ChatContext = createContext<ChatContextType | undefined>(undefined);

export const ChatProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const { chatConnection } = useSignalR();
    const [messages, setMessages] = useState<Record<string, Message[]>>({});
    const [activePrivateChats, setActivePrivateChats] = useState<string[]>([]);
    const messagesRef = useRef<Record<string, Message[]>>({});

    useEffect(() => {
        messagesRef.current = messages;
    }, [messages]);

    useEffect(() => {
        if (!chatConnection) return;

        chatConnection.invoke<string[]>('GetMyPrivateChats').then(setActivePrivateChats);

        const handleReceiveMessage = (channelId: string, messageDto: Message) => {
            const current = messagesRef.current;
            setMessages({
                ...current,
                [channelId]: [...(current[channelId] || []), messageDto]
            });
        };

        const handleReceivePrivate = (chatKey: string, messageDto: Message) => {
            const current = messagesRef.current;
            setMessages({
                ...current,
                [chatKey]: [...(current[chatKey] || []), messageDto]
            });
        };

        const handleUpdateList = (newChatKey: string) => {
            setActivePrivateChats(prev => prev.includes(newChatKey) ? prev : [...prev, newChatKey]);
        };

        chatConnection.on('ReceiveChannelMessage', handleReceiveMessage);
        chatConnection.on('ReceivePrivateMessage', handleReceivePrivate);
        chatConnection.on('UpdateList', handleUpdateList);
        return () => {
            chatConnection.off('ReceiveChannelMessage', handleReceiveMessage);
            chatConnection.off('ReceivePrivateMessage', handleReceivePrivate);
            chatConnection.off('UpdateList', handleUpdateList);
        };
    }, [chatConnection]);

    const sendMessage = async (targetId: string, text: string, isDM: boolean): Promise<Response> => {
        if (!chatConnection) {
            return { success: false, message: "Nije uspostavljena veza sa chatom.", errorCode: "NO_CONNECTION" };
        }
        try {
            const method = isDM ? 'SendPrivateMessage' : 'SendChannelMessage';
            const response = await chatConnection.invoke<Response>(method, targetId, text);
            return response;
        } catch (err) {
            console.error("Greška pri pozivu Hub-a:", err);
            return { success: false, message: "Sistemska greška pri slanju.", errorCode: "SYSTEM_ERROR" };
        }
    };

    const fetchHistory = async (id: string, isDM: boolean = false) => {
        if (!chatConnection) return;

        const endpoint = isDM ? 'GetPrivateHistory' : 'GetChannelHistory';

        const history = await chatConnection.invoke<Message[]>(endpoint, id);
        setMessages(prev => ({ ...prev, [id]: history }));
    };
    const joinChannel = async (channelId: string) => {
        if (!chatConnection) return;
        await chatConnection.invoke('JoinChannel', channelId);
    };

    const leaveChannel = async (channelId: string) => {
        if (!chatConnection) return;
        await chatConnection.invoke('LeaveChannel', channelId);
    };

    return (
        <ChatContext.Provider value={{ messages, activePrivateChats, sendMessage, fetchHistory, joinChannel, leaveChannel }}>
            {children}
        </ChatContext.Provider>
    );
};

export const useChat = () => {
    const context = useContext(ChatContext);
    if (!context) throw new Error('useChat must be used within a ChatProvider');
    return context;
};