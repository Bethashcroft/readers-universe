import { useState } from "react";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import ReadingHistory from "./ReadingHistory";
import type { LibraryEntryResponse } from "../api/books";

const { mockGetReadings, mockEditReading, mockDeleteReading } = vi.hoisted(
  () => ({
    mockGetReadings: vi.fn(),
    mockEditReading: vi.fn(),
    mockDeleteReading: vi.fn(),
  }),
);

vi.mock("../api/books", () => ({
  getReadings: mockGetReadings,
  editReading: mockEditReading,
  deleteReading: mockDeleteReading,
}));

const entry: LibraryEntryResponse = {
  id: 9,
  bookId: 1,
  alreadyOnShelves: false,
  canRequest: true,
  page: null,
  pageCount: null,
  finishedDate: "2026-09-23T00:00:00Z",
  timesRead: 2,
  title: "Babel",
  author: "R.F. Kuang",
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

function BookPage({
  start,
  onChanged,
}: {
  start: LibraryEntryResponse;
  onChanged: (changes: { timesRead: number; finishedDate: string | null }) => void;
}) {
  const [current, setCurrent] = useState(start);

  return (
    <ReadingHistory
      entry={current}
      onChanged={(changes) => {
        setCurrent({ ...current, ...changes });
        onChanged(changes);
      }}
    />
  );
}

const recent = { id: 2, finishedDate: "2026-09-23T00:00:00Z" };
const older = { id: 1, finishedDate: "2024-03-11T00:00:00Z" };

describe("ReadingHistory", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("lists every finish newest first and how many times", async () => {
    mockGetReadings.mockResolvedValue([recent, older]);
    render(<BookPage start={entry} onChanged={vi.fn()} />);

    const days = await screen.findAllByText(/\d{4}$/);

    expect(days.map((d) => d.textContent)).toEqual([
      "23 Sept 2026",
      "11 Mar 2024",
    ]);
    expect(screen.getByText("Read 2 times")).toBeInTheDocument();
  });

  it("corrects a date and tells the page", async () => {
    mockGetReadings.mockResolvedValue([older]);
    mockEditReading.mockResolvedValue([
      { id: 1, finishedDate: "2024-04-11T00:00:00Z" },
    ]);
    const onChanged = vi.fn();
    render(
      <BookPage start={{ ...entry, timesRead: 1 }} onChanged={onChanged} />,
    );

    await userEvent.click(
      await screen.findByRole("button", { name: "Edit 11 Mar 2024" }),
    );

    const box = screen.getByLabelText("Date you finished");
    expect(box).toHaveValue("2024-03-11");

    await userEvent.clear(box);
    await userEvent.type(box, "2024-04-11");
    await userEvent.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByText("11 Apr 2024")).toBeInTheDocument();
    expect(mockEditReading).toHaveBeenCalledWith(1, "2024-04-11");
    expect(onChanged).toHaveBeenCalledWith({
      timesRead: 1,
      finishedDate: "2024-04-11T00:00:00Z",
    });
  });

  it("asks before deleting, then takes it off", async () => {
    mockGetReadings.mockResolvedValue([recent, older]);
    mockDeleteReading.mockResolvedValue([older]);
    const onChanged = vi.fn();
    render(<BookPage start={entry} onChanged={onChanged} />);

    await userEvent.click(
      await screen.findByRole("button", { name: "Delete 23 Sept 2026" }),
    );

    expect(mockDeleteReading).not.toHaveBeenCalled();
    expect(
      screen.getByText(/will come off your reading history/),
    ).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Delete" }));

    expect(await screen.findByText("11 Mar 2024")).toBeInTheDocument();
    expect(screen.queryByText("23 Sept 2026")).not.toBeInTheDocument();
    expect(screen.queryByText(/Read \d times/)).not.toBeInTheDocument();
    expect(onChanged).toHaveBeenCalledWith({
      timesRead: 1,
      finishedDate: "2024-03-11T00:00:00Z",
    });
    expect(mockGetReadings).toHaveBeenCalledTimes(1);
  });

  it("shows nothing once the last finish is gone", async () => {
    mockGetReadings.mockResolvedValue([older]);
    mockDeleteReading.mockResolvedValue([]);
    const onChanged = vi.fn();
    render(
      <BookPage start={{ ...entry, timesRead: 1 }} onChanged={onChanged} />,
    );

    await userEvent.click(
      await screen.findByRole("button", { name: "Delete 11 Mar 2024" }),
    );
    await userEvent.click(screen.getByRole("button", { name: "Delete" }));

    await waitFor(() =>
      expect(onChanged).toHaveBeenCalledWith({
        timesRead: 0,
        finishedDate: null,
      }),
    );
    expect(screen.queryByText("Finished")).not.toBeInTheDocument();
  });
});
