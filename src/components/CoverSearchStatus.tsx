import type { CoverSearch, CoverSearchOutcome } from "../hooks/useCoverSearch";
import "./CoverSearchStatus.css";

type CoverSearchStatusProps = {
  search: CoverSearch;
};

const summarise = (result: CoverSearchOutcome) => {
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
    parts.push("Open Library is busy, so we stopped early.");
  } else if (result.unreachable > 0) {
    parts.push(
      `${result.unreachable} could not be checked and were left alone.`,
    );
  }

  if (result.stoppedEarly && !result.throttled) {
    parts.push("There are more to go.");
  }

  return parts.join(" ");
};

const needsAnotherGo = (result: CoverSearchOutcome) =>
  result.throttled || result.stoppedEarly;

function CoverSearchStatus({ search }: CoverSearchStatusProps) {
  const { running, processed, total, outcome, error } = search;
  const showTryAgain =
    !running && (error !== "" || (outcome !== null && needsAnotherGo(outcome)));

  return (
    <div className="cover-search">
      {running && (
        <span className="cover-search-status">
          Looking for covers…
          {total > 0 && ` Checked ${processed} of ${total}`}
        </span>
      )}

      {!running && outcome && (
        <span className="cover-search-status">{summarise(outcome)}</span>
      )}

      {error && <span className="form-error">{error}</span>}

      {showTryAgain && (
        <button
          type="button"
          className="btn btn-secondary"
          onClick={search.start}
        >
          Try again
        </button>
      )}
    </div>
  );
}

export default CoverSearchStatus;
