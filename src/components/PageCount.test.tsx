import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import PageCount from "./PageCount";
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
  page: null,
  pageCount: 11,
  finishedDate: "2026-04-22T00:00:00",
  timesRead: 1,
  title: "The Profiler",
  author: "Helen Sarah Fields",
  coverUrl: "x",
  isbn: "",
  shelf: "read",
  offer: "none",
  format: "ebook",
  rating: 3,
  userId: "me",
  ownerName: "Me",
  ownerUserName: "me",
  sellerVintedUrl: "",
};

function renderPageCount(overrides: Partial<LibraryEntryResponse> = {}) {
  const onSaved = vi.fn();
  render(<PageCount entry={{ ...entry, ...overrides }} onSaved={onSaved} />);
  return onSaved;
}

describe("PageCount", () => {
  beforeEach(() => {
    mockUpdateProgress.mockReset();
  });

  it("fixes a finished book's page count", async () => {
    mockUpdateProgress.mockResolvedValue({ ...entry, pageCount: 400 });
    const onSaved = renderPageCount();

    expect(screen.getByText("11")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Edit page count" }));
    const box = screen.getByLabelText("Pages in your copy");
    expect(box).toHaveValue("11");

    await userEvent.clear(box);
    await userEvent.type(box, "4a00");
    await userEvent.click(screen.getByRole("button", { name: "Save" }));

    expect(mockUpdateProgress).toHaveBeenCalledWith(9, {
      page: null,
      pageCount: 400,
    });
    expect(onSaved).toHaveBeenCalledWith({ ...entry, pageCount: 400 });
    expect(screen.queryByLabelText("Pages in your copy")).not.toBeInTheDocument();
  });

  it("drops a leftover page that would be past the new last page", async () => {
    mockUpdateProgress.mockResolvedValue({ ...entry, page: null, pageCount: 250 });
    renderPageCount({ page: 300, pageCount: 350 });

    await userEvent.click(screen.getByRole("button", { name: "Edit page count" }));
    const box = screen.getByLabelText("Pages in your copy");
    await userEvent.clear(box);
    await userEvent.type(box, "250");
    await userEvent.click(screen.getByRole("button", { name: "Save" }));

    expect(mockUpdateProgress).toHaveBeenCalledWith(9, {
      page: null,
      pageCount: 250,
    });
  });

  it("says Not set when there's no count", () => {
    renderPageCount({ pageCount: null });

    expect(screen.getByText("Not set")).toBeInTheDocument();
  });

  it("shows the error and stays open when saving fails", async () => {
    mockUpdateProgress.mockRejectedValue(
      new Error("Pages need to be positive numbers."),
    );
    renderPageCount();

    await userEvent.click(screen.getByRole("button", { name: "Edit page count" }));
    await userEvent.clear(screen.getByLabelText("Pages in your copy"));
    await userEvent.type(screen.getByLabelText("Pages in your copy"), "0");
    await userEvent.click(screen.getByRole("button", { name: "Save" }));

    expect(
      await screen.findByText("Pages need to be positive numbers."),
    ).toBeInTheDocument();
    expect(screen.getByLabelText("Pages in your copy")).toBeInTheDocument();
  });
});
