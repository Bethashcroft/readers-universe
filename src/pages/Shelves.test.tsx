import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import Shelves from "./Shelves";
import type { LibraryEntryResponse } from "../api/books";

const { mockGetMyBooks, mockGetShelfCounts } = vi.hoisted(() => ({
  mockGetMyBooks: vi.fn(),
  mockGetShelfCounts: vi.fn(),
}));

vi.mock("../api/books", () => ({
  getMyBooks: mockGetMyBooks,
  getShelfCounts: mockGetShelfCounts,
}));

const book: LibraryEntryResponse = {
  id: 1,
  bookId: 10,
  alreadyOnShelves: false,
  canRequest: true,
  page: null,
  pageCount: null,
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
});
