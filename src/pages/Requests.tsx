import { useState, useEffect, useCallback } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../context/useAuth";
import {
  getMyRequests,
  updateBorrowStatus,
  withdrawBorrowRequest,
} from "../api/borrow";
import type { BorrowRequestResponse, BorrowStatus } from "../api/borrow";
import { usePageTitle } from "../hooks/usePageTitle";
import "./Requests.css";

function Requests() {
  usePageTitle("Requests");
  const { user } = useAuth();
  const [requests, setRequests] = useState<BorrowRequestResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [updatingId, setUpdatingId] = useState<number | null>(null);

  const fetchRequests = useCallback(async () => {
    try {
      const data = await getMyRequests();
      setRequests(data);
    } catch (err) {
      console.error("Failed to fetch requests:", err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchRequests();
  }, [fetchRequests]);

  const incoming = requests.filter((r) => r.toUserId === user?.userId);
  const outgoing = requests.filter((r) => r.fromUserId === user?.userId);

  const handleStatusChange = async (id: number, status: BorrowStatus) => {
    setError("");
    setUpdatingId(id);

    try {
      await updateBorrowStatus(id, status);
      await fetchRequests();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update request");
    } finally {
      setUpdatingId(null);
    }
  };

  const handleWithdraw = async (id: number) => {
    setError("");
    setUpdatingId(id);

    try {
      await withdrawBorrowRequest(id);
      await fetchRequests();
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to withdraw request",
      );
    } finally {
      setUpdatingId(null);
    }
  };

  const formatDate = (date: string) =>
    new Date(date).toLocaleDateString("en-GB", {
      day: "numeric",
      month: "short",
      year: "numeric",
    });

  if (loading) {
    return <p>Loading requests...</p>;
  }

  return (
    <div className="requests">
      <h1>Borrow Requests</h1>
      {error && <p className="form-error">{error}</p>}

      <section className="requests-section">
        <h2>Incoming ({incoming.length})</h2>
        {incoming.length === 0 && (
          <p className="empty-requests">No one has requested your books yet.</p>
        )}
        {incoming.map((req) => (
          <div key={req.id} className="request-card">
            <div className="request-details">
              <h3>
                <Link to={`/book/${req.bookId}`}>{req.bookTitle}</Link>
              </h3>
              <p className="request-from">Requested by {req.fromUserName}</p>
              {req.message && (
                <p className="request-message">"{req.message}"</p>
              )}
              <p className="request-date">{formatDate(req.date)}</p>
              <Link className="request-chat" to={`/messages/${req.id}`}>
                Open chat →
              </Link>
            </div>
            {req.status === "pending" ? (
              <div className="request-actions">
                <button
                  className="btn btn-primary"
                  onClick={() => handleStatusChange(req.id, "accepted")}
                  disabled={updatingId === req.id}
                >
                  Accept
                </button>
                <button
                  className="btn btn-secondary"
                  onClick={() => handleStatusChange(req.id, "declined")}
                  disabled={updatingId === req.id}
                >
                  Decline
                </button>
              </div>
            ) : (
              <span className={`status-badge ${req.status}`}>{req.status}</span>
            )}
          </div>
        ))}
      </section>

      <section className="requests-section">
        <h2>Outgoing ({outgoing.length})</h2>
        {outgoing.length === 0 && (
          <p className="empty-requests">You haven't requested any books yet.</p>
        )}
        {outgoing.map((req) => (
          <div key={req.id} className="request-card">
            <div className="request-details">
              <h3>
                <Link to={`/book/${req.bookId}`}>{req.bookTitle}</Link>
              </h3>
              {req.message && (
                <p className="request-message">"{req.message}"</p>
              )}
              <p className="request-date">{formatDate(req.date)}</p>
              <Link className="request-chat" to={`/messages/${req.id}`}>
                Open chat →
              </Link>
            </div>
            <div className="request-actions">
              <span className={`status-badge ${req.status}`}>{req.status}</span>
              {req.status === "pending" && (
                <button
                  className="btn btn-secondary"
                  onClick={() => handleWithdraw(req.id)}
                  disabled={updatingId === req.id}
                >
                  Withdraw
                </button>
              )}
            </div>
          </div>
        ))}
      </section>
    </div>
  );
}

export default Requests;
