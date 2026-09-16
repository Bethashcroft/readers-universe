import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import SearchBar from "./SearchBar";

const { mockSearchReaders, mockSearchBooks } = vi.hoisted(() => ({
  mockSearchReaders: vi.fn(),
  mockSearchBooks: vi.fn(),
}));

vi.mock("../api/readers", () => ({
  searchReaders: mockSearchReaders,
}));

vi.mock("../api/books", () => ({
  searchBooks: mockSearchBooks,
}));

const paged = <T,>(items: T[]) => ({
  items,
  page: 1,
  pageSize: 20,
  total: items.length,
  totalPages: 1,
});

const sophie = {
  userName: "sophie",
  displayName: "Sophie",
  bio: "",
  avatarUrl: "",
  joinedDate: "2026-01-01T00:00:00Z",
  bookCount: 3,
};

const babel = { id: 7, title: "Babel", author: "R.F. Kuang", coverUrl: "" };

function renderSearchBar() {
  return render(
    <MemoryRouter>
      <SearchBar />
      <Routes>
        <Route path="/" element={null} />
        <Route path="/book/:id" element={<p>Book page</p>} />
        <Route path="/profile/:name" element={<p>Profile page</p>} />
        <Route path="/add-book" element={<p>Add a book page</p>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("SearchBar", () => {
  beforeEach(() => {
    mockSearchReaders.mockReset();
    mockSearchBooks.mockReset();
  });

  it("shows readers and books in their own groups", async () => {
    mockSearchReaders.mockResolvedValue(paged([sophie]));
    mockSearchBooks.mockResolvedValue(paged([babel]));
    renderSearchBar();

    await userEvent.type(screen.getByLabelText("Find readers or books"), "so");

    expect(await screen.findByText("Readers")).toBeInTheDocument();
    expect(screen.getByText("Books")).toBeInTheDocument();
    expect(screen.getByText("Sophie")).toBeInTheDocument();
    expect(screen.getByText("Babel")).toBeInTheDocument();
  });

  it("opens the book page when you pick a book", async () => {
    mockSearchReaders.mockResolvedValue(paged([]));
    mockSearchBooks.mockResolvedValue(paged([babel]));
    renderSearchBar();

    await userEvent.type(screen.getByLabelText("Find readers or books"), "bab");
    await userEvent.click(await screen.findByRole("button", { name: /Babel/ }));

    expect(screen.getByText("Book page")).toBeInTheDocument();
  });

  it("opens the first result on Enter", async () => {
    mockSearchReaders.mockResolvedValue(paged([sophie]));
    mockSearchBooks.mockResolvedValue(paged([babel]));
    renderSearchBar();

    const input = screen.getByLabelText("Find readers or books");
    await userEvent.type(input, "so");
    await screen.findByText("Sophie");
    await userEvent.type(input, "{Enter}");

    expect(screen.getByText("Profile page")).toBeInTheDocument();
  });

  it("says when nothing matches", async () => {
    mockSearchReaders.mockResolvedValue(paged([]));
    mockSearchBooks.mockResolvedValue(paged([]));
    renderSearchBar();

    await userEvent.type(screen.getByLabelText("Find readers or books"), "zzz");

    expect(
      await screen.findByText("No readers or books found."),
    ).toBeInTheDocument();
  });

  it("offers to add the book when it isn't here yet", async () => {
    mockSearchReaders.mockResolvedValue(paged([]));
    mockSearchBooks.mockResolvedValue(paged([]));
    renderSearchBar();

    await userEvent.type(
      screen.getByLabelText("Find readers or books"),
      "yellowface",
    );
    await userEvent.click(
      await screen.findByRole("button", { name: "Can't see it? Add a book" }),
    );

    expect(screen.getByText("Add a book page")).toBeInTheDocument();
  });
});
