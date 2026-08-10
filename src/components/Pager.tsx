import "./Pager.css";

interface PagerProps {
  page: number;
  totalPages: number;
  total: number;
  noun: string;
  onChange: (page: number) => void;
}

function Pager({ page, totalPages, total, noun, onChange }: PagerProps) {
  if (totalPages <= 1) {
    return null;
  }

  return (
    <nav className="pager" aria-label="Pages">
      <button
        type="button"
        className="pager-step"
        onClick={() => onChange(page - 1)}
        disabled={page <= 1}
      >
        Back
      </button>

      <span className="pager-status">
        Page {page} of {totalPages}
        <span className="pager-total">
          {total} {total === 1 ? noun : `${noun}s`}
        </span>
      </span>

      <button
        type="button"
        className="pager-step"
        onClick={() => onChange(page + 1)}
        disabled={page >= totalPages}
      >
        Next
      </button>
    </nav>
  );
}

export default Pager;
