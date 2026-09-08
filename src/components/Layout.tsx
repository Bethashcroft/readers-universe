import {
  useState,
  useEffect,
  useRef,
  useCallback,
  type ReactNode,
} from "react";
import {
  Outlet,
  Link,
  NavLink,
  useNavigate,
  useLocation,
} from "react-router-dom";
import { useAuth } from "../context/useAuth";
import { getMyRequests } from "../api/borrow";
import { getUnreadCount, messagesReadEvent } from "../api/messages";
import {
  getChatConnection,
  startChatConnection,
  stopChatConnection,
} from "../realtime/connection";
import ReaderSearchBar from "./ReaderSearchBar";
import "./Layout.css";

type NavMenuProps = {
  label: string;
  badgeCount?: number;
  children: ReactNode;
};

function NavMenu({ label, badgeCount = 0, children }: NavMenuProps) {
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLLIElement>(null);

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

  return (
    <li className="nav-menu" ref={containerRef}>
      <button
        type="button"
        className="nav-menu-trigger"
        aria-haspopup="true"
        aria-expanded={open}
        onClick={() => setOpen((value) => !value)}
      >
        {label}
        {badgeCount > 0 && <span className="nav-badge">{badgeCount}</span>}
        <svg className="nav-menu-chevron" viewBox="0 0 12 8" aria-hidden="true">
          <path d="M1 1.5 L6 6.5 L11 1.5" />
        </svg>
      </button>
      {open && (
        <div className="nav-menu-panel" onClick={() => setOpen(false)}>
          {children}
        </div>
      )}
    </li>
  );
}

function Layout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [pendingCount, setPendingCount] = useState(0);
  const [unreadCount, setUnreadCount] = useState(0);
  const unreadRequest = useRef(0);

  const refreshUnreadCount = useCallback(async () => {
    const ticket = ++unreadRequest.current;

    try {
      const { count } = await getUnreadCount();

      if (ticket === unreadRequest.current) {
        setUnreadCount(count);
      }
    } catch (err) {
      console.error("Failed to refresh unread count:", err);
    }
  }, []);

  useEffect(() => {
    if (!user) {
      return;
    }

    const fetchCounts = async () => {
      try {
        const requests = await getMyRequests();
        const incomingPending = requests.filter(
          (r) => r.toUserId === user.userId && r.status === "pending",
        );
        setPendingCount(incomingPending.length);
        await refreshUnreadCount();
      } catch (err) {
        console.error("Failed to load nav counts:", err);
      }
    };

    fetchCounts();
  }, [user, location.pathname, refreshUnreadCount]);

  useEffect(() => {
    if (!user) {
      stopChatConnection();
      return;
    }

    let active = true;

    const handleMessageReceived = () => {
      unreadRequest.current++;
      setUnreadCount((count) => count + 1);
    };

    const handleMessagesRead = () => {
      refreshUnreadCount();
    };

    window.addEventListener(messagesReadEvent, handleMessagesRead);

    const connect = async () => {
      try {
        const conn = await startChatConnection();
        if (!active) return;
        conn.on("MessageReceived", handleMessageReceived);
      } catch (err) {
        console.error("Failed to connect to live updates:", err);
      }
    };

    connect();

    return () => {
      active = false;
      window.removeEventListener(messagesReadEvent, handleMessagesRead);
      getChatConnection().off("MessageReceived", handleMessageReceived);
    };
  }, [user, refreshUnreadCount]);

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <div className="layout">
      <nav className="navbar" aria-label="Primary">
        <svg
          className="nav-decor nav-decor-planet"
          viewBox="0 0 130 130"
          strokeWidth="4"
          aria-hidden="true"
        >
          <circle cx="65" cy="58" r="32" />
          <ellipse
            cx="65"
            cy="64"
            rx="56"
            ry="17"
            transform="rotate(-18 65 64)"
          />
        </svg>
        <svg
          className="nav-decor nav-decor-star nav-decor-star-1"
          viewBox="0 0 40 40"
          strokeWidth="3"
          aria-hidden="true"
        >
          <path d="M20 3 L23.5 16.5 L37 20 L23.5 23.5 L20 37 L16.5 23.5 L3 20 L16.5 16.5 Z" />
        </svg>
        <svg
          className="nav-decor nav-decor-star nav-decor-star-2"
          viewBox="0 0 40 40"
          strokeWidth="3"
          aria-hidden="true"
        >
          <path d="M20 3 L23.5 16.5 L37 20 L23.5 23.5 L20 37 L16.5 23.5 L3 20 L16.5 16.5 Z" />
        </svg>
        <Link to="/" className="navbar-brand">
          The Readers Universe
        </Link>
        {user && <ReaderSearchBar />}
        <ul className="navbar-links">
          {user ? (
            <>
              <NavMenu label="Library" badgeCount={pendingCount + unreadCount}>
                <NavLink to="/shelves">My Shelves</NavLink>
                <NavLink to="/add-book">Add Books</NavLink>
                <NavLink to="/browse">Browse</NavLink>
                <NavLink to="/readers">Find Readers</NavLink>
                <NavLink to="/requests">
                  Requests
                  {pendingCount > 0 && (
                    <span className="nav-badge">{pendingCount}</span>
                  )}
                  {unreadCount > 0 && (
                    <span className="nav-badge nav-badge-messages">
                      {unreadCount}
                    </span>
                  )}
                </NavLink>
              </NavMenu>
              <NavMenu label="Profile">
                <NavLink to={`/profile/${user.userName}`}>Profile</NavLink>
                <button className="nav-logout" onClick={handleLogout}>
                  Log Out
                </button>
              </NavMenu>
            </>
          ) : (
            <>
              <li>
                <NavLink to="/login">Login</NavLink>
              </li>
              <li>
                <NavLink to="/register">Register</NavLink>
              </li>
            </>
          )}
        </ul>
      </nav>
      <main className="main-content">
        <Outlet />
      </main>
      <footer className="footer">
        <div className="footer-inner">
          <span>© {new Date().getFullYear()} The Readers Universe</span>
          <span className="footer-made">
            Made for book lovers
            <svg
              className="footer-book"
              viewBox="0 0 130 100"
              aria-hidden="true"
            >
              <path d="M65 22 C 48 11, 22 11, 9 19 L 9 78 C 22 70, 48 70, 65 80" />
              <path d="M65 22 C 82 11, 108 11, 121 19 L 121 78 C 108 70, 82 70, 65 80" />
              <line x1="65" y1="22" x2="65" y2="80" />
            </svg>
          </span>
        </div>
      </footer>
    </div>
  );
}

export default Layout;
