import { useState, useEffect, useCallback } from "react";
import { Link } from "react-router-dom";
import { getTrustedReaders, untrustReader } from "../api/trust";
import type { TrustedReaderResponse } from "../api/trust";
import ReaderRow from "../components/ReaderRow";
import ErrorState from "../components/ErrorState";
import ConfirmDialog from "../components/ConfirmDialog";
import { usePageTitle } from "../hooks/usePageTitle";
import "./TrustedBookClub.css";

function TrustedBookClub() {
  usePageTitle("Trusted Book Club");
  const [readers, setReaders] = useState<TrustedReaderResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [pending, setPending] = useState<TrustedReaderResponse | null>(null);
  const [removing, setRemoving] = useState(false);
  const [removeError, setRemoveError] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    setLoadError(false);

    try {
      setReaders(await getTrustedReaders());
    } catch (err) {
      console.error("Failed to load Trusted Book Club:", err);
      setLoadError(true);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const handleRemove = async () => {
    if (!pending) return;

    setRemoving(true);
    setRemoveError("");

    try {
      await untrustReader(pending.userName);
      setReaders((current) =>
        current.filter((r) => r.userName !== pending.userName),
      );
      setPending(null);
    } catch (err) {
      setRemoveError(
        err instanceof Error ? err.message : "Something went wrong. Try again.",
      );
    } finally {
      setRemoving(false);
    }
  };

  if (loading) {
    return <p>Loading your Trusted Book Club...</p>;
  }

  if (loadError) {
    return (
      <ErrorState
        message="We couldn't load your Trusted Book Club. Check your connection and try again."
        onRetry={load}
      />
    );
  }

  return (
    <div className="club">
      <h1>Trusted Book Club</h1>
      <p className="club-subtitle">
        Readers in your club can ask to borrow your books. Add people from
        their profile.
      </p>

      {readers.length === 0 ? (
        <div className="club-empty">
          <p>Nobody in your club yet.</p>
          <Link className="btn btn-primary" to="/readers">
            Find readers
          </Link>
        </div>
      ) : (
        <div className="reader-rows">
          {readers.map((reader) => (
            <ReaderRow
              key={reader.userName}
              userName={reader.userName}
              displayName={reader.displayName}
              avatarUrl={reader.avatarUrl}
              bio={reader.bio}
              action={
                <button
                  type="button"
                  className="btn btn-danger"
                  onClick={() => {
                    setRemoveError("");
                    setPending(reader);
                  }}
                >
                  Remove
                </button>
              }
            />
          ))}
        </div>
      )}

      {pending && (
        <ConfirmDialog
          title="Remove from your Trusted Book Club?"
          confirmLabel="Remove"
          busy={removing}
          error={removeError}
          onConfirm={handleRemove}
          onCancel={() => setPending(null)}
        >
          <p>
            <strong>{pending.displayName}</strong> will no longer be able to
            request to borrow your books.
          </p>
          <p>They won't be notified.</p>
        </ConfirmDialog>
      )}
    </div>
  );
}

export default TrustedBookClub;
