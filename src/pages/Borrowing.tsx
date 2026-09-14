import { useState, useEffect, useCallback } from "react";
import { Link } from "react-router-dom";
import {
  getBorrowing,
  updateBorrowStatus,
  withdrawBorrowRequest,
  markReturned,
} from "../api/borrow";
import type {
  BorrowingResponse,
  BorrowRequestResponse,
  OfferedBookResponse,
} from "../api/borrow";
import { takeBackBook } from "../api/books";
import { useAuth } from "../context/useAuth";
import { statusBadgeClass } from "../types/book";
import BookCover from "../components/BookCover";
import OfferBookPicker from "../components/OfferBookPicker";
import RequestCard from "../components/RequestCard";
import ErrorState from "../components/ErrorState";
import { usePageTitle } from "../hooks/usePageTitle";
import "./Borrowing.css";

function Borrowing() {
  usePageTitle("Borrowing");
  const { user } = useAuth();
  const [data, setData] = useState<BorrowingResponse | null>(null);
  const [loadError, setLoadError] = useState(false);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState("");
  const [picking, setPicking] = useState(false);
  const [showHistory, setShowHistory] = useState(false);

  const load = useCallback(async () => {
    setLoadError(false);

    try {
      setData(await getBorrowing());
    } catch (err) {
      console.error("Failed to load borrowing:", err);
      setLoadError(true);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const run = async (key: string, action: () => Promise<unknown>) => {
    setError("");
    setBusy(key);

    try {
      await action();
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Something went wrong");
    } finally {
      setBusy("");
    }
  };

  const slotKey = (slot: OfferedBookResponse) => `entry-${slot.libraryEntryId}`;
  const requestKey = (request: BorrowRequestResponse) => `request-${request.id}`;

  if (loadError && !data) {
    return (
      <ErrorState
        message="We couldn't load your borrowing. Check your connection and try again."
        onRetry={load}
      />
    );
  }

  if (!data) {
    return <p>Loading your borrowing...</p>;
  }

  const slotCount = Math.max(data.limit, data.offering.length);
  const slots = Array.from(
    { length: slotCount },
    (_, index) => data.offering[index] ?? null,
  );

  return (
    <div className="borrowing">
      <h1>Borrowing</h1>
      {(error || loadError) && (
        <p className="form-error">
          {error || "We couldn't refresh this page. Try again in a moment."}
        </p>
      )}

      <section className="borrowing-section">
        <div className="borrowing-heading">
          <h2>Offering</h2>
          <span className="borrowing-count">
            {data.offering.length} of {data.limit}
          </span>
        </div>
        <p className="borrowing-hint">
          Books your Trusted Book Club can ask to borrow.
        </p>

        <div className="offer-slots">
          {slots.map((slot, index) =>
            slot ? (
              <div key={slot.libraryEntryId} className="offer-slot">
                <Link to={`/book/${slot.bookId}`} className="offer-slot-cover">
                  <BookCover src={slot.coverUrl} title={slot.title} />
                </Link>
                <div className="offer-slot-info">
                  <Link to={`/book/${slot.bookId}`} className="offer-slot-title">
                    {slot.title}
                  </Link>
                  <span className="offer-slot-author">{slot.author}</span>
                  {slot.borrower ? (
                    <span className="badge badge-amber">
                      With{" "}
                      <Link to={`/profile/${slot.borrower.userName}`}>
                        {slot.borrower.displayName}
                      </Link>
                    </span>
                  ) : slot.offer === "lent-out" ? (
                    <span className="badge badge-amber">Lent out</span>
                  ) : (
                    <span className="badge badge-green">Available</span>
                  )}
                  <div className="offer-slot-actions">
                    {slot.borrower ? (
                      <>
                        <button
                          className="btn btn-primary"
                          onClick={() =>
                            run(slotKey(slot), () =>
                              markReturned(slot.borrower!.requestId),
                            )
                          }
                          disabled={busy === slotKey(slot)}
                        >
                          Mark returned
                        </button>
                        <Link
                          className="btn btn-secondary"
                          to={`/messages/${slot.borrower.requestId}`}
                        >
                          Chat
                        </Link>
                      </>
                    ) : (
                      <button
                        className="btn btn-secondary"
                        onClick={() =>
                          run(slotKey(slot), () =>
                            takeBackBook(slot.libraryEntryId),
                          )
                        }
                        disabled={busy === slotKey(slot)}
                      >
                        Take back
                      </button>
                    )}
                  </div>
                </div>
              </div>
            ) : (
              <button
                key={`empty-${index}`}
                type="button"
                className="offer-slot offer-slot-empty"
                onClick={() => setPicking(true)}
              >
                <span className="offer-slot-plus" aria-hidden="true">
                  +
                </span>
                Offer a book
              </button>
            ),
          )}
        </div>
      </section>

      <section className="borrowing-section">
        <h2>Borrowed</h2>
        {data.borrowed.length === 0 ? (
          <p className="borrowing-empty">Nothing borrowed right now.</p>
        ) : (
          data.borrowed.map((request) => (
            <RequestCard
              key={request.id}
              request={request}
              from={`From ${request.toUserName}`}
              datePrefix="Since "
              showMessage={false}
            />
          ))
        )}
      </section>

      <section className="borrowing-section">
        <h2>Requests</h2>

        <h3 className="eyebrow">Incoming ({data.incoming.length})</h3>
        {data.incoming.length === 0 && (
          <p className="borrowing-empty">No one is waiting on you.</p>
        )}
        {data.incoming.map((request) => (
          <RequestCard
            key={request.id}
            request={request}
            from={`Requested by ${request.fromUserName}`}
            actions={
              <>
                <button
                  className="btn btn-primary"
                  onClick={() =>
                    run(requestKey(request), () =>
                      updateBorrowStatus(request.id, "accepted"),
                    )
                  }
                  disabled={busy === requestKey(request)}
                >
                  Accept
                </button>
                <button
                  className="btn btn-secondary"
                  onClick={() =>
                    run(requestKey(request), () =>
                      updateBorrowStatus(request.id, "declined"),
                    )
                  }
                  disabled={busy === requestKey(request)}
                >
                  Decline
                </button>
              </>
            }
          />
        ))}

        <h3 className="eyebrow">Outgoing ({data.outgoing.length})</h3>
        {data.outgoing.length === 0 && (
          <p className="borrowing-empty">You haven't asked for anything.</p>
        )}
        {data.outgoing.map((request) => (
          <RequestCard
            key={request.id}
            request={request}
            from={`From ${request.toUserName}`}
            actions={
              <button
                className="btn btn-secondary"
                onClick={() =>
                  run(requestKey(request), () =>
                    withdrawBorrowRequest(request.id),
                  )
                }
                disabled={busy === requestKey(request)}
              >
                Withdraw
              </button>
            }
          />
        ))}
      </section>

      {data.history.length > 0 && (
        <section className="borrowing-section">
          <button
            type="button"
            className="borrowing-history-toggle"
            aria-expanded={showHistory}
            onClick={() => setShowHistory((open) => !open)}
          >
            <h2>History</h2>
            <span className="borrowing-count">
              {data.history.length} {showHistory ? "shown" : "hidden"}
            </span>
          </button>
          {showHistory &&
            data.history.map((request) => (
              <RequestCard
                key={request.id}
                request={request}
                from={
                  request.fromUserId === user?.userId
                    ? `You asked ${request.toUserName}`
                    : `${request.fromUserName} asked you`
                }
                showMessage={false}
                actions={
                  <span className={statusBadgeClass(request.status)}>
                    {request.status}
                  </span>
                }
              />
            ))}
        </section>
      )}

      {picking && (
        <OfferBookPicker
          onClose={() => setPicking(false)}
          onOffered={() => {
            setPicking(false);
            load();
          }}
        />
      )}
    </div>
  );
}

export default Borrowing;
