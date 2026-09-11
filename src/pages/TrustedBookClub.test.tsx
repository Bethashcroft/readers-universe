import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import TrustedBookClub from "./TrustedBookClub";

const { mockGetTrusted, mockUntrust } = vi.hoisted(() => ({
  mockGetTrusted: vi.fn(),
  mockUntrust: vi.fn(),
}));

vi.mock("../api/trust", () => ({
  getTrustedReaders: mockGetTrusted,
  untrustReader: mockUntrust,
}));

const sophie = {
  userName: "bookdragon",
  displayName: "Sophie Bell",
  avatarUrl: "",
  bio: "Fantasy and a lot of tea.",
  date: new Date().toISOString(),
};

function renderClub() {
  return render(
    <MemoryRouter>
      <TrustedBookClub />
    </MemoryRouter>,
  );
}

describe("TrustedBookClub", () => {
  beforeEach(() => {
    mockGetTrusted.mockReset();
    mockUntrust.mockReset();
  });

  it("points you at Find Readers when the club is empty", async () => {
    mockGetTrusted.mockResolvedValue([]);
    renderClub();

    expect(await screen.findByText("Nobody in your club yet.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Find readers" })).toHaveAttribute(
      "href",
      "/readers",
    );
  });

  it("removing someone asks first, then drops them from the list", async () => {
    mockGetTrusted.mockResolvedValue([sophie]);
    mockUntrust.mockResolvedValue({ trusted: false });
    renderClub();

    await screen.findByText("Sophie Bell");
    await userEvent.click(screen.getByRole("button", { name: "Remove" }));

    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(mockUntrust).not.toHaveBeenCalled();

    await userEvent.click(
      screen.getByRole("dialog").querySelector("button.btn-primary")!,
    );

    expect(mockUntrust).toHaveBeenCalledWith("bookdragon");
    expect(screen.queryByText("Sophie Bell")).not.toBeInTheDocument();
    expect(screen.getByText("Nobody in your club yet.")).toBeInTheDocument();
  });
});
