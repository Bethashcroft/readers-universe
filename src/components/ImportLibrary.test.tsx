import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import ImportLibrary from "./ImportLibrary";

const { mockImportLibrary, mockRefreshCovers } = vi.hoisted(() => ({
  mockImportLibrary: vi.fn(),
  mockRefreshCovers: vi.fn(),
}));

vi.mock("../api/import", () => ({
  importLibrary: mockImportLibrary,
}));

vi.mock("../api/books", () => ({
  refreshCovers: mockRefreshCovers,
}));

const summary = {
  service: "Goodreads",
  committed: false,
  rowsFound: 2,
  added: 2,
  alreadyOnShelves: 0,
  updated: 0,
  reviewsAdded: 0,
  newToCatalogue: 2,
  skippedRows: 0,
  byShelf: { read: 2 },
  sample: ["Babel", "Piranesi"],
};

const coversDone = {
  checked: 2,
  fixed: 2,
  alreadyFine: 0,
  notFound: 0,
  unverifiable: 0,
  unreachable: 0,
  nextAfterId: 2,
  total: 2,
  done: true,
  throttled: false,
};

async function importAFile() {
  const user = userEvent.setup();
  render(
    <MemoryRouter>
      <ImportLibrary />
    </MemoryRouter>,
  );

  const file = new File(["Title,Author"], "goodreads.csv", { type: "text/csv" });
  await user.upload(document.getElementById("import-file")!, file);
  await user.click(screen.getByRole("button", { name: "Check the file" }));
  await user.click(await screen.findByRole("button", { name: /^Import/ }));
}

describe("ImportLibrary", () => {
  beforeEach(() => {
    mockImportLibrary.mockReset();
    mockRefreshCovers.mockReset();
  });

  it("looks for covers by itself once books have been added", async () => {
    mockImportLibrary
      .mockResolvedValueOnce(summary)
      .mockResolvedValueOnce({ ...summary, committed: true });
    mockRefreshCovers.mockResolvedValue(coversDone);

    await importAFile();

    expect(await screen.findByText("Found 2 covers.")).toBeInTheDocument();
    expect(mockRefreshCovers).toHaveBeenCalledTimes(1);
    expect(
      screen.queryByRole("button", { name: "Try again" }),
    ).not.toBeInTheDocument();
  });

  it("offers another go when Open Library was too busy", async () => {
    mockImportLibrary
      .mockResolvedValueOnce(summary)
      .mockResolvedValueOnce({ ...summary, committed: true });
    mockRefreshCovers.mockResolvedValue({
      ...coversDone,
      fixed: 0,
      checked: 0,
      throttled: true,
    });

    await importAFile();

    expect(
      await screen.findByText(/Open Library is busy, so we stopped early/),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "Try again" }),
    ).toBeInTheDocument();
  });

  it("does not bother when nothing was added", async () => {
    const nothingNew = { ...summary, added: 0, alreadyOnShelves: 2 };
    mockImportLibrary.mockResolvedValue(nothingNew);
    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <ImportLibrary />
      </MemoryRouter>,
    );

    const file = new File(["Title,Author"], "goodreads.csv", { type: "text/csv" });
    await user.upload(document.getElementById("import-file")!, file);
    await user.click(screen.getByRole("button", { name: "Check the file" }));

    expect(
      await screen.findByRole("button", { name: "Nothing new to import" }),
    ).toBeDisabled();
    expect(mockRefreshCovers).not.toHaveBeenCalled();
  });

  it("gets one book right in the summary", async () => {
    const oneAlready = { ...summary, alreadyOnShelves: 1 };
    mockImportLibrary
      .mockResolvedValueOnce(oneAlready)
      .mockResolvedValueOnce({ ...oneAlready, committed: true });
    mockRefreshCovers.mockResolvedValue(coversDone);

    await importAFile();

    expect(
      await screen.findByText(/1 was already on your shelves and up to date/),
    ).toBeInTheDocument();
  });

  it("lets a re-import fill in books you already have", async () => {
    const fillOnly = { ...summary, added: 0, alreadyOnShelves: 2, updated: 2 };
    mockImportLibrary
      .mockResolvedValueOnce(fillOnly)
      .mockResolvedValueOnce({ ...fillOnly, committed: true });
    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <ImportLibrary />
      </MemoryRouter>,
    );

    const file = new File(["Title,Author"], "goodreads.csv", { type: "text/csv" });
    await user.upload(document.getElementById("import-file")!, file);
    await user.click(screen.getByRole("button", { name: "Check the file" }));

    expect(
      await screen.findByText(/getting missing finish dates/),
    ).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Fill in 2 books" }));

    expect(
      await screen.findByText(/Filled in missing details on/),
    ).toBeInTheDocument();
    expect(mockRefreshCovers).not.toHaveBeenCalled();
  });
});
