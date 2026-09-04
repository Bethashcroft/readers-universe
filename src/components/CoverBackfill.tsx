import { useEffect, useRef, useState } from "react";
import { refreshCovers } from "../api/books";
import "./CoverBackfill.css";

interface CoverBackfillProps {
  onFinished: () => void;
}

type Outcome = {
  fixed: number;
  alreadyFine: number;
  notFound: number;
  unverifiable: number;
  unreachable: number;
  throttled: boolean;
  stoppedEarly: boolean;
};

const maxBatches = 500;

function CoverBackfill({ onFinished }: CoverBackfillProps) {
  const [running, setRunning] = useState(false);
  const [processed, setProcessed] = useState(0);
  const [total, setTotal] = useState(0);
  const [outcome, setOutcome] = useState<Outcome | null>(null);
  const [error, setError] = useState("");

  const active = useRef(true);
  const aborter = useRef<AbortController | null>(null);
  const finishedRef = useRef(onFinished);

  useEffect(() => {
    finishedRef.current = onFinished;
  }, [onFinished]);

  useEffect(() => {
    active.current = true;

    return () => {
      active.current = false;
      aborter.current?.abort();
    };
  }, []);

  const run = async () => {
    const controller = new AbortController();
    aborter.current = controller;

    setRunning(true);
    setOutcome(null);
    setError("");
    setProcessed(0);
    setTotal(0);

    let afterId = 0;
    let seen = 0;
    let target = 0;
    let settled = 0;
    const tally: Outcome = {
      fixed: 0,
      alreadyFine: 0,
      notFound: 0,
      unverifiable: 0,
      unreachable: 0,
      throttled: false,
      stoppedEarly: true,
    };

    try {
      for (let batch = 0; batch < maxBatches; batch++) {
        const result = await refreshCovers(
          afterId,
          batch === 0,
          controller.signal,
        );

        if (!active.current) return;

        if (batch === 0) {
          target = result.total;
          setTotal(result.total);
        }

        tally.fixed += result.fixed;
        tally.alreadyFine += result.alreadyFine;
        tally.notFound += result.notFound;
        tally.unverifiable += result.unverifiable;
        tally.unreachable += result.unreachable;
        tally.throttled = tally.throttled || result.throttled;
        settled += result.fixed + result.notFound;

        seen += result.checked;
        afterId = result.nextAfterId;
        setProcessed(Math.min(seen, target));

        if (result.done || result.checked === 0) {
          tally.stoppedEarly = false;
          break;
        }
      }

      if (!active.current) return;
      setOutcome(tally);
    } catch (err) {
      if (!active.current) return;
      setError(
        err instanceof Error ? err.message : "The cover search did not finish.",
      );
    } finally {
      if (active.current) {
        setRunning(false);
        if (settled > 0) finishedRef.current();
      }
    }
  };

  const summarise = (result: Outcome) => {
    const parts: string[] = [];

    parts.push(
      result.fixed > 0
        ? `Found ${result.fixed} ${result.fixed === 1 ? "cover" : "covers"}.`
        : "No new covers this time.",
    );

    if (result.alreadyFine > 0) {
      parts.push(`${result.alreadyFine} already looked fine.`);
    }

    if (result.notFound > 0) {
      parts.push(
        `${result.notFound} ${result.notFound === 1 ? "book has" : "books have"} no cover we can find.`,
      );
    }

    if (result.unverifiable > 0) {
      parts.push(
        `${result.unverifiable} already had a cover from somewhere we cannot check, so we left ${result.unverifiable === 1 ? "it" : "them"} as ${result.unverifiable === 1 ? "it is" : "they are"}.`,
      );
    }

    if (result.throttled) {
      parts.push("Open Library is busy, so we stopped early. Try again later.");
    } else if (result.unreachable > 0) {
      parts.push(
        `${result.unreachable} could not be checked and were left alone.`,
      );
    }

    if (result.stoppedEarly && !result.throttled) {
      parts.push("There are more to go, so run it again.");
    }

    return parts.join(" ");
  };

  return (
    <div className="cover-backfill">
      <button
        type="button"
        className="btn btn-secondary"
        onClick={run}
        disabled={running}
      >
        {running ? "Looking for covers…" : "Find missing covers"}
      </button>

      {running && total > 0 && (
        <span className="cover-backfill-status">
          Checked {processed} of {total}
        </span>
      )}

      {!running && outcome && (
        <span className="cover-backfill-status">{summarise(outcome)}</span>
      )}

      {error && <span className="form-error">{error}</span>}
    </div>
  );
}

export default CoverBackfill;
