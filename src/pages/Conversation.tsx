import { useState, useEffect, useRef } from "react";
import { useParams } from "react-router-dom";
import { useAuth } from "../context/useAuth";
import { getConversation, sendMessage } from "../api/messages";
import type { ConversationResponse, MessageResponse } from "../api/messages";
import {
  getChatConnection,
  startChatConnection,
} from "../realtime/connection";
import { usePageTitle } from "../hooks/usePageTitle";
import "./Conversation.css";

function Conversation() {
  const { requestId } = useParams();
  const { user } = useAuth();
  const [conversation, setConversation] =
    useState<ConversationResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [text, setText] = useState("");
  const [error, setError] = useState("");
  const [sending, setSending] = useState(false);
  const threadEndRef = useRef<HTMLDivElement>(null);

  usePageTitle(
    conversation ? `Chat about ${conversation.bookTitle}` : "Messages",
  );

  useEffect(() => {
    const load = async () => {
      try {
        setConversation(await getConversation(Number(requestId)));
      } catch (err) {
        console.error("Failed to load conversation:", err);
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [requestId]);

  useEffect(() => {
    let active = true;

    const handleNewMessage = (message: MessageResponse) => {
      setConversation((prev) => {
        if (!prev || prev.messages.some((m) => m.id === message.id)) {
          return prev;
        }
        return { ...prev, messages: [...prev.messages, message] };
      });
    };

    const join = async () => {
      try {
        const conn = await startChatConnection();
        if (!active) return;
        conn.on("NewMessage", handleNewMessage);
        await conn.invoke("JoinConversation", Number(requestId));
      } catch (err) {
        console.error("Failed to join live chat:", err);
      }
    };

    join();

    return () => {
      active = false;
      const conn = getChatConnection();
      conn.off("NewMessage", handleNewMessage);
      conn.invoke("LeaveConversation", Number(requestId)).catch(() => {});
    };
  }, [requestId]);

  useEffect(() => {
    threadEndRef.current?.scrollIntoView?.({ behavior: "smooth" });
  }, [conversation?.messages.length]);

  const handleSend = async (e: React.SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!text.trim() || !conversation) return;

    setError("");
    setSending(true);

    try {
      const sent = await sendMessage(Number(requestId), text);
      setConversation({
        ...conversation,
        messages: [...conversation.messages, sent],
      });
      setText("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to send message");
    } finally {
      setSending(false);
    }
  };

  const formatTime = (date: string) =>
    new Date(date).toLocaleString("en-GB", {
      day: "numeric",
      month: "short",
      hour: "2-digit",
      minute: "2-digit",
    });

  if (loading) {
    return <p>Loading conversation...</p>;
  }

  if (!conversation) {
    return <p>Conversation not found</p>;
  }

  return (
    <div className="conversation">
      <header className="conversation-header">
        <h1>{conversation.bookTitle}</h1>
        <p>Chatting with {conversation.otherUserName}</p>
      </header>

      <div className="conversation-thread">
        {conversation.messages.length === 0 && (
          <p className="conversation-empty">
            No messages yet. Say hi and sort out the details!
          </p>
        )}
        {conversation.messages.map((message) => (
          <div
            key={message.id}
            className={`bubble ${
              message.senderId === user?.userId ? "mine" : "theirs"
            }`}
          >
            <p className="bubble-text">{message.text}</p>
            <span className="bubble-meta">{formatTime(message.date)}</span>
          </div>
        ))}
        <div ref={threadEndRef} />
      </div>

      {error && <p className="form-error">{error}</p>}

      <form className="conversation-form" onSubmit={handleSend}>
        <input
          type="text"
          placeholder={`Message ${conversation.otherUserName}`}
          value={text}
          onChange={(e) => setText(e.target.value)}
          maxLength={1000}
        />
        <button className="btn btn-primary" type="submit" disabled={sending}>
          {sending ? "Sending..." : "Send"}
        </button>
      </form>
    </div>
  );
}

export default Conversation;
