import "./ErrorState.css";

interface ErrorStateProps {
  message?: string;
  onRetry?: () => void;
}

function ErrorState({ message, onRetry }: ErrorStateProps) {
  return (
    <div className="error-state" role="alert">
      <svg
        className="error-state-art"
        viewBox="0 0 130 130"
        aria-hidden="true"
      >
        <circle cx="65" cy="60" r="29" />
        <ellipse
          cx="65"
          cy="66"
          rx="54"
          ry="16"
          transform="rotate(-18 65 66)"
          stroke-dasharray="5 9"
        />
      </svg>
      <p>
        {message ??
          "We couldn't load this right now. Check your connection and try again."}
      </p>
      {onRetry && (
        <button className="btn btn-secondary" onClick={onRetry}>
          Try again
        </button>
      )}
    </div>
  );
}

export default ErrorState;
