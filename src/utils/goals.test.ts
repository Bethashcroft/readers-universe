import { scheduleMessage } from "./goals";

const midYear = new Date(2026, 6, 3);

describe("scheduleMessage", () => {
  it("is on schedule halfway through with half the books", () => {
    expect(scheduleMessage(50, 25, midYear)).toBe("Right on schedule");
  });

  it("counts books ahead", () => {
    expect(scheduleMessage(50, 28, midYear)).toBe("3 books ahead of schedule");
  });

  it("counts books behind, singular when it's one", () => {
    expect(scheduleMessage(50, 24, midYear)).toBe("1 book behind schedule");
  });

  it("celebrates once the goal is met", () => {
    expect(scheduleMessage(50, 50, midYear)).toBe("You've hit your goal.");
  });
});
