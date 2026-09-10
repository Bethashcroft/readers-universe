import { useState, useEffect, useRef, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import {
  getNotifications,
  getNotificationCount,
  markNotificationsRead,
  clearNotifications,
} from "../api/notifications";
import type { NotificationResponse } from "../api/notifications";
import { getChatConnection } from "../realtime/connection";
import { API_ORIGIN } from "../api/client";
import "./NotificationBell.css";

const messages: Record<NotificationResponse["type"], string> = {
  "new-follower": "started following you",
  "follow-requested": "asked to follow you",
  "follow-approved": "accepted your follow request",
};

function timeAgo(date: string) {
  const seconds = Math.floor((Date.now() - new Date(date).getTime()) / 1000);

  if (seconds < 60) return "just now";
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
  if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
  if (seconds < 604800) return `${Math.floor(seconds / 86400)}d ago`;

  return new Date(date).toLocaleDateString("en-GB", {
    day: "numeric",
    month: "short",
  });
}

function NotificationBell() {
  const navigate = useNavigate();
  const [items, setItems] = useState<NotificationResponse[]>([]);
  const [count, setCount] = useState(0);
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLLIElement>(null);

  const refreshCount = useCallback(async () => {
    try {
      const { count } = await getNotificationCount();
      setCount(count);
    } catch (err) {
      console.error("Failed to load notification count:", err);
    }
  }, []);

  useEffect(() => {
    const conn = getChatConnection();
    const handleNotification = () => {
      refreshCount();
    };

    const load = async () => {
      await refreshCount();
    };

    load();
    conn.on("NotificationReceived", handleNotification);

    return () => {
      conn.off("NotificationReceived", handleNotification);
    };
  }, [refreshCount]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const handlePointerDown = (event: MouseEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    };

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setOpen(false);
      }
    };

    document.addEventListener("mousedown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("mousedown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [open]);

  const handleToggle = async () => {
    const next = !open;
    setOpen(next);

    if (!next) {
      return;
    }

    try {
      const list = await getNotifications();
      setItems(list);

      if (list.some((n) => !n.isRead)) {
        await markNotificationsRead();
        setCount(0);
      }
    } catch (err) {
      console.error("Failed to load notifications:", err);
    }
  };

  const handleClear = async () => {
    try {
      await clearNotifications();
      setItems([]);
      setCount(0);
    } catch (err) {
      console.error("Failed to clear notifications:", err);
    }
  };

  const goTo = (notification: NotificationResponse) => {
    setOpen(false);
    navigate(
      notification.type === "follow-requested"
        ? "/requests"
        : `/profile/${notification.actorUserName}`,
    );
  };

  return (
    <li className="nav-menu" ref={containerRef}>
      <button
        type="button"
        className="notification-trigger"
        aria-haspopup="true"
        aria-expanded={open}
        aria-label={
          count > 0 ? `Notifications, ${count} unread` : "Notifications"
        }
        onClick={handleToggle}
      >
        <svg className="notification-icon" viewBox="0 0 24 24" aria-hidden="true">
          <path d="M18 8a6 6 0 0 0-12 0c0 7-3 8-3 8h18s-3-1-3-8" />
          <path d="M13.7 21a2 2 0 0 1-3.4 0" />
        </svg>
        {count > 0 && <span className="nav-badge">{count}</span>}
      </button>

      {open && (
        <div className="nav-menu-panel notification-panel">
          <div className="notification-header">
            <span>Notifications</span>
            {items.length > 0 && (
              <button
                type="button"
                className="notification-clear"
                onClick={handleClear}
              >
                Clear all
              </button>
            )}
          </div>
          {items.length === 0 ? (
            <p className="notification-empty">Nothing new yet.</p>
          ) : (
            items.map((notification) => (
              <button
                key={notification.id}
                type="button"
                className={`notification-item ${
                  notification.isRead ? "" : "notification-unread"
                }`}
                onClick={() => goTo(notification)}
              >
                <span className="notification-avatar">
                  {notification.actorAvatarUrl ? (
                    <img
                      src={`${API_ORIGIN}${notification.actorAvatarUrl}`}
                      alt=""
                    />
                  ) : (
                    notification.actorDisplayName.charAt(0)
                  )}
                </span>
                <span className="notification-text">
                  <span>
                    <strong>{notification.actorDisplayName}</strong>{" "}
                    {messages[notification.type]}
                  </span>
                  <span className="notification-time">
                    {timeAgo(notification.date)}
                  </span>
                </span>
              </button>
            ))
          )}
        </div>
      )}
    </li>
  );
}

export default NotificationBell;
