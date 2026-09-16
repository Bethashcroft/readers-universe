import { useState } from "react";
import { updateProgress } from "../api/books";
import type { LibraryEntryResponse } from "../api/books";
import { percentThrough } from "../utils/progress";
import "./ReadingProgress.css";

type ReadingProgressProps = {
  entry: LibraryEntryResponse;
  onSaved: (updated: LibraryEntryResponse) => void;
};

function ReadingProgress({ entry, onSaved }: ReadingProgressProps) {
  const [page, setPage] = useState(String(entry.page ?? ""));
  const [count, setCount] = useState(String(entry.pageCount ?? ""));
  const [editingCount, setEditingCount] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const savedPage = String(entry.page ?? "");
  const savedCount = String(entry.pageCount ?? "");
  const changed = page !== savedPage || count !== savedCount;
  const countKnown = entry.pageCount !== null;
  const percent = percentThrough(entry.page, entry.pageCount);

  const handleSave = async () => {
    setSaving(true);
    setError("");

    try {
      const updated = await updateProgress(entry.id, {
        page: page === "" ? null : Number(page),
        pageCount: count === "" ? null : Number(count),
      });
      onSaved(updated);
      setEditingCount(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save");
    } finally {
      setSaving(false);
    }
  };

  const handleCancel = () => {
    setPage(savedPage);
    setCount(savedCount);
    setEditingCount(false);
    setError("");
  };

  return (
    <div className="reading-progress">
      <h3 className="eyebrow">Your progress</h3>

      {percent !== null && (
        <div className="progress-bar" role="progressbar" aria-valuenow={percent}>
          <div className="progress-bar-fill" style={{ width: `${percent}%` }} />
          <span className="progress-bar-label">{percent}%</span>
        </div>
      )}

      <div className="progress-row">
        <label htmlFor="progress-page">Page</label>
        <input
          id="progress-page"
          type="number"
          min={0}
          inputMode="numeric"
          value={page}
          onChange={(e) => setPage(e.target.value)}
        />
        <span>out of</span>
        {editingCount || !countKnown ? (
          <input
            type="number"
            min={1}
            inputMode="numeric"
            aria-label="Pages in your copy"
            value={count}
            onChange={(e) => setCount(e.target.value)}
          />
        ) : (
          <span className="progress-count">{entry.pageCount}</span>
        )}
      </div>

      <p className="progress-note">
        {countKnown
          ? "Page count can be edited. Edited versions of the page count are for your copies only."
          : "We couldn't find a page count for this book. Type the pages in your copy."}
      </p>

      {error && <p className="form-error">{error}</p>}

      <div className="progress-actions">
        {countKnown && !editingCount && (
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => setEditingCount(true)}
          >
            Edit
          </button>
        )}
        {changed && (
          <button
            type="button"
            className="btn btn-primary"
            onClick={handleSave}
            disabled={saving}
          >
            {saving ? "Saving..." : "Update"}
          </button>
        )}
        {(changed || editingCount) && (
          <button
            type="button"
            className="btn btn-secondary"
            onClick={handleCancel}
            disabled={saving}
          >
            Cancel
          </button>
        )}
      </div>
    </div>
  );
}

export default ReadingProgress;
