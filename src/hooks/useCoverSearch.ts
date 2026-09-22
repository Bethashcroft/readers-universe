import { useEffect, useRef, useState } from "react";
import { refreshCovers } from "../api/books";

export type CoverSearchOutcome = {
  fixed: number;
  alreadyFine: number;
  notFound: number;
  unverifiable: number;
  unreachable: number;
  throttled: boolean;
  stoppedEarly: boolean;
};

const maxBatches = 500;
const batchPauseMs = 400;

export function useCoverSearch() {
  const [running, setRunning] = useState(false);
  const [processed, setProcessed] = useState(0);
  const [total, setTotal] = useState(0);
  const [outcome, setOutcome] = useState<CoverSearchOutcome | null>(null);
  const [error, setError] = useState("");

  const aborter = useRef<AbortController | null>(null);

  useEffect(() => () => aborter.current?.abort(), []);

  const start = async () => {
    aborter.current?.abort();
    const controller = new AbortController();
    aborter.current = controller;
    const stillWanted = () => !controller.signal.aborted;

    setRunning(true);
    setOutcome(null);
    setError("");
    setProcessed(0);
    setTotal(0);

    let afterId = 0;
    let seen = 0;
    let target = 0;
    const tally: CoverSearchOutcome = {
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

        if (!stillWanted()) return;

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

        seen += result.checked;
        afterId = result.nextAfterId;
        setProcessed(Math.min(seen, target));

        if (result.done || result.checked === 0) {
          tally.stoppedEarly = false;
          break;
        }

        await new Promise((resolve) => setTimeout(resolve, batchPauseMs));
      }

      if (!stillWanted()) return;
      setOutcome(tally);
    } catch (err) {
      if (!stillWanted()) return;
      setError(
        err instanceof Error ? err.message : "The cover search did not finish.",
      );
    } finally {
      if (stillWanted()) {
        setRunning(false);
      }
    }
  };

  return { start, running, processed, total, outcome, error };
}

export type CoverSearch = ReturnType<typeof useCoverSearch>;
