import { useState, useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { searchReaders } from "../api/readers";
import type { ReaderResponse } from "../api/readers";
import { API_ORIGIN } from "../api/client";
import "./ReaderSearchBar.css";

function ReaderSearchBar() {
  const navigate = useNavigate();
  const [term, setTerm] = useState("");
  const [results, setResults] = useState<ReaderResponse[]>([]);
  const [total, setTotal] = useState(0);
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!term.trim()) {
      return;
    }

    let active = true;

    const timer = setTimeout(async () => {
      try {
        const found = await searchReaders({ q: term.trim(), page: 1 });
        if (!active) return;
        setResults(found.items.slice(0, 5));
        setTotal(found.total);
        setOpen(true);
      } catch (err) {
        console.error("Reader search failed:", err);
      }
    }, 300);

    return () => {
      active = false;
      clearTimeout(timer);
    };
  }, [term]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const handlePointerDown = (event: MouseEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    };

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setOpen(false);
      }
    };

    document.addEventListener("mousedown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("mousedown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [open]);

  const visible = term.trim() ? results : [];

  const goTo = (path: string) => {
    setOpen(false);
    setTerm("");
    navigate(path);
  };

  return (
    <div className="reader-search" ref={containerRef}>
      <svg
        className="reader-search-icon"
        viewBox="0 0 20 20"
        aria-hidden="true"
      >
        <circle cx="9" cy="9" r="6" />
        <path d="M13.5 13.5 L17 17" />
      </svg>
      <input
        type="search"
        className="reader-search-input"
        placeholder="Find readers"
        aria-label="Find readers"
        value={term}
        onChange={(e) => setTerm(e.target.value)}
        onFocus={() => term.trim() && setOpen(true)}
        onKeyDown={(e) => {
          if (e.key === "Enter" && term.trim()) {
            goTo(`/readers?q=${encodeURIComponent(term.trim())}`);
          }
        }}
      />

      {open && term.trim() && (
        <div className="reader-search-panel">
          {visible.length === 0 ? (
            <p className="reader-search-empty">No readers found.</p>
          ) : (
            visible.map((reader) => (
              <button
                key={reader.userName}
                type="button"
                className="reader-search-result"
                onClick={() => goTo(`/profile/${reader.userName}`)}
              >
                <span className="reader-search-avatar">
                  {reader.avatarUrl ? (
                    <img src={`${API_ORIGIN}${reader.avatarUrl}`} alt="" />
                  ) : (
                    reader.displayName.charAt(0)
                  )}
                </span>
                <span className="reader-search-names">
                  <span className="reader-search-name">
                    {reader.displayName}
                  </span>
                  <span className="reader-search-handle">
                    @{reader.userName}
                  </span>
                </span>
              </button>
            ))
          )}

          {total > visible.length && visible.length > 0 && (
            <button
              type="button"
              className="reader-search-more"
              onClick={() =>
                goTo(`/readers?q=${encodeURIComponent(term.trim())}`)
              }
            >
              See all {total} readers
            </button>
          )}
        </div>
      )}
    </div>
  );
}

export default ReaderSearchBar;
