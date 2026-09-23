import React, { useState, useEffect, useCallback } from "react";
import { getGoals, setGoal, removeGoal } from "../api/goals";
import type { GoalResponse } from "../api/goals";
import { scheduleMessage } from "../utils/goals";
import { usePageTitle } from "../hooks/usePageTitle";
import ConfirmDialog from "../components/ConfirmDialog";
import ErrorState from "../components/ErrorState";
import "./ReadingGoals.css";

const books = (n: number) => `${n} ${n === 1 ? "book" : "books"}`;
const digitsOnly = (value: string) => value.replace(/\D/g, "").slice(0, 4);
const mostBooks = 1000;

function ReadingGoals() {
  usePageTitle("Reading Goals");

  const thisYear = new Date().getFullYear();
  const [goals, setGoals] = useState<GoalResponse[] | null>(null);
  const [loadError, setLoadError] = useState(false);
  const [busy, setBusy] = useState(false);

  const [editingThisYear, setEditingThisYear] = useState(false);
  const [target, setTarget] = useState("");
  const [removing, setRemoving] = useState(false);
  const [error, setError] = useState("");

  const [editingPast, setEditingPast] = useState(false);
  const [drafts, setDrafts] = useState<Record<number, string>>({});
  const [pastError, setPastError] = useState("");

  const load = useCallback(async () => {
    setLoadError(false);
    try {
      setGoals(await getGoals());
    } catch (err) {
      console.error("Failed to load goals:", err);
      setLoadError(true);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  if (loadError) {
    return (
      <ErrorState
        message="We couldn't load your reading goals. Check your connection and try again."
        onRetry={load}
      />
    );
  }

  if (goals === null) {
    return <p>Loading your goals...</p>;
  }

  const current = goals.find((g) => g.year === thisYear) ?? {
    year: thisYear,
    target: null,
    booksRead: 0,
  };
  const pastYears = goals.filter((g) => g.year < thisYear);

  const replace = (updates: GoalResponse[]) =>
    setGoals((all) => {
      const next = [...(all ?? [])];

      for (const updated of updates) {
        const at = next.findIndex((g) => g.year === updated.year);
        if (at >= 0) next[at] = updated;
        else next.unshift(updated);
      }

      return next;
    });

  const startEditingThisYear = () => {
    setTarget(current.target ? String(current.target) : "");
    setEditingThisYear(true);
    setError("");
  };

  const saveThisYear = async (e: React.SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    setBusy(true);
    setError("");

    try {
      replace([await setGoal(thisYear, Number(target))]);
      setEditingThisYear(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save");
    } finally {
      setBusy(false);
    }
  };

  const removeThisYear = async () => {
    setBusy(true);
    setError("");

    try {
      replace([await removeGoal(thisYear)]);
      setRemoving(false);
      setEditingThisYear(false);
      setTarget("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to remove");
    } finally {
      setBusy(false);
    }
  };

  const startEditingPast = () => {
    setDrafts(
      Object.fromEntries(
        pastYears.map((y) => [y.year, y.target ? String(y.target) : ""]),
      ),
    );
    setEditingPast(true);
    setPastError("");
  };

  const savePast = async () => {
    const changes = pastYears.filter(
      (y) => drafts[y.year] !== (y.target ? String(y.target) : ""),
    );

    if (changes.some((y) => drafts[y.year] !== "" && Number(drafts[y.year]) < 1)) {
      setPastError(`Goals need to be between 1 and ${mostBooks} books.`);
      return;
    }

    setBusy(true);
    setPastError("");

    try {
      replace(
        await Promise.all(
          changes.map((y) =>
            drafts[y.year] === ""
              ? removeGoal(y.year)
              : setGoal(y.year, Number(drafts[y.year])),
          ),
        ),
      );
      setEditingPast(false);
    } catch (err) {
      setPastError(err instanceof Error ? err.message : "Failed to save");
      await load();
    } finally {
      setBusy(false);
    }
  };

  const percent = current.target
    ? Math.min(100, Math.round((current.booksRead / current.target) * 100))
    : 0;

  return (
    <div className="reading-goals">
      <h1>Reading Goals</h1>

      <section className="goal-current">
        <h2 className="eyebrow">{thisYear} goal</h2>

        {current.target !== null && !editingThisYear ? (
          <>
            <p className="goal-count">
              <strong>{current.booksRead}</strong> of {books(current.target)}
            </p>
            <div
              className="progress-bar"
              role="progressbar"
              aria-valuenow={percent}
              aria-label={`${thisYear} reading goal`}
            >
              <div className="progress-bar-fill" style={{ width: `${percent}%` }} />
              <span className="progress-bar-label">{percent}%</span>
            </div>
            <div className="goal-footer">
              <span className="goal-pace">
                {scheduleMessage(current.target, current.booksRead)}
              </span>
              <button
                type="button"
                className="btn-pill"
                onClick={startEditingThisYear}
              >
                Edit goal
              </button>
            </div>
          </>
        ) : (
          <form className="goal-form" onSubmit={saveThisYear}>
            {current.target === null && (
              <p className="goal-so-far">
                You've read {books(current.booksRead)} so far this year.
              </p>
            )}
            <label htmlFor="goal-target">How many books this year?</label>
            <div className="goal-form-row">
              <input
                id="goal-target"
                className="goal-input"
                type="text"
                inputMode="numeric"
                value={target}
                onChange={(e) => setTarget(digitsOnly(e.target.value))}
              />
              <button
                type="submit"
                className="btn btn-primary"
                disabled={busy || !target}
              >
                {current.target === null ? "Set goal" : "Save"}
              </button>
              {editingThisYear && (
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setEditingThisYear(false)}
                  disabled={busy}
                >
                  Cancel
                </button>
              )}
            </div>
            {editingThisYear && current.target !== null && (
              <button
                type="button"
                className="goal-remove"
                onClick={() => setRemoving(true)}
                disabled={busy}
              >
                Remove goal
              </button>
            )}
          </form>
        )}

        {error && !removing && <p className="form-error">{error}</p>}
      </section>

      {pastYears.length > 0 && (
        <section className="goal-history">
          <div className="goal-history-header">
            <h2 className="eyebrow">Past years</h2>
            {!editingPast && (
              <button
                type="button"
                className="btn-pill"
                aria-label="Edit past years' goals"
                onClick={startEditingPast}
              >
                Edit
              </button>
            )}
          </div>

          <ul>
            {pastYears.map((year) => (
              <li key={year.year} className="goal-history-row">
                <span className="goal-history-year">{year.year}</span>
                <span>{books(year.booksRead)}</span>
                {editingPast ? (
                  <label className="goal-history-edit">
                    Goal
                    <input
                      className="goal-input"
                      type="text"
                      inputMode="numeric"
                      aria-label={`Goal for ${year.year}`}
                      value={drafts[year.year] ?? ""}
                      onChange={(e) =>
                        setDrafts((all) => ({
                          ...all,
                          [year.year]: digitsOnly(e.target.value),
                        }))
                      }
                    />
                  </label>
                ) : year.target === null ? (
                  <span className="goal-history-note">No goal set</span>
                ) : year.booksRead >= year.target ? (
                  <span className="badge badge-green">
                    Goal of {year.target} reached
                  </span>
                ) : (
                  <span className="goal-history-note">Goal was {year.target}</span>
                )}
              </li>
            ))}
          </ul>

          {editingPast && (
            <div className="goal-history-actions">
              <span className="goal-history-note">
                Leave a box empty for no goal.
              </span>
              <button
                type="button"
                className="btn btn-primary"
                onClick={savePast}
                disabled={busy}
              >
                Save
              </button>
              <button
                type="button"
                className="btn btn-secondary"
                onClick={() => setEditingPast(false)}
                disabled={busy}
              >
                Cancel
              </button>
            </div>
          )}

          {pastError && <p className="form-error">{pastError}</p>}
        </section>
      )}

      {removing && (
        <ConfirmDialog
          title="Remove your goal?"
          confirmLabel="Remove"
          busy={busy}
          error={error}
          onConfirm={removeThisYear}
          onCancel={() => setRemoving(false)}
        >
          <p>
            Your {thisYear} goal goes, but the books you've read still count.
          </p>
        </ConfirmDialog>
      )}
    </div>
  );
}

export default ReadingGoals;
