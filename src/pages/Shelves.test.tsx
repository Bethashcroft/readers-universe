import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import Shelves from "./Shelves";
import type { LibraryEntryResponse } from "../api/books";

const {
  mockGetMyBooks,
  mockGetShelfCounts,
  mockOfferBook,
  mockGetBorrowing,
  mockRefreshCovers,
} = vi.hoisted(() => ({
  mockGetMyBooks: vi.fn(),
  mockGetShelfCounts: vi.fn(),
  mockOfferBook: vi.fn(),
  mockGetBorrowing: vi.fn(),
  mockRefreshCovers: vi.fn(),
}));

vi.mock("../api/books", () => ({
  getMyBooks: mockGetMyBooks,
  getShelfCounts: mockGetShelfCounts,
  offerBook: mockOfferBook,
  refreshCovers: mockRefreshCovers,
}));

const coverRun = (fixed: number) => ({
  checked: 1,
  fixed,
  alreadyFine: 0,
  notFound: 1 - fixed,
  unverifiable: 0,
  unreachable: 0,
  nextAfterId: 1,
  total: 0,
  done: true,
  throttled: false,
});

vi.mock("../api/borrow", () => ({
  getBorrowing: mockGetBorrowing,
}));

function lending(used: number) {
  return {
    offering: Array.from({ length: used }, (_, i) => ({ libraryEntryId: 100 + i })),
    borrowed: [],
    incoming: [],
    outgoing: [],
    history: [],
    limit: 3,
  };
}

function shelvesOf(...items: LibraryEntryResponse[]) {
  return {
    items,
    page: 1,
    pageSize: 20,
    total: items.length,
    totalPages: 1,
  };
}

const book: LibraryEntryResponse = {
  id: 1,
  bookId: 10,
  alreadyOnShelves: false,
  canRequest: true,
  page: null,
  pageCount: null,
  finishedDate: null,
  timesRead: 0,
  title: "Piranesi",
  author: "Susanna Clarke",
  coverUrl: "x",
  isbn: "",
  shelf: "read",
  offer: "none",
  format: "",
  rating: null,
  userId: "me",
  ownerName: "Me",
  ownerUserName: "me",
  sellerVintedUrl: "",
};

function renderShelves() {
  return render(
    <MemoryRouter>
      <Shelves />
    </MemoryRouter>,
  );
}

describe("Shelves", () => {
  beforeEach(() => {
    mockGetMyBooks.mockReset();
    mockGetShelfCounts.mockReset();
    mockGetMyBooks.mockResolvedValue({
      items: [book],
      page: 1,
      pageSize: 20,
      total: 1,
      totalPages: 1,
    });
    mockGetShelfCounts.mockResolvedValue({ read: 1, tbr: 2 });
    mockGetBorrowing.mockResolvedValue(lending(0));
    mockOfferBook.mockReset();
    mockRefreshCovers.mockReset();
    mockRefreshCovers.mockResolvedValue(coverRun(0));
  });

  it("quietly looks for missing covers and shows any it finds", async () => {
    mockRefreshCovers.mockResolvedValue(coverRun(1));
    mockGetMyBooks
      .mockResolvedValueOnce(shelvesOf(book))
      .mockResolvedValueOnce(shelvesOf({ ...book, coverUrl: "found.jpg" }));
    renderShelves();

    const cover = await screen.findByRole("img", { name: "Cover of Piranesi" });
    expect(cover).toHaveAttribute("src", "x");

    await waitFor(
      () => expect(screen.getByRole("img", { name: "Cover of Piranesi" })).toHaveAttribute("src", "found.jpg"),
      { timeout: 3000 },
    );
    expect(mockRefreshCovers).toHaveBeenCalledTimes(1);
    expect(screen.queryByText("Loading your shelves...")).not.toBeInTheDocument();
  });

  it("offers a book to borrow straight from its card", async () => {
    mockOfferBook.mockResolvedValue(undefined);
    renderShelves();

    await userEvent.click(
      await screen.findByRole("button", { name: "Offer Piranesi to borrow" }),
    );

    expect(mockOfferBook).toHaveBeenCalledWith(1);
    expect(await screen.findByText("Available to Borrow")).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Offer Piranesi to borrow" }),
    ).not.toBeInTheDocument();
  });

  it("only offers books that can be lent", async () => {
    mockGetMyBooks.mockResolvedValue(
      shelvesOf(
        { ...book, id: 1, title: "Paper" },
        { ...book, id: 2, title: "Kindle", format: "ebook" },
        { ...book, id: 3, title: "Wishlist", shelf: "want-to-read" },
        { ...book, id: 4, title: "Selling", offer: "for-sale" },
      ),
    );
    renderShelves();

    expect(
      await screen.findByRole("button", { name: "Offer Paper to borrow" }),
    ).toBeInTheDocument();
    expect(screen.getAllByRole("button", { name: /to borrow$/ })).toHaveLength(1);
  });

  it("hides the offer buttons once every lending spot is taken", async () => {
    mockGetBorrowing.mockResolvedValue(lending(3));
    renderShelves();

    expect(
      await screen.findByText(/All 3 of your lending spots are taken/),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Offer Piranesi to borrow" }),
    ).not.toBeInTheDocument();
  });

  it("shows the books and a count on each shelf tab", async () => {
    renderShelves();

    expect(await screen.findByText("Piranesi")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /All\s*3/ })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Read\s*1/ })).toBeInTheDocument();
  });

  it("filters by shelf when a tab is clicked", async () => {
    renderShelves();
    await screen.findByText("Piranesi");

    await userEvent.click(screen.getByRole("button", { name: /To Be Read/ }));

    expect(mockGetMyBooks).toHaveBeenLastCalledWith(
      expect.objectContaining({ shelf: "tbr", page: 1 }),
    );
  });

  it("flips the order and says which way round it is", async () => {
    renderShelves();
    await screen.findByText("Piranesi");

    await userEvent.click(
      screen.getByRole("button", { name: "Sort by Recently added" }),
    );
    await userEvent.click(screen.getByRole("option", { name: "Date finished" }));
    await userEvent.click(screen.getByRole("button", { name: /Newest first/ }));

    expect(mockGetMyBooks).toHaveBeenLastCalledWith(
      expect.objectContaining({ sort: "finished", reverse: true }),
    );
    expect(
      screen.getByRole("button", { name: /Oldest first/ }),
    ).toBeInTheDocument();
  });

  it("starts a new sort the natural way round", async () => {
    renderShelves();
    await screen.findByText("Piranesi");

    await userEvent.click(screen.getByRole("button", { name: /Newest first/ }));
    await userEvent.click(
      screen.getByRole("button", { name: "Sort by Recently added" }),
    );
    await userEvent.click(screen.getByRole("option", { name: "Title" }));

    expect(mockGetMyBooks).toHaveBeenLastCalledWith(
      expect.objectContaining({ sort: "title", reverse: false }),
    );
    expect(screen.getByRole("button", { name: /A to Z/ })).toBeInTheDocument();
  });
});
