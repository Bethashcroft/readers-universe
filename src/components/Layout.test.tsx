import { render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import Layout from "./Layout";

const navbar = () =>
  within(screen.getByRole("navigation", { name: "Primary" }));

const { mockUseAuth, mockGetMyRequests, mockGetUnreadCount } = vi.hoisted(
  () => ({
    mockUseAuth: vi.fn(),
    mockGetMyRequests: vi.fn(),
    mockGetUnreadCount: vi.fn(),
  }),
);

vi.mock("../context/useAuth", () => ({
  useAuth: mockUseAuth,
}));

vi.mock("../api/borrow", () => ({
  getMyRequests: mockGetMyRequests,
}));

vi.mock("../api/messages", () => ({
  getUnreadCount: mockGetUnreadCount,
}));

vi.mock("../realtime/connection", () => {
  const conn = { on: vi.fn(), off: vi.fn(), invoke: vi.fn() };
  return {
    getChatConnection: () => conn,
    startChatConnection: vi.fn().mockResolvedValue(conn),
    stopChatConnection: vi.fn(),
  };
});

function renderLayout() {
  return render(
    <MemoryRouter>
      <Layout />
    </MemoryRouter>,
  );
}

const loggedInUser = {
  token: "t",
  userId: "me",
  userName: "me",
  displayName: "Me",
};

describe("Layout", () => {
  beforeEach(() => {
    mockUseAuth.mockReset();
    mockGetMyRequests.mockReset();
    mockGetMyRequests.mockResolvedValue([]);
    mockGetUnreadCount.mockReset();
    mockGetUnreadCount.mockResolvedValue({ count: 0 });
  });

  it("shows only Login and Register when logged out", () => {
    mockUseAuth.mockReturnValue({ user: null, logout: vi.fn() });
    renderLayout();

    expect(navbar().getByText("Login")).toBeInTheDocument();
    expect(navbar().getByText("Register")).toBeInTheDocument();
    expect(navbar().queryByText("My Shelves")).not.toBeInTheDocument();
    expect(navbar().queryByText("Add Book")).not.toBeInTheDocument();
    expect(navbar().queryByText("Browse")).not.toBeInTheDocument();
  });

  it("shows the app links when logged in", () => {
    mockUseAuth.mockReturnValue({ user: loggedInUser, logout: vi.fn() });
    renderLayout();

    expect(navbar().getByText("My Shelves")).toBeInTheDocument();
    expect(navbar().getByText("Add Book")).toBeInTheDocument();
    expect(navbar().getByText("Browse")).toBeInTheDocument();
    expect(navbar().getByText("Logout")).toBeInTheDocument();
    expect(navbar().queryByText("Login")).not.toBeInTheDocument();
  });

  it("shows a badge with the count of incoming pending requests", async () => {
    mockUseAuth.mockReturnValue({ user: loggedInUser, logout: vi.fn() });
    mockGetMyRequests.mockResolvedValue([
      { id: 1, toUserId: "me", status: "pending" },
      { id: 2, toUserId: "me", status: "pending" },
      { id: 3, toUserId: "me", status: "accepted" },
      { id: 4, fromUserId: "me", toUserId: "other", status: "pending" },
    ]);
    renderLayout();

    expect(await screen.findByText("2")).toBeInTheDocument();
  });

  it("shows a badge with the unread message count", async () => {
    mockUseAuth.mockReturnValue({ user: loggedInUser, logout: vi.fn() });
    mockGetUnreadCount.mockResolvedValue({ count: 3 });
    renderLayout();

    expect(await screen.findByText("3")).toBeInTheDocument();
  });
});
