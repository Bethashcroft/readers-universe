import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import Browse from "./Browse";
import type { LibraryEntryResponse } from "../api/books";

const { mockBrowseBooks, mockUseBooks } = vi.hoisted(() => ({
  mockBrowseBooks: vi.fn(),
  mockUseBooks: vi.fn(),
}));

vi.mock("../api/books", () => ({
  browseBooks: mockBrowseBooks,
}));

vi.mock("../context/useBooks", () => ({
  useBooks: mockUseBooks,
}));

vi.mock("../context/useAuth", () => ({
  useAuth: () => ({
    user: {
      token: "t",
      userId: "viewer",
      userName: "viewer",
      displayName: "Viewer",
    },
  }),
}));

function paged(items: LibraryEntryResponse[]) {
  return {
    items,
    page: 1,
    pageSize: 24,
    total: items.length,
    totalPages: items.length === 0 ? 0 : 1,
  };
}

const books: LibraryEntryResponse[] = [
  {
    id: 1,
    bookId: 101,
    alreadyOnShelves: false,
    title: "The Hobbit",
    author: "J.R.R. Tolkien",
    coverUrl: "x",
    isbn: "",
    shelf: "read",
    offer: "available-to-borrow",
    rating: null,
    userId: "u1",
    sellerVintedUrl: "",
    ownerName: "Hobbit Owner",
    ownerUserName: "hobbitowner",
  },
  {
    id: 2,
    bookId: 102,
    alreadyOnShelves: false,
    title: "Dune",
    author: "Frank Herbert",
    coverUrl: "x",
    isbn: "",
    shelf: "read",
    offer: "for-sale",
    rating: null,
    userId: "u2",
    sellerVintedUrl: "https://www.vinted.co.uk/member/dune-seller",
    ownerName: "Dune Seller",
    ownerUserName: "duneseller",
  },
];

describe("Browse", () => {
  beforeEach(() => {
    mockBrowseBooks.mockReset();
    mockUseBooks.mockReset();
    mockUseBooks.mockReturnValue({ version: 0 });
  });

  it("shows a loading message, then the fetched books", async () => {
    mockBrowseBooks.mockResolvedValue(paged(books));
    render(
      <MemoryRouter>
        <Browse />
      </MemoryRouter>,
    );

    expect(screen.getByText("Loading books...")).toBeInTheDocument();

    expect(await screen.findByText("The Hobbit")).toBeInTheDocument();
    expect(screen.getByText("Dune")).toBeInTheDocument();
    expect(screen.queryByText("Loading books...")).not.toBeInTheDocument();
  });

  it("shows an empty-state message when no books are available", async () => {
    mockBrowseBooks.mockResolvedValue(paged([]));
    render(
      <MemoryRouter>
        <Browse />
      </MemoryRouter>,
    );

    expect(
      await screen.findByText("No books available nearby right now."),
    ).toBeInTheDocument();
  });

  it("asks the server to search, rather than filtering one page", async () => {
    mockBrowseBooks.mockResolvedValue(paged(books));
    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <Browse />
      </MemoryRouter>,
    );

    await screen.findByText("The Hobbit");
    mockBrowseBooks.mockResolvedValue(paged([books[1]]));

    await user.type(
      screen.getByPlaceholderText("Search by title or author"),
      "dune",
    );

    await waitFor(() =>
      expect(mockBrowseBooks).toHaveBeenLastCalledWith(
        expect.objectContaining({ search: "dune", page: 1 }),
      ),
    );
    expect(await screen.findByText("Dune")).toBeInTheDocument();
    expect(screen.queryByText("The Hobbit")).not.toBeInTheDocument();
  });

  it("asks the server for the For Sale filter", async () => {
    mockBrowseBooks.mockResolvedValue(paged(books));
    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <Browse />
      </MemoryRouter>,
    );

    await screen.findByText("The Hobbit");
    mockBrowseBooks.mockResolvedValue(paged([books[1]]));

    await user.click(screen.getByRole("button", { name: "For Sale" }));

    await waitFor(() =>
      expect(mockBrowseBooks).toHaveBeenLastCalledWith(
        expect.objectContaining({ offer: "for-sale", page: 1 }),
      ),
    );
    expect(await screen.findByText("Dune")).toBeInTheDocument();
  });

  it("links each book to its book page", async () => {
    mockBrowseBooks.mockResolvedValue(paged(books));
    render(
      <MemoryRouter>
        <Browse />
      </MemoryRouter>,
    );

    const title = await screen.findByRole("link", { name: "The Hobbit" });
    expect(title).toHaveAttribute("href", "/book/101");
  });

  it("won't offer to borrow a book already on your shelves", async () => {
    mockBrowseBooks.mockResolvedValue(
      paged([{ ...books[0], alreadyOnShelves: true }]),
    );
    render(
      <MemoryRouter>
        <Browse />
      </MemoryRouter>,
    );

    await screen.findByText("The Hobbit");

    expect(screen.getByText("Already on your shelves")).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Request to Borrow" }),
    ).not.toBeInTheDocument();
  });

  it("shows a no-match message when the search returns nothing", async () => {
    mockBrowseBooks.mockResolvedValue(paged(books));
    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <Browse />
      </MemoryRouter>,
    );

    await screen.findByText("The Hobbit");
    mockBrowseBooks.mockResolvedValue(paged([]));

    await user.type(
      screen.getByPlaceholderText("Search by title or author"),
      "zzznomatch",
    );

    expect(
      await screen.findByText("No books match your search."),
    ).toBeInTheDocument();
    expect(screen.queryByText("The Hobbit")).not.toBeInTheDocument();
  });

  it("pages through results and shows the pager only when needed", async () => {
    mockBrowseBooks.mockResolvedValue({
      items: [books[0]],
      page: 1,
      pageSize: 1,
      total: 2,
      totalPages: 2,
    });
    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <Browse />
      </MemoryRouter>,
    );

    await screen.findByText("The Hobbit");
    expect(screen.getByText("Page 1 of 2")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Back" })).toBeDisabled();

    mockBrowseBooks.mockResolvedValue({
      items: [books[1]],
      page: 2,
      pageSize: 1,
      total: 2,
      totalPages: 2,
    });

    await user.click(screen.getByRole("button", { name: "Next" }));

    await waitFor(() =>
      expect(mockBrowseBooks).toHaveBeenLastCalledWith(
        expect.objectContaining({ page: 2 }),
      ),
    );
    expect(await screen.findByText("Dune")).toBeInTheDocument();
  });
});
