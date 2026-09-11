import { render, screen } from "@testing-library/react";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import FollowList from "./FollowList";

const { mockGetFollowers, mockGetFollowing } = vi.hoisted(() => ({
  mockGetFollowers: vi.fn(),
  mockGetFollowing: vi.fn(),
}));

vi.mock("../api/follows", () => ({
  getFollowers: mockGetFollowers,
  getFollowing: mockGetFollowing,
  followReader: vi.fn(),
  unfollowReader: vi.fn(),
}));

function renderList(mode: "followers" | "following") {
  return render(
    <MemoryRouter initialEntries={[`/profile/beth/${mode}`]}>
      <Routes>
        <Route
          path="/profile/:username/followers"
          element={<FollowList mode="followers" />}
        />
        <Route
          path="/profile/:username/following"
          element={<FollowList mode="following" />}
        />
      </Routes>
    </MemoryRouter>,
  );
}

describe("FollowList", () => {
  beforeEach(() => {
    mockGetFollowers.mockReset();
    mockGetFollowing.mockReset();
  });

  it("lists followers with a link to each profile", async () => {
    mockGetFollowers.mockResolvedValue([
      {
        userName: "bookdragon",
        displayName: "Sophie Bell",
        avatarUrl: "",
        bio: "Fantasy and a lot of tea.",
        followState: "following",
      },
    ]);
    renderList("followers");

    expect(await screen.findByText("Sophie Bell")).toBeInTheDocument();
    expect(mockGetFollowers).toHaveBeenCalledWith("beth");
    expect(screen.getByRole("link", { name: /Sophie Bell/ })).toHaveAttribute(
      "href",
      "/profile/bookdragon",
    );
    expect(
      screen.getByRole("button", { name: "Following" }),
    ).toBeInTheDocument();
  });

  it("asks for the following list in following mode", async () => {
    mockGetFollowing.mockResolvedValue([]);
    renderList("following");

    expect(
      await screen.findByText("Not following anyone yet."),
    ).toBeInTheDocument();
    expect(mockGetFollowing).toHaveBeenCalledWith("beth");
    expect(mockGetFollowers).not.toHaveBeenCalled();
  });
});
