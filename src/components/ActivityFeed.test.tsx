import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import ActivityFeed from "./ActivityFeed";
import type { ActivityResponse } from "../api/feed";

function activity(
  id: number,
  overrides: Partial<ActivityResponse> = {},
): ActivityResponse {
  return {
    id,
    type: "finished",
    date: new Date().toISOString(),
    rating: null,
    page: null,
    pageCount: null,
    userName: "bookdragon",
    displayName: "Sophie Bell",
    avatarUrl: "",
    book: { id: 10, title: "Piranesi", author: "Susanna Clarke", coverUrl: "x" },
    targetUser: null,
    ...overrides,
  };
}

function renderFeed(load: () => Promise<ActivityResponse[]>, initialCount = 10) {
  return render(
    <MemoryRouter>
      <ActivityFeed
        load={load}
        empty={<p>Nothing here.</p>}
        initialCount={initialCount}
      />
    </MemoryRouter>,
  );
}

describe("ActivityFeed", () => {
  it("reads each event as a sentence", async () => {
    renderFeed(async () => [
      activity(1, { rating: 4 }),
      activity(2, { type: "started-reading" }),
      activity(3, { type: "did-not-finish" }),
      activity(4, { type: "offered" }),
      activity(5, {
        type: "followed",
        book: null,
        targetUser: { userName: "quietpages", displayName: "Tom Ashby" },
      }),
      activity(6, { type: "progress", page: 120, pageCount: 340 }),
      activity(7, { type: "progress", page: 120, pageCount: null }),
    ]);

    expect(await screen.findAllByText("Sophie Bell")).toHaveLength(7);
    expect(screen.getByText("is 35% through")).toBeInTheDocument();
    expect(screen.getByText("is on page 120 of")).toBeInTheDocument();
    expect(screen.getByText("finished")).toBeInTheDocument();
    expect(screen.getByText("★★★★☆")).toBeInTheDocument();
    expect(screen.getByText("started reading")).toBeInTheDocument();
    expect(screen.getByText("didn't finish")).toBeInTheDocument();
    expect(screen.getByText(/is offering/)).toBeInTheDocument();
    expect(screen.getByText(/to borrow/)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Tom Ashby" })).toHaveAttribute(
      "href",
      "/profile/quietpages",
    );
    expect(screen.getAllByRole("link", { name: "Piranesi" })[0]).toHaveAttribute(
      "href",
      "/book/10",
    );
  });

  it("shows the empty state when there is nothing", async () => {
    renderFeed(async () => []);

    expect(await screen.findByText("Nothing here.")).toBeInTheDocument();
  });

  it("shows the error when loading fails", async () => {
    renderFeed(async () => {
      throw new Error("Failed to load your feed");
    });

    expect(
      await screen.findByText("Failed to load your feed"),
    ).toBeInTheDocument();
  });

  it("collapses long lists behind Show more", async () => {
    renderFeed(async () => [1, 2, 3].map((n) => activity(n)), 2);

    expect(await screen.findAllByText("Sophie Bell")).toHaveLength(2);

    await userEvent.click(screen.getByRole("button", { name: "Show 1 more" }));

    expect(screen.getAllByText("Sophie Bell")).toHaveLength(3);
  });
});
