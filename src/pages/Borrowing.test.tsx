import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import Borrowing from "./Borrowing";
import type { BorrowingResponse, BorrowRequestResponse } from "../api/borrow";

const {
  mockGetBorrowing,
  mockUpdateStatus,
  mockWithdraw,
  mockMarkReturned,
  mockTakeBack,
  mockGetMyBooks,
  mockOfferBook,
} = vi.hoisted(() => ({
  mockGetBorrowing: vi.fn(),
  mockUpdateStatus: vi.fn(),
  mockWithdraw: vi.fn(),
  mockMarkReturned: vi.fn(),
  mockTakeBack: vi.fn(),
  mockGetMyBooks: vi.fn(),
  mockOfferBook: vi.fn(),
}));

vi.mock("../api/borrow", () => ({
  getBorrowing: mockGetBorrowing,
  updateBorrowStatus: mockUpdateStatus,
  withdrawBorrowRequest: mockWithdraw,
  markReturned: mockMarkReturned,
}));

vi.mock("../api/books", () => ({
  takeBackBook: mockTakeBack,
  getMyBooks: mockGetMyBooks,
  offerBook: mockOfferBook,
}));

vi.mock("../context/useAuth", () => ({
  useAuth: () => ({ user: { userId: "me", userName: "me" } }),
}));

const request: BorrowRequestResponse = {
  id: 7,
  bookId: 20,
  bookTitle: "Circe",
  fromUserId: "u2",
  fromUserName: "Sophie Bell",
  toUserId: "me",
  toUserName: "Me",
  status: "pending",
  message: "Please?",
  date: new Date().toISOString(),
};

const data: BorrowingResponse = {
  offering: [
    {
      libraryEntryId: 1,
      bookId: 10,
      title: "Piranesi",
      author: "Susanna Clarke",
      coverUrl: "x",
      offer: "available-to-borrow",
      borrower: null,
    },
    {
      libraryEntryId: 2,
      bookId: 11,
      title: "The Hobbit",
      author: "J.R.R. Tolkien",
      coverUrl: "x",
      offer: "lent-out",
      borrower: { requestId: 5, displayName: "Tom Ashby", userName: "quietpages" },
    },
  ],
  borrowed: [],
  incoming: [request],
  outgoing: [],
  history: [{ ...request, id: 8, status: "declined" }],
  limit: 3,
};

function renderPage() {
  return render(
    <MemoryRouter>
      <Borrowing />
    </MemoryRouter>,
  );
}

describe("Borrowing", () => {
  beforeEach(() => {
    mockGetBorrowing.mockReset();
    mockUpdateStatus.mockReset();
    mockWithdraw.mockReset();
    mockMarkReturned.mockReset();
    mockTakeBack.mockReset();
    mockGetMyBooks.mockReset();
    mockOfferBook.mockReset();
    mockGetBorrowing.mockResolvedValue(data);
    mockUpdateStatus.mockResolvedValue({ ...request, status: "accepted" });
    mockMarkReturned.mockResolvedValue({ ...request, status: "returned" });
    mockTakeBack.mockResolvedValue(undefined);
    mockOfferBook.mockResolvedValue(undefined);
  });

  it("shows one slot per offered book and an empty slot for the rest", async () => {
    renderPage();

    expect(await screen.findByText("2 of 3")).toBeInTheDocument();
    expect(screen.getByText("Piranesi")).toBeInTheDocument();
    expect(screen.getByText("Available")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Tom Ashby" })).toHaveAttribute(
      "href",
      "/profile/quietpages",
    );
    expect(
      screen.getAllByRole("button", { name: "Offer a book" }),
    ).toHaveLength(1);
  });

  it("never hides an offered book, even past the limit", async () => {
    mockGetBorrowing.mockResolvedValue({
      ...data,
      offering: [1, 2, 3, 4].map((n) => ({
        ...data.offering[0],
        libraryEntryId: n,
        bookId: n * 10,
        title: `Book ${n}`,
      })),
    });
    renderPage();

    expect(await screen.findByText("4 of 3")).toBeInTheDocument();
    expect(screen.getByText("Book 4")).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Offer a book" }),
    ).not.toBeInTheDocument();
  });

  it("marks a lent book as returned", async () => {
    renderPage();

    await userEvent.click(
      await screen.findByRole("button", { name: "Mark returned" }),
    );

    expect(mockMarkReturned).toHaveBeenCalledWith(5);
    expect(mockGetBorrowing).toHaveBeenCalledTimes(2);
  });

  it("takes an available book back", async () => {
    renderPage();

    await userEvent.click(
      await screen.findByRole("button", { name: "Take back" }),
    );

    expect(mockTakeBack).toHaveBeenCalledWith(1);
  });

  it("accepts an incoming request", async () => {
    renderPage();

    expect(await screen.findByText("Requested by Sophie Bell")).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: "Accept" }));

    expect(mockUpdateStatus).toHaveBeenCalledWith(7, "accepted");
  });

  it("keeps declined and returned requests in a collapsed history", async () => {
    renderPage();

    await screen.findByText("2 of 3");
    expect(screen.queryByText("declined")).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: /History/ }));

    expect(screen.getByText("declined")).toBeInTheDocument();
    expect(screen.getByText("Sophie Bell asked you")).toBeInTheDocument();
    expect(screen.getAllByRole("link", { name: "Open chat" })).toHaveLength(2);
  });

  it("offers a book from the picker", async () => {
    mockGetMyBooks.mockResolvedValue({
      items: [
        {
          id: 3,
          bookId: 12,
          title: "Circe",
          author: "Madeline Miller",
          coverUrl: "x",
          shelf: "read",
          offer: "none",
        },
      ],
      page: 1,
      pageSize: 100,
      total: 1,
      totalPages: 1,
    });
    renderPage();

    await userEvent.click(
      await screen.findByRole("button", { name: "Offer a book" }),
    );

    const dialog = await screen.findByRole("dialog");
    await userEvent.click(
      await within(dialog).findByRole("button", { name: "Offer" }),
    );

    expect(mockGetMyBooks).toHaveBeenCalledWith(
      expect.objectContaining({ offerable: true }),
    );
    expect(mockOfferBook).toHaveBeenCalledWith(3);
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });
});
