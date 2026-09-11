import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import BookDetail from "./BookDetail";
import type { BookDetailResponse, LibraryEntryResponse } from "../api/books";
import type { ReviewResponse } from "../api/reviews";

const { mockGetBook, mockGetReviews, mockUseAuth, mockAddBook } = vi.hoisted(
  () => ({
    mockGetBook: vi.fn(),
    mockGetReviews: vi.fn(),
    mockUseAuth: vi.fn(),
    mockAddBook: vi.fn(),
  }),
);

vi.mock("../api/books", () => ({
  getBook: mockGetBook,
}));

vi.mock("../api/reviews", () => ({
  getReviewsForBook: mockGetReviews,
  addReview: vi.fn(),
  updateReview: vi.fn(),
  deleteReview: vi.fn(),
}));

vi.mock("../context/useBooks", () => ({
  useBooks: () => ({
    addBook: mockAddBook,
    updateBook: vi.fn(),
    removeBook: vi.fn(),
  }),
}));

vi.mock("../api/borrow", () => ({
  createBorrowRequest: vi.fn(),
}));

vi.mock("../context/useAuth", () => ({
  useAuth: mockUseAuth,
}));

const myEntry: LibraryEntryResponse = {
  id: 9,
  bookId: 1,
  alreadyOnShelves: false,
  canRequest: true,
  title: "Gone Girl",
  author: "Gillian Flynn",
  coverUrl: "x",
  isbn: "",
  shelf: "read",
  offer: "none",
  rating: 4,
  userId: "viewer",
  ownerName: "Viewer",
  ownerUserName: "viewer",
  sellerVintedUrl: "",
};

const book: BookDetailResponse = {
  id: 1,
  title: "Gone Girl",
  author: "Gillian Flynn",
  coverUrl: "x",
  isbn: "",
  averageRating: 4,
  ratingCount: 2,
  myEntry: null,
  owners: [],
};

const spoilerReview: ReviewResponse = {
  id: 10,
  rating: 5,
  text: "The twist is she framed him!",
  containsSpoiler: true,
  date: "2026-07-20T10:00:00Z",
  bookId: 1,
  userId: "reviewer",
  userName: "Reviewer",
};

function renderBookDetail() {
  return render(
    <MemoryRouter initialEntries={["/book/1"]}>
      <Routes>
        <Route path="/book/:id" element={<BookDetail />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("BookDetail", () => {
  beforeEach(() => {
    mockGetBook.mockReset();
    mockGetReviews.mockReset();
    mockUseAuth.mockReset();
    mockUseAuth.mockReturnValue({
      user: { userId: "viewer", userName: "viewer" },
    });
  });

  it("shows the community rating for the book", async () => {
    mockGetBook.mockResolvedValue(book);
    mockGetReviews.mockResolvedValue([]);
    renderBookDetail();

    await screen.findByText("Gone Girl");

    expect(screen.getByText(/4 from 2 ratings/)).toBeInTheDocument();
  });

  it("hides a spoiler review's text until Show is clicked", async () => {
    mockGetBook.mockResolvedValue(book);
    mockGetReviews.mockResolvedValue([spoilerReview]);
    const user = userEvent.setup();
    renderBookDetail();

    await screen.findByText("Gone Girl");

    expect(screen.getByText("Reviewer")).toBeInTheDocument();
    expect(
      screen.queryByText("The twist is she framed him!"),
    ).not.toBeInTheDocument();

    await user.click(
      screen.getByRole("button", { name: /This review contains spoilers/ }),
    );

    expect(
      screen.getByText("The twist is she framed him!"),
    ).toBeInTheDocument();
  });

  it("lets anyone rate and review the book", async () => {
    mockGetBook.mockResolvedValue(book);
    mockGetReviews.mockResolvedValue([]);
    renderBookDetail();

    await screen.findByText("Gone Girl");

    expect(screen.getByText("Rate and Review")).toBeInTheDocument();
    expect(screen.getByLabelText("Review (optional)")).toBeInTheDocument();
  });

  it("hides the form once you have already reviewed the book", async () => {
    mockGetBook.mockResolvedValue(book);
    mockGetReviews.mockResolvedValue([
      { ...spoilerReview, userId: "viewer", containsSpoiler: false },
    ]);
    renderBookDetail();

    await screen.findByText("Gone Girl");

    expect(screen.queryByLabelText("Review (optional)")).not.toBeInTheDocument();
  });

  it("opens your own review in the form when you click Edit", async () => {
    mockGetBook.mockResolvedValue(book);
    mockGetReviews.mockResolvedValue([
      {
        ...spoilerReview,
        userId: "viewer",
        userName: "Viewer",
        containsSpoiler: false,
        text: "My original thoughts",
      },
    ]);
    const user = userEvent.setup();
    renderBookDetail();

    await screen.findByText("Gone Girl");

    expect(screen.queryByText("Edit your review")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Edit" }));

    expect(screen.getByText("Edit your review")).toBeInTheDocument();
    expect(screen.getByLabelText("Review (optional)")).toHaveValue(
      "My original thoughts",
    );
    expect(screen.getByLabelText("Rating")).toHaveValue("5");
  });

  it("offers to add the book to your shelves when you don't own it", async () => {
    mockGetBook.mockResolvedValue(book);
    mockGetReviews.mockResolvedValue([]);
    mockAddBook.mockResolvedValue({});
    const user = userEvent.setup();
    renderBookDetail();

    await screen.findByText("Gone Girl");

    await user.click(
      screen.getByRole("button", { name: "Add to my shelves" }),
    );

    expect(mockAddBook).toHaveBeenCalledWith(
      expect.objectContaining({ bookId: 1, shelf: "tbr" }),
    );
  });

  it("lists other owners you can borrow from", async () => {
    mockGetBook.mockResolvedValue({
      ...book,
      owners: [
        {
          libraryEntryId: 42,
          userName: "rebel",
          displayName: "Rebel Ashcroft",
          offer: "available-to-borrow",
          sellerVintedUrl: "",
          canRequest: true,
        },
      ],
    });
    mockGetReviews.mockResolvedValue([]);
    renderBookDetail();

    await screen.findByText("Gone Girl");

    expect(screen.getByText("Available from")).toBeInTheDocument();
    expect(screen.getByText("Rebel Ashcroft")).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "Request to Borrow" }),
    ).toBeInTheDocument();
  });

  it("only lets Trusted Book Club members request to borrow", async () => {
    mockGetBook.mockResolvedValue({
      ...book,
      owners: [
        {
          libraryEntryId: 42,
          userName: "rebel",
          displayName: "Rebel Ashcroft",
          offer: "available-to-borrow",
          sellerVintedUrl: "",
          canRequest: false,
        },
      ],
    });
    mockGetReviews.mockResolvedValue([]);
    renderBookDetail();

    await screen.findByText("Gone Girl");

    expect(
      screen.getByText("Only Rebel Ashcroft's Trusted Book Club can borrow this"),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Request to Borrow" }),
    ).not.toBeInTheDocument();
  });

  it("shows shelf controls only when the book is on your shelves", async () => {
    mockGetBook.mockResolvedValue({ ...book, myEntry });
    mockGetReviews.mockResolvedValue([]);
    renderBookDetail();

    await screen.findByText("Gone Girl");

    expect(screen.getByLabelText("Shelf")).toBeInTheDocument();
    expect(
      screen.queryByText("This book isn't on your shelves."),
    ).not.toBeInTheDocument();
  });
});
