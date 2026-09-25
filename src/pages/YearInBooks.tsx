import { useState, useEffect } from "react";
import { useParams, Link } from "react-router-dom";
import { getYearInBooks } from "../api/yearInBooks";
import type { YearBook, YearInBooks as YearData } from "../api/yearInBooks";
import { formatLabels } from "../types/book";
import BookCover from "../components/BookCover";
import MonthChart from "../components/MonthChart";
import ErrorState from "../components/ErrorState";
import NotFound from "./NotFound";
import { usePageTitle } from "../hooks/usePageTitle";
import "./YearInBooks.css";

const books = (n: number) => `${n} ${n === 1 ? "book" : "books"}`;
const pages = (n: number) =>
  `${n.toLocaleString("en-GB")} ${n === 1 ? "page" : "pages"}`;

const formatRows = [
  { key: "physical", label: formatLabels.physical },
  { key: "ebook", label: formatLabels.ebook },
  { key: "audiobook", label: formatLabels.audiobook },
  { key: "unset", label: formatLabels[""] },
] as const;

function YearInBooks() {
  const { year } = useParams();
  const thisYear = new Date().getFullYear();

  if (!year || !/^\d{4}$/.test(year) || Number(year) > thisYear) {
    return <NotFound />;
  }

  return <YearPage key={year} year={Number(year)} thisYear={thisYear} />;
}

type YearPageProps = {
  year: number;
  thisYear: number;
};

function YearPage({ year, thisYear }: YearPageProps) {
  usePageTitle(`${year} in books`);

  const [data, setData] = useState<YearData | null>(null);
  const [loadError, setLoadError] = useState(false);
  const [attempt, setAttempt] = useState(0);
  const current = year === thisYear;

  useEffect(() => {
    let active = true;

    const load = async () => {
      try {
        const found = await getYearInBooks(year);
        if (active) setData(found);
      } catch (err) {
        console.error("Failed to load year in books:", err);
        if (active) setLoadError(true);
      }
    };

    load();

    return () => {
      active = false;
    };
  }, [year, attempt]);

  const retry = () => {
    setLoadError(false);
    setAttempt((n) => n + 1);
  };

  if (loadError) {
    return (
      <ErrorState
        message="We couldn't load your year in books. Check your connection and try again."
        onRetry={retry}
      />
    );
  }

  if (data === null) {
    return <p>Loading your {year}...</p>;
  }

  const formats = formatRows
    .map((row) => ({ ...row, count: data.formats[row.key] }))
    .filter((row) => row.count > 0);
  const showFormats = formats.some((row) => row.key !== "unset");
  const mostInOneFormat = Math.max(...formats.map((row) => row.count));

  const goalNote =
    data.target === null
      ? null
      : data.booksRead >= data.target
        ? `Goal of ${data.target} reached`
        : current
          ? `Goal is ${data.target}`
          : `Goal was ${data.target}`;

  return (
    <div className="year-in-books">
      <Link className="year-back" to="/reading-goals">
        Back to Reading Goals
      </Link>
      <h1>{current ? `Your ${year} so far` : `Your ${year} in books`}</h1>

      {data.booksRead === 0 ? (
        <p className="year-empty">
          {current
            ? "You haven't finished any books yet this year."
            : `You didn't finish any books in ${year}.`}
        </p>
      ) : (
        <>
          <section className="year-stats" aria-label="Your year in numbers">
            <div className="year-stat">
              <span className="year-stat-value">{data.booksRead}</span>
              <span className="year-stat-label">
                {data.booksRead === 1 ? "book" : "books"} finished
              </span>
              {goalNote && <span className="year-stat-note">{goalNote}</span>}
            </div>

            <div className="year-stat">
              <span className="year-stat-value">
                {data.pagesRead.toLocaleString("en-GB")}
              </span>
              <span className="year-stat-label">pages read</span>
              {data.booksWithoutPageCount > 0 && (
                <span className="year-stat-note">
                  Not counting {books(data.booksWithoutPageCount)} with no page
                  count
                </span>
              )}
            </div>

            {data.averageRating !== null && (
              <div className="year-stat">
                <span className="year-stat-value">
                  {data.averageRating.toFixed(1)}
                  <span className="year-stat-star" aria-hidden="true">
                    ★
                  </span>
                </span>
                <span className="year-stat-label">average rating</span>
              </div>
            )}

            {data.rereads > 0 && (
              <div className="year-stat">
                <span className="year-stat-value">{data.rereads}</span>
                <span className="year-stat-label">
                  {data.rereads === 1 ? "reread" : "rereads"}
                </span>
              </div>
            )}
          </section>

          <section className="year-section">
            <h2 className="eyebrow">Month by month</h2>
            <MonthChart months={data.months} />
          </section>

          {showFormats && (
            <section className="year-section">
              <h2 className="eyebrow">How you read</h2>
              <ul className="year-formats">
                {formats.map((row) => (
                  <li key={row.key} className="year-format">
                    <span>{row.label}</span>
                    <span className="year-format-track" aria-hidden="true">
                      <span
                        className="year-format-bar"
                        style={{
                          width: `${(row.count / mostInOneFormat) * 100}%`,
                        }}
                      />
                    </span>
                    <span className="year-format-count">
                      {books(row.count)}
                    </span>
                  </li>
                ))}
              </ul>
            </section>
          )}

          {(data.longest || data.topAuthor) && (
            <section className="year-section">
              <h2 className="eyebrow">Highlights</h2>
              <div className="year-highlights">
                {data.longest && (
                  <BookHighlight label="Longest book" book={data.longest} />
                )}
                {data.shortest && (
                  <BookHighlight label="Shortest book" book={data.shortest} />
                )}
                {data.topAuthor && (
                  <div className="year-highlight">
                    <div className="year-highlight-text">
                      <span className="eyebrow">Most read author</span>
                      <strong>{data.topAuthor.name}</strong>
                      <span className="year-highlight-detail">
                        {books(data.topAuthor.books)}
                      </span>
                    </div>
                  </div>
                )}
              </div>
            </section>
          )}

          {data.fiveStars.length > 0 && (
            <section className="year-section">
              <h2 className="eyebrow">Your five stars</h2>
              <CoverWall books={data.fiveStars} />
            </section>
          )}

          <section className="year-section">
            <h2 className="eyebrow">Everything you finished</h2>
            <CoverWall books={data.books} />
          </section>
        </>
      )}
    </div>
  );
}

type BookHighlightProps = {
  label: string;
  book: YearBook;
};

function BookHighlight({ label, book }: BookHighlightProps) {
  return (
    <Link className="year-highlight" to={`/book/${book.bookId}`}>
      <BookCover
        className="year-highlight-cover"
        src={book.coverUrl}
        title={book.title}
      />
      <div className="year-highlight-text">
        <span className="eyebrow">{label}</span>
        <strong>{book.title}</strong>
        {book.pageCount !== null && (
          <span className="year-highlight-detail">{pages(book.pageCount)}</span>
        )}
      </div>
    </Link>
  );
}

type CoverWallProps = {
  books: YearBook[];
};

function CoverWall({ books }: CoverWallProps) {
  return (
    <ul className="year-covers">
      {books.map((book, index) => (
        <li key={`${book.bookId}-${index}`}>
          <Link
            to={`/book/${book.bookId}`}
            title={book.author ? `${book.title} by ${book.author}` : book.title}
          >
            <BookCover src={book.coverUrl} title={book.title} />
          </Link>
        </li>
      ))}
    </ul>
  );
}

export default YearInBooks;
