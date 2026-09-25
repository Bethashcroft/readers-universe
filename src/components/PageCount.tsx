import { useState } from "react";
import { updateProgress } from "../api/books";
import type { LibraryEntryResponse } from "../api/books";
import "./PageCount.css";

const digitsOnly = (value: string) => value.replace(/\D/g, "").slice(0, 5);

type PageCountProps = {
  entry: LibraryEntryResponse;
  onSaved: (updated: LibraryEntryResponse) => void;
};

function PageCount({ entry, onSaved }: PageCountProps) {
  const [editing, setEditing] = useState(false);
  const [count, setCount] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const startEditing = () => {
    setCount(entry.pageCount === null ? "" : String(entry.pageCount));
    setEditing(true);
    setError("");
  };

  const handleSave = async () => {
    const pageCount = count === "" ? null : Number(count);
    const pastTheEnd =
      entry.page !== null && pageCount !== null && entry.page > pageCount;

    setSaving(true);
    setError("");

    try {
      onSaved(
        await updateProgress(entry.id, {
          page: pastTheEnd ? null : entry.page,
          pageCount,
        }),
      );
      setEditing(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="page-count">
      <h3 className="eyebrow">Pages</h3>

      {editing ? (
        <div className="page-count-row">
          <input
            className="page-count-input"
            type="text"
            inputMode="numeric"
            aria-label="Pages in your copy"
            value={count}
            onChange={(e) => setCount(digitsOnly(e.target.value))}
          />
          <button
            type="button"
            className="btn btn-primary"
            onClick={handleSave}
            disabled={saving}
          >
            {saving ? "Saving..." : "Save"}
          </button>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => setEditing(false)}
            disabled={saving}
          >
            Cancel
          </button>
        </div>
      ) : (
        <div className="page-count-row">
          <span className="page-count-value">
            {entry.pageCount ?? "Not set"}
          </span>
          <button
            type="button"
            className="page-count-edit"
            aria-label="Edit page count"
            onClick={startEditing}
          >
            Edit
          </button>
        </div>
      )}

      {error && <p className="form-error">{error}</p>}
    </div>
  );
}

export default PageCount;
