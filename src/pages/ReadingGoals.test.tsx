import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Routes, Route, useParams } from "react-router-dom";
import ReadingGoals from "./ReadingGoals";

const { mockGetGoals, mockSetGoal, mockRemoveGoal } = vi.hoisted(() => ({
  mockGetGoals: vi.fn(),
  mockSetGoal: vi.fn(),
  mockRemoveGoal: vi.fn(),
}));

vi.mock("../api/goals", () => ({
  getGoals: mockGetGoals,
  setGoal: mockSetGoal,
  removeGoal: mockRemoveGoal,
}));

const thisYear = new Date().getFullYear();

function renderGoals() {
  return render(
    <MemoryRouter initialEntries={["/reading-goals"]}>
      <Routes>
        <Route path="/reading-goals" element={<ReadingGoals />} />
        <Route path="/reading-goals/:year" element={<YearStub />} />
      </Routes>
    </MemoryRouter>,
  );
}

function YearStub() {
  const { year } = useParams();
  return <p>Year page for {year}</p>;
}

describe("ReadingGoals", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("offers to set a goal and shows what you've read so far", async () => {
    mockGetGoals.mockResolvedValue([
      { year: thisYear, target: null, booksRead: 12 },
    ]);
    mockSetGoal.mockResolvedValue({ year: thisYear, target: 40, booksRead: 12 });
    renderGoals();

    expect(
      await screen.findByText("You've read 12 books so far this year."),
    ).toBeInTheDocument();

    await userEvent.type(
      screen.getByLabelText("How many books this year?"),
      "40",
    );
    await userEvent.click(screen.getByRole("button", { name: "Set goal" }));

    expect(mockSetGoal).toHaveBeenCalledWith(thisYear, 40);
    expect(await screen.findByText(/of 40 books/)).toBeInTheDocument();
    expect(
      screen.getByRole("progressbar", { name: `${thisYear} reading goal` }),
    ).toHaveAttribute("aria-valuenow", "30");
  });

  it("lets you change your goal", async () => {
    mockGetGoals.mockResolvedValue([
      { year: thisYear, target: 40, booksRead: 12 },
    ]);
    mockSetGoal.mockResolvedValue({ year: thisYear, target: 60, booksRead: 12 });
    renderGoals();

    await userEvent.click(
      await screen.findByRole("button", { name: "Edit goal" }),
    );
    const box = screen.getByLabelText("How many books this year?");
    expect(box).toHaveValue("40");

    await userEvent.clear(box);
    await userEvent.type(box, "60");
    await userEvent.click(screen.getByRole("button", { name: "Save" }));

    expect(mockSetGoal).toHaveBeenCalledWith(thisYear, 60);
    expect(await screen.findByText(/of 60 books/)).toBeInTheDocument();
  });

  it("asks before removing a goal", async () => {
    mockGetGoals.mockResolvedValue([
      { year: thisYear, target: 40, booksRead: 12 },
    ]);
    mockRemoveGoal.mockResolvedValue({
      year: thisYear,
      target: null,
      booksRead: 12,
    });
    renderGoals();

    await userEvent.click(
      await screen.findByRole("button", { name: "Edit goal" }),
    );
    await userEvent.click(screen.getByRole("button", { name: "Remove goal" }));

    expect(mockRemoveGoal).not.toHaveBeenCalled();

    await userEvent.click(screen.getByRole("button", { name: "Remove" }));

    expect(mockRemoveGoal).toHaveBeenCalledWith(thisYear);
    expect(
      await screen.findByText("You've read 12 books so far this year."),
    ).toBeInTheDocument();
  });

  it("lists past years, with or without a goal", async () => {
    mockGetGoals.mockResolvedValue([
      { year: thisYear, target: null, booksRead: 0 },
      { year: thisYear - 1, target: 40, booksRead: 43 },
      { year: thisYear - 2, target: 50, booksRead: 31 },
      { year: thisYear - 3, target: null, booksRead: 1 },
    ]);
    renderGoals();

    expect(await screen.findByText("Goal of 40 reached")).toBeInTheDocument();
    expect(screen.getByText("Goal was 50")).toBeInTheDocument();
    expect(screen.getByText("No goal set")).toBeInTheDocument();
    expect(screen.getByText("1 book")).toBeInTheDocument();
  });

  it("links this year and every past year to their year in books", async () => {
    const lastYear = thisYear - 1;
    mockGetGoals.mockResolvedValue([
      { year: thisYear, target: null, booksRead: 3 },
      { year: lastYear, target: 40, booksRead: 43 },
    ]);
    renderGoals();

    expect(
      await screen.findByRole("link", { name: `See your ${thisYear} so far` }),
    ).toHaveAttribute("href", `/reading-goals/${thisYear}`);

    await userEvent.click(
      screen.getByRole("link", { name: `See your ${lastYear} in books` }),
    );

    expect(
      await screen.findByText(`Year page for ${lastYear}`),
    ).toBeInTheDocument();
  });

  it("only takes digits in the goal box", async () => {
    mockGetGoals.mockResolvedValue([
      { year: thisYear, target: null, booksRead: 0 },
    ]);
    renderGoals();

    const box = await screen.findByLabelText("How many books this year?");
    await userEvent.type(box, "4a0!");

    expect(box).toHaveValue("40");
  });

  it("edits every past year at once and only saves what changed", async () => {
    const lastYear = thisYear - 1;
    const yearBefore = thisYear - 2;
    mockGetGoals.mockResolvedValue([
      { year: thisYear, target: null, booksRead: 0 },
      { year: lastYear, target: null, booksRead: 43 },
      { year: yearBefore, target: 50, booksRead: 31 },
    ]);
    mockSetGoal.mockResolvedValue({ year: lastYear, target: 40, booksRead: 43 });
    renderGoals();

    await userEvent.click(
      await screen.findByRole("button", { name: "Edit past years' goals" }),
    );

    expect(screen.getByLabelText(`Goal for ${yearBefore}`)).toHaveValue("50");

    await userEvent.type(screen.getByLabelText(`Goal for ${lastYear}`), "40");
    await userEvent.click(screen.getByRole("button", { name: "Save" }));

    expect(mockSetGoal).toHaveBeenCalledTimes(1);
    expect(mockSetGoal).toHaveBeenCalledWith(lastYear, 40);
    expect(await screen.findByText("Goal of 40 reached")).toBeInTheDocument();
    expect(screen.getByText("Goal was 50")).toBeInTheDocument();
  });

  it("clearing a past year's box removes that goal", async () => {
    const lastYear = thisYear - 1;
    mockGetGoals.mockResolvedValue([
      { year: thisYear, target: null, booksRead: 0 },
      { year: lastYear, target: 40, booksRead: 43 },
    ]);
    mockRemoveGoal.mockResolvedValue({
      year: lastYear,
      target: null,
      booksRead: 43,
    });
    renderGoals();

    await userEvent.click(
      await screen.findByRole("button", { name: "Edit past years' goals" }),
    );
    await userEvent.clear(screen.getByLabelText(`Goal for ${lastYear}`));
    await userEvent.click(screen.getByRole("button", { name: "Save" }));

    expect(mockRemoveGoal).toHaveBeenCalledWith(lastYear);
    expect(await screen.findByText("No goal set")).toBeInTheDocument();
  });
});
