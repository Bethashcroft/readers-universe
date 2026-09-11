import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import FollowButton from "./FollowButton";

const { mockFollow, mockUnfollow } = vi.hoisted(() => ({
  mockFollow: vi.fn(),
  mockUnfollow: vi.fn(),
}));

vi.mock("../api/follows", () => ({
  followReader: mockFollow,
  unfollowReader: mockUnfollow,
}));

describe("FollowButton", () => {
  beforeEach(() => {
    mockFollow.mockReset();
    mockUnfollow.mockReset();
  });

  it("renders nothing on your own profile", () => {
    const { container } = render(
      <FollowButton username="me" state="self" onChange={vi.fn()} />,
    );

    expect(container).toBeEmptyDOMElement();
  });

  it("follows and reports the state the server returns", async () => {
    mockFollow.mockResolvedValue({ state: "requested" });
    const onChange = vi.fn();
    render(<FollowButton username="rebel" state="none" onChange={onChange} />);

    await userEvent.click(screen.getByRole("button", { name: "Follow" }));

    expect(mockFollow).toHaveBeenCalledWith("rebel");
    expect(onChange).toHaveBeenCalledWith("requested");
  });

  it("unfollows from either the Following or Requested state", async () => {
    mockUnfollow.mockResolvedValue({ state: "none" });
    const onChange = vi.fn();
    render(
      <FollowButton username="rebel" state="requested" onChange={onChange} />,
    );

    await userEvent.click(screen.getByRole("button", { name: "Requested" }));

    expect(mockUnfollow).toHaveBeenCalledWith("rebel");
    expect(onChange).toHaveBeenCalledWith("none");
  });
});
