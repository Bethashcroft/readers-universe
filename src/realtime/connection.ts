import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
} from "@microsoft/signalr";
import { API_ORIGIN, getToken } from "../api/client";

let connection: HubConnection | null = null;
let starting: Promise<void> | null = null;

export function getChatConnection(): HubConnection {
  if (!connection) {
    connection = new HubConnectionBuilder()
      .withUrl(`${API_ORIGIN}/hubs/chat`, {
        accessTokenFactory: () => getToken() ?? "",
      })
      .withAutomaticReconnect()
      .build();
  }
  return connection;
}

export async function startChatConnection(): Promise<HubConnection> {
  const conn = getChatConnection();
  if (conn.state === HubConnectionState.Disconnected) {
    starting ??= conn.start().finally(() => {
      starting = null;
    });
  }
  if (starting) {
    await starting;
  }
  return conn;
}

export async function stopChatConnection(): Promise<void> {
  if (connection) {
    const conn = connection;
    connection = null;
    await conn.stop();
  }
}
