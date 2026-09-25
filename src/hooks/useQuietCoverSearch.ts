import { useEffect, useEffectEvent } from "react";
import { refreshCovers } from "../api/books";

const maxBatches = 200;
const startDelayMs = 1500;
const batchPauseMs = 400;

export function useQuietCoverSearch(onFound: () => void) {
  const found = useEffectEvent(onFound);

  useEffect(() => {
    const controller = new AbortController();
    const pause = () =>
      new Promise((resolve) => setTimeout(resolve, batchPauseMs));

    const search = async () => {
      let afterId = 0;
      let fixed = 0;

      try {
        for (let batch = 0; batch < maxBatches; batch++) {
          const result = await refreshCovers(afterId, false, controller.signal);
          fixed += result.fixed;
          afterId = result.nextAfterId;

          if (result.done || result.checked === 0) break;
          await pause();
        }
      } catch (err) {
        if (!controller.signal.aborted) {
          console.warn("The quiet cover search stopped:", err);
        }
      }

      if (fixed > 0 && !controller.signal.aborted) {
        found();
      }
    };

    const timer = setTimeout(search, startDelayMs);

    return () => {
      clearTimeout(timer);
      controller.abort();
    };
  }, []);
}
