import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import ReadingProgress from "./ReadingProgress";
import type { LibraryEntryResponse } from "../api/books";

const { mockUpdateProgress } = vi.hoisted(() => ({
  mockUpdateProgress: vi.fn(),
}));

vi.mock("../api/books", () => ({
  updateProgress: mockUpdateProgress,
}));

const entry: LibraryEntryResponse = {
  id: 9,
  bookId: 1,
  alreadyOnShelves: false,
  canRequest: true,
  page: 120,
  pageCount: 340,
  title: "Piranesi",
  author: "Susanna Clarke",
  coverUrl: "x",
  isbn: "",
  shelf: "currently-reading",
  offer: "none",
  rating: null,
  userId: "me",
  ownerName: "Me",
  ownerUserName: "me",
  sellerVintedUrl: "",
};

function renderProgress(overrides: Partial<LibraryEntryResponse> = {}) {
  const onSaved = vi.fn();
  render(<ReadingProgress entry={{ ...entry, ...overrides }} onSaved={onSaved} />);
  return onSaved;
}

describe("ReadingProgress", () => {
  beforeEach(() => {
    mockUpdateProgress.mockReset();
    mockUpdateProgress.mockResolvedValue({ ...entry, page: 150 });
  });

  it("shows the bar and the page count as plain text", () => {
    renderProgress();

    expect(screen.getByRole("progressbar")).toHaveAttribute("aria-valuenow", "35");
    expect(screen.getByText("340")).toBeInTheDocument();
    expect(screen.queryByLabelText("Pages in your copy")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Update" })).not.toBeInTheDocument();
  });

  it("shows Update only once the page has changed, then saves", async () => {
    const onSaved = renderProgress();
    const page = screen.getByLabelText("Page");

    await userEvent.clear(page);
    await userEvent.type(page, "150");

    await userEvent.click(screen.getByRole("button", { name: "Update" }));

    expect(mockUpdateProgress).toHaveBeenCalledWith(9, {
      page: 150,
      pageCount: 340,
    });
    expect(onSaved).toHaveBeenCalledWith(expect.objectContaining({ page: 150 }));
  });

  it("only lets you change the page count after pressing Edit", async () => {
    renderProgress();

    await userEvent.click(screen.getByRole("button", { name: "Edit" }));

    const count = screen.getByLabelText("Pages in your copy");
    await userEvent.clear(count);
    await userEvent.type(count, "350");
    await userEvent.click(screen.getByRole("button", { name: "Update" }));

    expect(mockUpdateProgress).toHaveBeenCalledWith(9, {
      page: 120,
      pageCount: 350,
    });
  });

  it("shows no bar and an empty count box when the page count is unknown", () => {
    renderProgress({ pageCount: null });

    expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
    expect(screen.getByLabelText("Pages in your copy")).toHaveValue(null);
    expect(
      screen.queryByRole("button", { name: "Edit" }),
    ).not.toBeInTheDocument();
  });

  it("shows the server's message when the save is refused", async () => {
    mockUpdateProgress.mockRejectedValue(
      new Error("You can't be past the last page."),
    );
    renderProgress();
    const page = screen.getByLabelText("Page");

    await userEvent.clear(page);
    await userEvent.type(page, "999");
    await userEvent.click(screen.getByRole("button", { name: "Update" }));

    expect(
      await screen.findByText("You can't be past the last page."),
    ).toBeInTheDocument();
  });
});
