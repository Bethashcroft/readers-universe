import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import YearInBooks from "./YearInBooks";
import type {
  YearBook,
  YearInBooks as YearData,
} from "../api/yearInBooks";

const { mockGetYearInBooks } = vi.hoisted(() => ({
  mockGetYearInBooks: vi.fn(),
}));

vi.mock("../api/yearInBooks", () => ({
  getYearInBooks: mockGetYearInBooks,
}));

const thisYear = new Date().getFullYear();
const lastYear = thisYear - 1;

const book = (
  bookId: number,
  title: string,
  extra: Partial<YearBook> = {},
): YearBook => ({
  bookId,
  title,
  author: "R.F. Kuang",
  coverUrl: "",
  pageCount: null,
  finishedDate: `${lastYear}-03-01T00:00:00`,
  rating: null,
  ...extra,
});

const quietYear = (extra: Partial<YearData> = {}): YearData => ({
  year: lastYear,
  target: null,
  booksRead: 0,
  pagesRead: 0,
  booksWithoutPageCount: 0,
  averageRating: null,
  rereads: 0,
  months: Array(12).fill(0),
  formats: { physical: 0, ebook: 0, audiobook: 0, unset: 0 },
  longest: null,
  shortest: null,
  topAuthor: null,
  fiveStars: [],
  books: [],
  ...extra,
});

function renderYear(year: number) {
  return render(
    <MemoryRouter initialEntries={[`/reading-goals/${year}`]}>
      <Routes>
        <Route path="/reading-goals/:year" element={<YearInBooks />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("YearInBooks", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("adds up a past year", async () => {
    const piranesi = book(1, "Piranesi", {
      author: "Susanna Clarke",
      pageCount: 300,
      rating: 4,
    });
    const babel = book(2, "Babel", { pageCount: 500, rating: 5 });
    const poppyWar = book(3, "The Poppy War");
    mockGetYearInBooks.mockResolvedValue(
      quietYear({
        target: 10,
        booksRead: 3,
        pagesRead: 800,
        booksWithoutPageCount: 1,
        averageRating: 4.5,
        rereads: 1,
        months: [1, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0],
        formats: { physical: 1, ebook: 1, audiobook: 1, unset: 0 },
        longest: babel,
        shortest: piranesi,
        topAuthor: { name: "R.F. Kuang", books: 2 },
        fiveStars: [babel],
        books: [piranesi, babel, poppyWar],
      }),
    );
    renderYear(lastYear);

    expect(
      await screen.findByRole("heading", { name: `Your ${lastYear} in books` }),
    ).toBeInTheDocument();
    expect(mockGetYearInBooks).toHaveBeenCalledWith(lastYear);

    expect(screen.getByText("Goal was 10")).toBeInTheDocument();
    expect(screen.getByText("800")).toBeInTheDocument();
    expect(
      screen.getByText("Not counting 1 book with no page count"),
    ).toBeInTheDocument();
    expect(screen.getByText("4.5")).toBeInTheDocument();
    expect(screen.getByText("reread")).toBeInTheDocument();

    expect(
      screen.getByRole("img", { name: "2 books in March" }),
    ).toBeInTheDocument();
    expect(screen.getByText("Audiobook")).toBeInTheDocument();

    expect(screen.getByRole("link", { name: /Longest book/ })).toHaveAttribute(
      "href",
      "/book/2",
    );
    expect(screen.getByText("500 pages")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Shortest book/ })).toHaveAttribute(
      "href",
      "/book/1",
    );
    expect(screen.getByText("300 pages")).toBeInTheDocument();
    expect(screen.getByText("R.F. Kuang")).toBeInTheDocument();

    expect(
      screen.getAllByRole("link", { name: "Cover of Babel" }),
    ).toHaveLength(2);
    expect(
      screen.getByRole("link", { name: "Cover of The Poppy War" }),
    ).toHaveAttribute("href", "/book/3");
  });

  it("calls this year so far", async () => {
    mockGetYearInBooks.mockResolvedValue(
      quietYear({
        year: thisYear,
        target: 1,
        booksRead: 1,
        months: [1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
        books: [book(1, "Babel")],
      }),
    );
    renderYear(thisYear);

    expect(
      await screen.findByRole("heading", { name: `Your ${thisYear} so far` }),
    ).toBeInTheDocument();
    expect(screen.getByText("Goal of 1 reached")).toBeInTheDocument();
  });

  it("says so when you finished nothing that year", async () => {
    mockGetYearInBooks.mockResolvedValue(quietYear());
    renderYear(lastYear);

    expect(
      await screen.findByText(`You didn't finish any books in ${lastYear}.`),
    ).toBeInTheDocument();
    expect(screen.queryByText("Month by month")).not.toBeInTheDocument();
  });

  it("skips how you read when no formats were set", async () => {
    mockGetYearInBooks.mockResolvedValue(
      quietYear({
        booksRead: 2,
        booksWithoutPageCount: 2,
        months: [2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
        formats: { physical: 0, ebook: 0, audiobook: 0, unset: 2 },
        books: [book(1, "Babel"), book(2, "Yellowface")],
      }),
    );
    renderYear(lastYear);

    expect(await screen.findByText("Month by month")).toBeInTheDocument();
    expect(screen.queryByText("How you read")).not.toBeInTheDocument();
  });

  it("lets you try again when it can't load", async () => {
    mockGetYearInBooks
      .mockRejectedValueOnce(new Error("offline"))
      .mockResolvedValueOnce(quietYear());
    renderYear(lastYear);

    await userEvent.click(
      await screen.findByRole("button", { name: /try again/i }),
    );

    expect(
      await screen.findByText(`You didn't finish any books in ${lastYear}.`),
    ).toBeInTheDocument();
    expect(mockGetYearInBooks).toHaveBeenCalledTimes(2);
  });

  it("treats a year that hasn't happened yet as not found", async () => {
    renderYear(thisYear + 1);

    expect(screen.getByText("Lost in space")).toBeInTheDocument();
    expect(mockGetYearInBooks).not.toHaveBeenCalled();
  });
});
