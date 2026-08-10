import { useRef, useState } from "react";
import { Link } from "react-router-dom";
import { importLibrary } from "../api/import";
import type { ImportSummary } from "../api/import";
import { shelfLabels } from "../types/book";
import type { ShelfType } from "../types/book";
import "./ImportLibrary.css";

type Stage = "idle" | "checking" | "ready" | "importing" | "done";

function ImportLibrary() {
  const fileInput = useRef<HTMLInputElement>(null);

  const [file, setFile] = useState<File | null>(null);
  const [stage, setStage] = useState<Stage>("idle");
  const [preview, setPreview] = useState<ImportSummary | null>(null);
  const [result, setResult] = useState<ImportSummary | null>(null);
  const [error, setError] = useState("");

  const reset = () => {
    setFile(null);
    setPreview(null);
    setResult(null);
    setStage("idle");
    setError("");
    if (fileInput.current) {
      fileInput.current.value = "";
    }
  };

  const handleFileChange = (chosen: File | null) => {
    setFile(chosen);
    setPreview(null);
    setError("");
    setStage("idle");
  };

  const handleCheck = async () => {
    if (!file) return;
    setStage("checking");
    setError("");

    try {
      setPreview(await importLibrary(file, true));
      setStage("ready");
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "We could not read that file.",
      );
      setStage("idle");
    }
  };

  const handleImport = async () => {
    if (!file) return;
    setStage("importing");
    setError("");

    try {
      const done = await importLibrary(file, false);
      setResult(done);
      setPreview(null);
      setStage("done");
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "The import did not finish.",
      );
      setStage("ready");
    }
  };

  return (
    <section className="import-library">
      <h2>Bring your library with you</h2>
      <p className="import-intro">
        Already keep your books on Goodreads? Export your library there under My
        Books, Import and Export, then drop the CSV here.
      </p>

      {error && <p className="form-error">{error}</p>}

      {result && (
        <div className="import-done">
          <p className="import-headline">
            Added <strong>{result.added}</strong>{" "}
            {result.added === 1 ? "book" : "books"} to your shelves
            {result.reviewsAdded > 0 && (
              <>
                , along with <strong>{result.reviewsAdded}</strong> of your
                reviews
              </>
            )}
            .
          </p>
          {result.alreadyOnShelves > 0 && (
            <p className="import-note">
              {result.alreadyOnShelves}{" "}
              {result.alreadyOnShelves === 1
                ? "book was already on your shelves, so we left it alone."
                : "books were already on your shelves, so we left them alone."}
            </p>
          )}
          <div className="import-actions">
            <Link className="btn btn-primary" to="/shelves">
              Go to my shelves
            </Link>
            <button type="button" className="btn btn-secondary" onClick={reset}>
              Import another file
            </button>
          </div>
        </div>
      )}

      {!result && (
        <div className="import-controls">
          <input
            ref={fileInput}
            type="file"
            id="import-file"
            accept=".csv,text/csv"
            onChange={(e) => handleFileChange(e.target.files?.[0] ?? null)}
          />
          <button
            type="button"
            className="btn btn-secondary"
            onClick={handleCheck}
            disabled={!file || stage === "checking" || stage === "importing"}
          >
            {stage === "checking" ? "Reading your file…" : "Check the file"}
          </button>
        </div>
      )}

      {preview && (
        <div className="import-preview">
          <p className="import-headline">
            Found <strong>{preview.rowsFound}</strong>{" "}
            {preview.rowsFound === 1 ? "book" : "books"} in your{" "}
            {preview.service} export.
          </p>

          <ul className="import-facts">
            <li>
              <strong>{preview.added}</strong> to add to your shelves
            </li>
            {preview.alreadyOnShelves > 0 && (
              <li>
                <strong>{preview.alreadyOnShelves}</strong> already on your
                shelves, which we will leave alone
              </li>
            )}
            {preview.reviewsAdded > 0 && (
              <li>
                <strong>{preview.reviewsAdded}</strong> of your reviews and
                ratings coming with them
              </li>
            )}
            {Object.entries(preview.byShelf).map(([shelf, count]) => (
              <li key={shelf}>
                <strong>{count}</strong> going to{" "}
                {shelfLabels[shelf as ShelfType] ?? shelf}
              </li>
            ))}
            {preview.skippedRows > 0 && (
              <li>
                <strong>{preview.skippedRows}</strong> rows we could not read
              </li>
            )}
          </ul>

          {preview.sample.length > 0 && (
            <p className="import-sample">
              Starting with {preview.sample.slice(0, 3).join(", ")}
              {preview.rowsFound > 3 ? " and more" : ""}.
            </p>
          )}

          <div className="import-actions">
            <button
              type="button"
              className="btn btn-primary"
              onClick={handleImport}
              disabled={stage === "importing" || preview.added === 0}
            >
              {stage === "importing"
                ? "Adding your books…"
                : `Import ${preview.added} ${preview.added === 1 ? "book" : "books"}`}
            </button>
            <button type="button" className="btn btn-secondary" onClick={reset}>
              Cancel
            </button>
          </div>

          <p className="import-note">
            Nothing has been added yet. Your books go to your shelves only, and
            none of them are offered to borrow or for sale until you say so.
          </p>
        </div>
      )}
    </section>
  );
}

export default ImportLibrary;
