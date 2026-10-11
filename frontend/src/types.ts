export interface Channel {
  id: string;
  name: string;
}

export interface Server {
  id: string;
  name: string;
  icon: string;
  adminId: string;
  channels: Channel[];
}

export interface Message {
  senderId: string;
  text: string;
  timestamp?: string;
}

export interface Response {
  success: boolean;
  message: string;
  errorCode?: string;
}