import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import BookFinder from "./BookFinder";

const { mockFindBooks } = vi.hoisted(() => ({ mockFindBooks: vi.fn() }));

vi.mock("../api/books", () => ({
  findBooks: mockFindBooks,
}));

const babel = {
  title: "Babel",
  author: "R. F. Kuang",
  coverUrl: "",
  isbn: "9780008501815",
  pageCount: 569,
  year: 2022,
};

describe("BookFinder", () => {
  beforeEach(() => {
    mockFindBooks.mockReset();
  });

  it("lists matches with author and year as you type", async () => {
    mockFindBooks.mockResolvedValue([babel]);
    render(<BookFinder onPick={vi.fn()} />);

    await userEvent.type(screen.getByLabelText("Search for the book"), "babel");

    expect(await screen.findByText("Babel")).toBeInTheDocument();
    expect(screen.getByText("R. F. Kuang (2022)")).toBeInTheDocument();
  });

  it("hands the pick up and clears the box", async () => {
    mockFindBooks.mockResolvedValue([babel]);
    const onPick = vi.fn();
    render(<BookFinder onPick={onPick} />);

    const box = screen.getByLabelText("Search for the book");
    await userEvent.type(box, "babel");
    await userEvent.click(await screen.findByRole("button", { name: /Babel/ }));

    expect(onPick).toHaveBeenCalledWith(babel);
    expect(box).toHaveValue("");
    expect(screen.queryByText("R. F. Kuang (2022)")).not.toBeInTheDocument();
  });

  it("says so when nothing matches", async () => {
    mockFindBooks.mockResolvedValue([]);
    render(<BookFinder onPick={vi.fn()} />);

    await userEvent.type(screen.getByLabelText("Search for the book"), "zzz");

    expect(
      await screen.findByText("Nothing found. Just type the details in below."),
    ).toBeInTheDocument();
  });
});
