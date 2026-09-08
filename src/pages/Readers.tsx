import { useState, useEffect, useCallback } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { searchReaders } from "../api/readers";
import type { ReaderResponse } from "../api/readers";
import { API_ORIGIN } from "../api/client";
import Pager from "../components/Pager";
import ErrorState from "../components/ErrorState";
import { usePageTitle } from "../hooks/usePageTitle";
import "./Readers.css";

function Readers() {
  usePageTitle("Find Readers");

  const [params] = useSearchParams();
  const initial = params.get("q") ?? "";

  const [search, setSearch] = useState(initial);
  const [debouncedSearch, setDebouncedSearch] = useState(initial);
  const [page, setPage] = useState(1);
  const [readers, setReaders] = useState<ReaderResponse[]>([]);
  const [total, setTotal] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setLoadError(false);

    try {
      const result = await searchReaders({ q: debouncedSearch, page });
      setReaders(result.items);
      setTotal(result.total);
      setTotalPages(result.totalPages);
    } catch (err) {
      console.error("Failed to search readers:", err);
      setLoadError(true);
    } finally {
      setLoading(false);
    }
  }, [debouncedSearch, page]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search);
      setPage(1);
    }, 300);

    return () => clearTimeout(timer);
  }, [search]);

  const changePage = (next: number) => {
    setPage(next);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  if (loadError) {
    return (
      <ErrorState
        message="We couldn't load readers. Check your connection and try again."
        onRetry={load}
      />
    );
  }

  return (
    <div className="readers">
      <h1>Find Readers</h1>
      <p className="readers-subtitle">
        Search for other readers by name, and see what is on their shelves.
      </p>

      <input
        type="text"
        className="readers-search"
        placeholder="Search by name or username"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />

      {loading && <p>Loading readers...</p>}

      {!loading && total === 0 && (
        <p className="readers-empty">
          {debouncedSearch
            ? `No readers found for "${debouncedSearch}".`
            : "Nobody else has joined yet. You are the first!"}
        </p>
      )}

      <div className="readers-list">
        {readers.map((reader) => (
          <Link
            key={reader.userName}
            to={`/profile/${reader.userName}`}
            className="reader-card"
          >
            <div className="reader-avatar">
              {reader.avatarUrl ? (
                <img src={`${API_ORIGIN}${reader.avatarUrl}`} alt="" />
              ) : (
                reader.displayName.charAt(0)
              )}
            </div>
            <div className="reader-details">
              <span className="reader-name">{reader.displayName}</span>
              <span className="reader-username">@{reader.userName}</span>
              {reader.bio && <p className="reader-bio">{reader.bio}</p>}
              <span className="reader-books">
                {reader.bookCount} {reader.bookCount === 1 ? "book" : "books"}{" "}
                on their shelves
              </span>
            </div>
          </Link>
        ))}
      </div>

      <Pager
        page={page}
        totalPages={totalPages}
        total={total}
        noun="reader"
        onChange={changePage}
      />
    </div>
  );
}

export default Readers;
