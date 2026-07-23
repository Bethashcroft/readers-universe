import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import AddBook from "./AddBook";

const { mockLookupBook } = vi.hoisted(() => ({ mockLookupBook: vi.fn() }));

vi.mock("../api/books", () => ({
  lookupBook: mockLookupBook,
}));

vi.mock("../context/useBooks", () => ({
  useBooks: () => ({ addBook: vi.fn() }),
}));

function renderAddBook() {
  return render(
    <MemoryRouter>
      <AddBook />
    </MemoryRouter>,
  );
}

describe("AddBook ISBN lookup", () => {
  beforeEach(() => {
    mockLookupBook.mockReset();
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
