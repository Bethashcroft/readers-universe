import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import AddBook from "./AddBook";

const { mockLookupBook, mockFindBooks } = vi.hoisted(() => ({
  mockLookupBook: vi.fn(),
  mockFindBooks: vi.fn(),
}));

vi.mock("../api/books", () => ({
  lookupBook: mockLookupBook,
  findBooks: mockFindBooks,
}));

vi.mock("../context/useBooks", () => ({
  useBooks: () => ({ addBook: vi.fn() }),
}));

function renderAddBook(path = "/add-book") {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <AddBook />
    </MemoryRouter>,
  );
}

describe("AddBook ISBN lookup", () => {
  beforeEach(() => {
    mockLookupBook.mockReset();
    mockFindBooks.mockReset();
  });

  it("searches straight away when you arrive with a query and fills the form from a pick", async () => {
    mockFindBooks.mockResolvedValue([
      {
        title: "Babel",
        author: "R. F. Kuang",
        coverUrl: "",
        isbn: "9780008501815",
        pageCount: 569,
        year: 2022,
      },
    ]);
    const user = userEvent.setup();
    renderAddBook("/add-book?q=babel");

    await user.click(await screen.findByRole("button", { name: /Babel/ }));

    expect(mockFindBooks).toHaveBeenCalledWith("babel");
    expect(screen.getByDisplayValue("Babel")).toBeInTheDocument();
    expect(screen.getByDisplayValue("R. F. Kuang")).toBeInTheDocument();
    expect(screen.getByDisplayValue("9780008501815")).toBeInTheDocument();
    expect(
      screen.getByText("Found it! Check the details below."),
    ).toBeInTheDocument();
  });

  it("fills in the title and author from an ISBN lookup", async () => {
    mockLookupBook.mockResolvedValue({
      title: "The Hobbit",
      author: "J.R.R. Tolkien",
      coverUrl: "https://example.com/cover.jpg",
    });
    const user = userEvent.setup();
    renderAddBook();

    await user.type(
      screen.getByPlaceholderText("e.g. 9780261103344"),
      "9780261103344",
    );
    await user.click(
      screen.getByRole("button", { name: "Fill in the details" }),
    );

    expect(await screen.findByDisplayValue("The Hobbit")).toBeInTheDocument();
    expect(screen.getByDisplayValue("J.R.R. Tolkien")).toBeInTheDocument();
    expect(mockLookupBook).toHaveBeenCalledWith("9780261103344");
  });

  it("keeps the Goodreads import folded away until you ask", async () => {
    const user = userEvent.setup();
    renderAddBook();

    expect(screen.queryByText(/drop the CSV here/)).not.toBeInTheDocument();

    await user.click(
      screen.getByRole("button", { name: /Bring your library with you/ }),
    );

    expect(screen.getByText(/drop the CSV here/)).toBeInTheDocument();
  });

  it("shows a message when the ISBN isn't found", async () => {
    mockLookupBook.mockRejectedValue(new Error("No book found for that ISBN"));
    const user = userEvent.setup();
    renderAddBook();

    await user.type(
      screen.getByPlaceholderText("e.g. 9780261103344"),
      "0000000000",
    );
    await user.click(
      screen.getByRole("button", { name: "Fill in the details" }),
    );

    expect(
      await screen.findByText(/No book found for that ISBN/),
    ).toBeInTheDocument();
  });
});
