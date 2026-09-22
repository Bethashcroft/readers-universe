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
      await screen.findByRole("button", { name: "Import 0 books" }),
    ).toBeDisabled();
    expect(mockRefreshCovers).not.toHaveBeenCalled();
  });
});
