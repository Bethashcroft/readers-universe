import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import ActivityFeed from "./ActivityFeed";
import type { ActivityResponse } from "../api/feed";

const {
  mockLikeActivity,
  mockUnlikeActivity,
  mockGetComments,
  mockAddComment,
  mockEditComment,
  mockDeleteComment,
} = vi.hoisted(() => ({
  mockLikeActivity: vi.fn(),
  mockUnlikeActivity: vi.fn(),
  mockGetComments: vi.fn(),
  mockAddComment: vi.fn(),
  mockEditComment: vi.fn(),
  mockDeleteComment: vi.fn(),
}));

vi.mock("../api/feed", () => ({
  likeActivity: mockLikeActivity,
  unlikeActivity: mockUnlikeActivity,
  getComments: mockGetComments,
  addComment: mockAddComment,
  editComment: mockEditComment,
  deleteComment: mockDeleteComment,
}));

const comment = (
  id: number,
  text: string,
  mine = false,
  editedDate: string | null = null,
) => ({
  id,
  text,
  date: new Date().toISOString(),
  editedDate,
  userName: "quietpages",
  displayName: "Tom Ashby",
  avatarUrl: "",
  canEdit: mine,
  canDelete: mine,
});

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
    likeCount: 0,
    likedByMe: false,
    commentCount: 0,
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
  beforeEach(() => {
    vi.clearAllMocks();
  });

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

  it("fills the heart and bumps the count when you like a row", async () => {
    mockLikeActivity.mockResolvedValue({ likeCount: 3, likedByMe: true });
    renderFeed(async () => [activity(1, { likeCount: 2 })]);

    const heart = await screen.findByRole("button", { name: "Like" });
    expect(heart).toHaveTextContent("2");

    await userEvent.click(heart);

    const filled = await screen.findByRole("button", { name: "Unlike" });
    expect(filled).toHaveAttribute("aria-pressed", "true");
    expect(filled).toHaveTextContent("3");
    expect(mockLikeActivity).toHaveBeenCalledWith(1);
  });

  it("takes a like back", async () => {
    mockUnlikeActivity.mockResolvedValue({ likeCount: 0, likedByMe: false });
    renderFeed(async () => [activity(1, { likeCount: 1, likedByMe: true })]);

    await userEvent.click(await screen.findByRole("button", { name: "Unlike" }));

    const empty = await screen.findByRole("button", { name: "Like" });
    expect(empty).toHaveAttribute("aria-pressed", "false");
    expect(empty).not.toHaveTextContent("1");
    expect(mockUnlikeActivity).toHaveBeenCalledWith(1);
  });

  it("opens the comments only when you ask for them", async () => {
    mockGetComments.mockResolvedValue([comment(5, "Loved this one")]);
    renderFeed(async () => [activity(1, { commentCount: 1 })]);

    expect(mockGetComments).not.toHaveBeenCalled();

    await userEvent.click(await screen.findByRole("button", { name: "1" }));

    expect(await screen.findByText(/Loved this one/)).toBeInTheDocument();
    expect(mockGetComments).toHaveBeenCalledWith(1);
  });

  it("posts a comment and updates the count on the row", async () => {
    mockGetComments.mockResolvedValue([]);
    mockAddComment.mockResolvedValue([comment(5, "First!", true)]);
    renderFeed(async () => [activity(1)]);

    await userEvent.click(
      await screen.findByRole("button", { name: "Comment" }),
    );
    await userEvent.type(
      await screen.findByLabelText("Write a comment"),
      "First!",
    );
    await userEvent.click(screen.getByRole("button", { name: "Post" }));

    expect(await screen.findByText(/First!/)).toBeInTheDocument();
    expect(mockAddComment).toHaveBeenCalledWith(1, "First!");
    expect(screen.getByRole("button", { name: "1" })).toBeInTheDocument();
    expect(screen.getByLabelText("Write a comment")).toHaveValue("");
  });

  it("deletes a comment you are allowed to remove", async () => {
    mockGetComments.mockResolvedValue([comment(5, "Oops", true)]);
    mockDeleteComment.mockResolvedValue([]);
    renderFeed(async () => [activity(1, { commentCount: 1 })]);

    await userEvent.click(await screen.findByRole("button", { name: "1" }));
    await userEvent.click(await screen.findByRole("button", { name: "Delete" }));

    expect(screen.queryByText(/Oops/)).not.toBeInTheDocument();
    expect(mockDeleteComment).toHaveBeenCalledWith(5);
    expect(
      screen.getByRole("button", { name: "Comment" }),
    ).toBeInTheDocument();
  });

  it("hides Edit and Delete on other people's comments", async () => {
    mockGetComments.mockResolvedValue([comment(5, "Not yours")]);
    renderFeed(async () => [activity(1, { commentCount: 1 })]);

    await userEvent.click(await screen.findByRole("button", { name: "1" }));

    expect(await screen.findByText(/Not yours/)).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Delete" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Edit" }),
    ).not.toBeInTheDocument();
  });

  it("edits your own comment in place and marks it edited", async () => {
    mockGetComments.mockResolvedValue([comment(5, "Lvoed this", true)]);
    mockEditComment.mockResolvedValue([
      comment(5, "Loved this", true, new Date().toISOString()),
    ]);
    renderFeed(async () => [activity(1, { commentCount: 1 })]);

    await userEvent.click(await screen.findByRole("button", { name: "1" }));
    expect(screen.queryByText(/\(edited\)/)).not.toBeInTheDocument();

    await userEvent.click(await screen.findByRole("button", { name: "Edit" }));

    const box = screen.getByLabelText("Edit your comment");
    expect(box).toHaveValue("Lvoed this");

    await userEvent.clear(box);
    await userEvent.type(box, "Loved this");
    await userEvent.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByText(/Loved this/)).toBeInTheDocument();
    expect(screen.getByText(/\(edited\)/)).toBeInTheDocument();
    expect(mockEditComment).toHaveBeenCalledWith(5, "Loved this");
    expect(
      screen.queryByLabelText("Edit your comment"),
    ).not.toBeInTheDocument();
  });

  it("Cancel leaves the comment as it was", async () => {
    mockGetComments.mockResolvedValue([comment(5, "As written", true)]);
    renderFeed(async () => [activity(1, { commentCount: 1 })]);

    await userEvent.click(await screen.findByRole("button", { name: "1" }));
    await userEvent.click(await screen.findByRole("button", { name: "Edit" }));
    await userEvent.type(
      screen.getByLabelText("Edit your comment"),
      " and more",
    );
    await userEvent.click(screen.getByRole("button", { name: "Cancel" }));

    expect(await screen.findByText(/As written/)).toBeInTheDocument();
    expect(mockEditComment).not.toHaveBeenCalled();
  });
});
