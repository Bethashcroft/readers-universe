import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import MonthChart from "./MonthChart";

const months = [1, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0];

describe("MonthChart", () => {
  it("shows a month's count when you hover its bar", async () => {
    render(<MonthChart months={months} />);

    expect(screen.queryByText("2 books in March")).not.toBeInTheDocument();

    await userEvent.hover(screen.getByRole("img", { name: "2 books in March" }));

    expect(screen.getByText("2 books in March")).toBeInTheDocument();
  });

  it("shows a month's count when you tab to its bar", async () => {
    render(<MonthChart months={months} />);

    await userEvent.tab();

    expect(screen.getByText("1 book in January")).toBeInTheDocument();
  });

  it("has a table of every month for screen readers", () => {
    render(<MonthChart months={months} />);

    const table = screen.getByRole("table", {
      name: "Books finished each month",
    });

    expect(within(table).getAllByRole("row")).toHaveLength(13);
    expect(within(table).getByRole("row", { name: "March 2" })).toBeInTheDocument();
  });
});
