import { useState, useEffect } from "react";
import { getReadings, editReading, deleteReading } from "../api/books";
import type { LibraryEntryResponse, ReadingResponse } from "../api/books";
import { formatDay, today } from "../utils/dates";
import ConfirmDialog from "./ConfirmDialog";
import "./ReadingHistory.css";

type ReadingHistoryProps = {
  entry: LibraryEntryResponse;
  onChanged: (changes: {
    timesRead: number;
    finishedDate: string | null;
  }) => void;
};

function ReadingHistory({ entry, onChanged }: ReadingHistoryProps) {
  const [readings, setReadings] = useState<ReadingResponse[] | null>(null);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editDay, setEditDay] = useState("");
  const [deleting, setDeleting] = useState<ReadingResponse | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  const upToDate = readings !== null && readings.length === entry.timesRead;

  useEffect(() => {
    if (upToDate) return;

    let active = true;

    const load = async () => {
      try {
        const found = await getReadings(entry.id);
        if (active) setReadings(found);
      } catch (err) {
        if (active) {
          setError(err instanceof Error ? err.message : "Failed to load");
        }
      }
    };

    load();

    return () => {
      active = false;
    };
  }, [entry.id, upToDate]);

  const settle = (found: ReadingResponse[]) => {
    setReadings(found);
    setEditingId(null);
    setDeleting(null);
    onChanged({
      timesRead: found.length,
      finishedDate: found[0]?.finishedDate ?? null,
    });
  };

  const startEditing = (reading: ReadingResponse) => {
    setEditingId(reading.id);
    setEditDay(reading.finishedDate.slice(0, 10));
    setError("");
  };

  const handleSave = async () => {
    if (editingId === null || !editDay) return;
    setBusy(true);
    setError("");

    try {
      settle(await editReading(editingId, editDay));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save");
    } finally {
      setBusy(false);
    }
  };

  const handleDelete = async () => {
    if (!deleting) return;
    setBusy(true);
    setError("");

    try {
      settle(await deleteReading(deleting.id));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete");
    } finally {
      setBusy(false);
    }
  };

  if (readings === null) {
    return error ? <p className="form-error">{error}</p> : null;
  }

  if (readings.length === 0) {
    return null;
  }

  return (
    <div className="reading-history">
      <h3 className="eyebrow">Finished</h3>

      <ul className="reading-history-list">
        {readings.map((reading) => (
          <li key={reading.id} className="reading-history-row">
            {editingId === reading.id ? (
              <>
                <input
                  type="date"
                  aria-label="Date you finished"
                  value={editDay}
                  min="1900-01-01"
                  max={today()}
                  onChange={(e) => setEditDay(e.target.value)}
                />
                <button
                  type="button"
                  className="btn btn-primary"
                  onClick={handleSave}
                  disabled={busy || !editDay}
                >
                  Save
                </button>
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setEditingId(null)}
                  disabled={busy}
                >
                  Cancel
                </button>
              </>
            ) : (
              <>
                <span className="reading-history-day">
                  {formatDay(reading.finishedDate)}
                </span>
                <button
                  type="button"
                  className="reading-history-action"
                  aria-label={`Edit ${formatDay(reading.finishedDate)}`}
                  onClick={() => startEditing(reading)}
                  disabled={busy}
                >
                  Edit
                </button>
                <button
                  type="button"
                  className="reading-history-action reading-history-delete"
                  aria-label={`Delete ${formatDay(reading.finishedDate)}`}
                  onClick={() => setDeleting(reading)}
                  disabled={busy}
                >
                  Delete
                </button>
              </>
            )}
          </li>
        ))}
      </ul>

      {readings.length > 1 && (
        <p className="reading-history-count">Read {readings.length} times</p>
      )}

      {error && <p className="form-error">{error}</p>}

      {deleting && (
        <ConfirmDialog
          title="Delete this finish?"
          confirmLabel="Delete"
          busy={busy}
          onConfirm={handleDelete}
          onCancel={() => setDeleting(null)}
        >
          <p>
            {formatDay(deleting.finishedDate)} will come off your reading
            history and your feed.
          </p>
        </ConfirmDialog>
      )}
    </div>
  );
}

export default ReadingHistory;
