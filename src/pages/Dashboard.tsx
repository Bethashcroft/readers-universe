import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../context/useAuth";
import { getDashboardSummary } from "../api/dashboard";
import type { DashboardSummary } from "../api/dashboard";
import { getFeed } from "../api/feed";
import ActivityFeed from "../components/ActivityFeed";
import { usePageTitle } from "../hooks/usePageTitle";
import "./Dashboard.css";

function Dashboard() {
  usePageTitle("Home");
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

  const stats = summary
    ? [
        {
          to: "/shelves",
          value: summary.myBooks,
          label: summary.myBooks === 1 ? "book" : "books",
        },
        { to: "/browse", value: summary.nearby, label: "to borrow" },
        {
          to: "/borrowing",
          value: summary.pendingRequests,
          label: summary.pendingRequests === 1 ? "request" : "requests",
        },
      ]
    : [];

  return (
    <div className="dashboard">
      <header className="dashboard-header">
        <h1>Welcome back, {user?.displayName}</h1>
      </header>

      <nav className="dashboard-strip" aria-label="At a glance">
        {stats.map((stat) => (
          <Link key={stat.to} to={stat.to} className="dashboard-stat">
            <strong>{stat.value}</strong> {stat.label}
          </Link>
        ))}
        <Link to="/add-book" className="dashboard-stat dashboard-stat-action">
          Add a book
        </Link>
      </nav>

      <section className="dashboard-feed">
        <h2 className="eyebrow">From people you follow</h2>
        <ActivityFeed
          load={getFeed}
          empty={
            <>
              <p>Follow some readers and their reading shows up here.</p>
              <Link className="btn btn-primary" to="/readers">
                Find readers
              </Link>
            </>
          }
        />
      </section>
    </div>
  );
}

export default Dashboard;
