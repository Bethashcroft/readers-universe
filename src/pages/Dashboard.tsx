import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../context/useAuth";
import { getDashboardSummary } from "../api/dashboard";
import type { DashboardSummary } from "../api/dashboard";
import { usePageTitle } from "../hooks/usePageTitle";
import "./Dashboard.css";

function Dashboard() {
  usePageTitle("Dashboard");
  const { user } = useAuth();
  const [summary, setSummary] = useState<DashboardSummary | null>(null);

  useEffect(() => {
    const loadSummary = async () => {
      try {
        setSummary(await getDashboardSummary());
      } catch (err) {
        console.error("Failed to load dashboard summary:", err);
      }
    };

    loadSummary();
  }, []);

  const actions = [
    {
      to: "/shelves",
      title: "My Shelves",
      text: "Organise everything you're reading, want to read, and have read.",
      stat:
        summary &&
        `${summary.myBooks} ${summary.myBooks === 1 ? "book" : "books"}`,
    },
    {
      to: "/browse",
      title: "Browse Nearby",
      text: "Discover books to borrow or buy from readers around you.",
      stat: summary && `${summary.nearby} available`,
    },
    {
      to: "/add-book",
      title: "Add a Book",
      text: "Put a new book on your shelves in seconds.",
      stat: null,
    },
    {
      to: "/requests",
      title: "Requests",
      text: "See who wants your books and track your own requests.",
      stat: summary && `${summary.pendingRequests} pending`,
    },
  ];

  return (
    <div className="dashboard">
      <header className="dashboard-header">
        <p className="dashboard-eyebrow">Welcome back</p>
        <h1>{user?.displayName}</h1>
        <p className="dashboard-sub">Where would you like to go?</p>
      </header>

      <div className="dashboard-grid">
        {actions.map((action) => (
          <Link key={action.to} to={action.to} className="dashboard-card">
            <h2>{action.title}</h2>
            <p>{action.text}</p>
            {action.stat && (
              <span className="dashboard-stat">{action.stat}</span>
            )}
          </Link>
        ))}
      </div>
    </div>
  );
}

export default Dashboard;
