import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import Browse from "./Browse";
import type { BookResponse } from "../api/books";

const { mockBrowseBooks } = vi.hoisted(() => ({ mockBrowseBooks: vi.fn() }));

vi.mock("../api/books", () => ({
  browseBooks: mockBrowseBooks,
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

const books: BookResponse[] = [
  {
    id: 1,
    title: "The Hobbit",
    author: "J.R.R. Tolkien",
    coverUrl: "x",
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
    title: "Dune",
    author: "Frank Herbert",
    coverUrl: "x",
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
  });

  it("shows a loading message, then the fetched books", async () => {
    mockBrowseBooks.mockResolvedValue(books);
    render(<MemoryRouter><Browse /></MemoryRouter>);

    expect(screen.getByText("Loading books...")).toBeInTheDocument();

    expect(await screen.findByText("The Hobbit")).toBeInTheDocument();
    expect(screen.getByText("Dune")).toBeInTheDocument();
    expect(screen.queryByText("Loading books...")).not.toBeInTheDocument();
  });

  it("shows an empty-state message when no books are available", async () => {
    mockBrowseBooks.mockResolvedValue([]);
    render(<MemoryRouter><Browse /></MemoryRouter>);

    expect(
      await screen.findByText("No books available nearby right now."),
    ).toBeInTheDocument();
  });

  it("filters the list by search text", async () => {
    mockBrowseBooks.mockResolvedValue(books);
    const user = userEvent.setup();
    render(<MemoryRouter><Browse /></MemoryRouter>);

    await screen.findByText("The Hobbit");

    await user.type(
      screen.getByPlaceholderText("Search by title or author"),
      "dune",
    );

    expect(screen.getByText("Dune")).toBeInTheDocument();
    expect(screen.queryByText("The Hobbit")).not.toBeInTheDocument();
  });

  it("filters the list by the For Sale tab", async () => {
    mockBrowseBooks.mockResolvedValue(books);
    const user = userEvent.setup();
    render(<MemoryRouter><Browse /></MemoryRouter>);

    await screen.findByText("The Hobbit");

    await user.click(screen.getByRole("button", { name: "For Sale" }));

    expect(screen.getByText("Dune")).toBeInTheDocument();
    expect(screen.queryByText("The Hobbit")).not.toBeInTheDocument();
  });

  it("shows a no-match message when nothing matches the search", async () => {
    mockBrowseBooks.mockResolvedValue(books);
    const user = userEvent.setup();
    render(<MemoryRouter><Browse /></MemoryRouter>);

    await screen.findByText("The Hobbit");

    await user.type(
      screen.getByPlaceholderText("Search by title or author"),
      "zzznomatch",
    );

    expect(screen.getByText("No books match your search.")).toBeInTheDocument();
    expect(screen.queryByText("The Hobbit")).not.toBeInTheDocument();
  });
});
