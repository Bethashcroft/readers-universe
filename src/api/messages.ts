import { request } from "./client";

export interface MessageResponse {
  id: number;
  senderId: string;
  senderName: string;
  text: string;
  date: string;
}

export interface ConversationResponse {
  bookTitle: string;
  otherUserName: string;
  messages: MessageResponse[];
}

export function getConversation(
  requestId: number,
): Promise<ConversationResponse> {
  return request(
    `/messages/request/${requestId}`,
    "Failed to load conversation",
  );
}

export function getUnreadCount(): Promise<{ count: number }> {
  return request("/messages/unread-count", "Failed to load unread count");
}

export const messagesReadEvent = "readers:messages-read";

export function announceMessagesRead(): void {
  window.dispatchEvent(new CustomEvent(messagesReadEvent));
}

export function markConversationRead(
  requestId: number,
): Promise<{ marked: number }> {
  return request(`/messages/${requestId}/read`, "Failed to mark as read", {
    method: "POST",
  });
}

export function sendMessage(
  requestId: number,
  text: string,
): Promise<MessageResponse> {
  return request("/messages", "Failed to send message", {
    method: "POST",
    body: JSON.stringify({ borrowRequestId: requestId, text }),
  });
}
