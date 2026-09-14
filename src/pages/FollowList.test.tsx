import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import FollowList from "./FollowList";

const {
  mockGetFollowers,
  mockGetFollowing,
  mockGetFollowRequests,
  mockApprove,
  mockDecline,
} = vi.hoisted(() => ({
  mockGetFollowers: vi.fn(),
  mockGetFollowing: vi.fn(),
  mockGetFollowRequests: vi.fn(),
  mockApprove: vi.fn(),
  mockDecline: vi.fn(),
}));

vi.mock("../api/follows", () => ({
  getFollowers: mockGetFollowers,
  getFollowing: mockGetFollowing,
  getFollowRequests: mockGetFollowRequests,
  approveFollow: mockApprove,
  declineFollow: mockDecline,
  followReader: vi.fn(),
  unfollowReader: vi.fn(),
}));

vi.mock("../context/useAuth", () => ({
  useAuth: () => ({ user: { userName: "beth" } }),
}));

const sophie = {
  userName: "bookdragon",
  displayName: "Sophie Bell",
  avatarUrl: "",
  bio: "Fantasy and a lot of tea.",
  followState: "following",
};

function renderList(mode: "followers" | "following", username = "beth") {
  return render(
    <MemoryRouter initialEntries={[`/profile/${username}/${mode}`]}>
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
    mockGetFollowRequests.mockReset();
    mockApprove.mockReset();
    mockDecline.mockReset();
    mockGetFollowRequests.mockResolvedValue([]);
  });

  it("lists followers with a link to each profile", async () => {
    mockGetFollowers.mockResolvedValue([sophie]);
    renderList("followers", "rebel");

    expect(await screen.findByText("Sophie Bell")).toBeInTheDocument();
    expect(mockGetFollowers).toHaveBeenCalledWith("rebel");
    expect(mockGetFollowRequests).not.toHaveBeenCalled();
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

  it("shows pending requests on your own followers page and lets you approve", async () => {
    mockGetFollowers.mockResolvedValue([]);
    mockGetFollowRequests
      .mockResolvedValueOnce([
        {
          userName: "quietpages",
          displayName: "Tom Ashby",
          avatarUrl: "",
          requestedDate: new Date().toISOString(),
        },
      ])
      .mockResolvedValueOnce([]);
    mockApprove.mockResolvedValue({ approved: true });
    renderList("followers");

    expect(await screen.findByText("Requests (1)")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Approve" }));

    expect(mockApprove).toHaveBeenCalledWith("quietpages");
    expect(await screen.findByText("No followers yet.")).toBeInTheDocument();
    expect(screen.queryByText("Requests (1)")).not.toBeInTheDocument();
  });
});
