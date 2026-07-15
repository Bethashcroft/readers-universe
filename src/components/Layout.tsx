import { useState, useEffect } from "react";
import { Outlet, Link, NavLink, useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "../context/useAuth";
import { getMyRequests } from "../api/borrow";
import { getUnreadCount } from "../api/messages";
import {
  getChatConnection,
  startChatConnection,
  stopChatConnection,
} from "../realtime/connection";
import "./Layout.css";

function Layout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [pendingCount, setPendingCount] = useState(0);
  const [unreadCount, setUnreadCount] = useState(0);

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
        setUnreadCount((await getUnreadCount()).count);
      } catch (err) {
        console.error("Failed to load nav counts:", err);
      }
    };

    fetchCounts();
  }, [user, location.pathname]);

  useEffect(() => {
    if (!user) {
      stopChatConnection();
      return;
    }

    let active = true;

    const handleMessageReceived = () => {
      setUnreadCount((count) => count + 1);
    };

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
      getChatConnection().off("MessageReceived", handleMessageReceived);
    };
  }, [user]);

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
          <ellipse cx="65" cy="64" rx="56" ry="17" transform="rotate(-18 65 64)" />
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
        <ul className="navbar-links">
          {user ? (
            <>
              <li>
                <NavLink to="/shelves">My Shelves</NavLink>
              </li>
              <li>
                <NavLink to="/add-book">Add Book</NavLink>
              </li>
              <li>
                <NavLink to="/browse">Browse</NavLink>
              </li>
              <li>
                <NavLink to="/requests">
                  Requests
                  {user && pendingCount > 0 && (
                    <span className="nav-badge">{pendingCount}</span>
                  )}
                  {user && unreadCount > 0 && (
                    <span className="nav-badge nav-badge-messages">
                      {unreadCount}
                    </span>
                  )}
                </NavLink>
              </li>
              <li>
                <NavLink to={`/profile/${user.userName}`}>Profile</NavLink>
              </li>
              <li>
                <button className="nav-logout" onClick={handleLogout}>
                  Logout
                </button>
              </li>
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
