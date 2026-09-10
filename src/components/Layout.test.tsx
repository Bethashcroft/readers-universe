import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import Layout from "./Layout";

const navbar = () =>
  within(screen.getByRole("navigation", { name: "Primary" }));

const {
  mockUseAuth,
  mockGetMyRequests,
  mockGetUnreadCount,
  mockGetFollowRequests,
  mockGetNotifications,
  mockGetNotificationCount,
} = vi.hoisted(() => ({
  mockUseAuth: vi.fn(),
  mockGetMyRequests: vi.fn(),
  mockGetUnreadCount: vi.fn(),
  mockGetFollowRequests: vi.fn(),
  mockGetNotifications: vi.fn(),
  mockGetNotificationCount: vi.fn(),
}));

vi.mock("../api/notifications", () => ({
  getNotifications: mockGetNotifications,
  getNotificationCount: mockGetNotificationCount,
  markNotificationsRead: vi.fn(),
}));

vi.mock("../context/useAuth", () => ({
  useAuth: mockUseAuth,
}));

vi.mock("../api/borrow", () => ({
  getMyRequests: mockGetMyRequests,
}));

vi.mock("../api/follows", () => ({
  getFollowRequests: mockGetFollowRequests,
}));

vi.mock("../api/messages", () => ({
  getUnreadCount: mockGetUnreadCount,
  messagesReadEvent: "readers:messages-read",
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
    mockGetFollowRequests.mockReset();
    mockGetFollowRequests.mockResolvedValue([]);
    mockGetNotifications.mockReset();
    mockGetNotifications.mockResolvedValue([]);
    mockGetNotificationCount.mockReset();
    mockGetNotificationCount.mockResolvedValue({ count: 0 });
  });

  it("shows only Login and Register when logged out", () => {
    mockUseAuth.mockReturnValue({ user: null, logout: vi.fn() });
    renderLayout();

    expect(navbar().getByText("Login")).toBeInTheDocument();
    expect(navbar().getByText("Register")).toBeInTheDocument();
    expect(navbar().queryByText("My Shelves")).not.toBeInTheDocument();
    expect(navbar().queryByText("Add Books")).not.toBeInTheDocument();
    expect(navbar().queryByText("Browse")).not.toBeInTheDocument();
  });

  it("shows the two menu triggers when logged in", () => {
    mockUseAuth.mockReturnValue({ user: loggedInUser, logout: vi.fn() });
    renderLayout();

    expect(
      navbar().getByRole("button", { name: /Library/ }),
    ).toBeInTheDocument();
    expect(
      navbar().getByRole("button", { name: /Account/ }),
    ).toBeInTheDocument();
    expect(navbar().queryByText("Login")).not.toBeInTheDocument();
  });

  it("keeps the app links inside the Library menu until it is opened", async () => {
    mockUseAuth.mockReturnValue({ user: loggedInUser, logout: vi.fn() });
    renderLayout();

    expect(navbar().queryByRole("link", { name: "Browse" })).toBeNull();

    await userEvent.click(navbar().getByRole("button", { name: /Library/ }));

    expect(navbar().getByRole("link", { name: "My Shelves" })).toBeInTheDocument();
    expect(navbar().getByRole("link", { name: "Add Books" })).toBeInTheDocument();
    expect(navbar().getByRole("link", { name: "Browse" })).toBeInTheDocument();
    expect(navbar().getByRole("link", { name: "Requests" })).toBeInTheDocument();
  });

  it("puts Profile and Log Out inside the Account menu", async () => {
    mockUseAuth.mockReturnValue({ user: loggedInUser, logout: vi.fn() });
    renderLayout();

    await userEvent.click(navbar().getByRole("button", { name: /Account/ }));

    expect(navbar().getByRole("link", { name: "Profile" })).toHaveAttribute(
      "href",
      "/profile/me",
    );
    expect(navbar().getByRole("button", { name: "Log Out" })).toBeInTheDocument();
  });

  it("closes an open menu when Escape is pressed", async () => {
    mockUseAuth.mockReturnValue({ user: loggedInUser, logout: vi.fn() });
    renderLayout();

    await userEvent.click(navbar().getByRole("button", { name: /Library/ }));
    expect(navbar().getByRole("link", { name: "Browse" })).toBeInTheDocument();

    await userEvent.keyboard("{Escape}");
    expect(navbar().queryByRole("link", { name: "Browse" })).toBeNull();
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
