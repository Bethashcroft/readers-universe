import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import NotificationBell from "./NotificationBell";
import type { NotificationResponse } from "../api/notifications";

const { mockGetNotifications, mockGetCount, mockMarkRead, mockClear } =
  vi.hoisted(() => ({
    mockGetNotifications: vi.fn(),
    mockGetCount: vi.fn(),
    mockMarkRead: vi.fn(),
    mockClear: vi.fn(),
  }));

vi.mock("../api/notifications", () => ({
  getNotifications: mockGetNotifications,
  getNotificationCount: mockGetCount,
  markNotificationsRead: mockMarkRead,
  clearNotifications: mockClear,
}));

vi.mock("../realtime/connection", () => {
  const conn = { on: vi.fn(), off: vi.fn() };
  return { getChatConnection: () => conn };
});

const approved: NotificationResponse = {
  id: 1,
  type: "follow-approved",
  isRead: false,
  date: new Date().toISOString(),
  actorUserName: "bookdragon",
  actorDisplayName: "Sophie Bell",
  actorAvatarUrl: "",
};

function renderBell() {
  return render(
    <MemoryRouter>
      <ul>
        <NotificationBell />
      </ul>
    </MemoryRouter>,
  );
}

describe("NotificationBell", () => {
  beforeEach(() => {
    mockGetNotifications.mockReset();
    mockGetCount.mockReset();
    mockMarkRead.mockReset();
    mockClear.mockReset();
    mockGetNotifications.mockResolvedValue([approved]);
    mockGetCount.mockResolvedValue({ count: 1 });
    mockMarkRead.mockResolvedValue({ count: 1 });
    mockClear.mockResolvedValue({ count: 1 });
  });

  it("shows the unread count on the bell", async () => {
    renderBell();

    expect(
      await screen.findByRole("button", { name: "Notifications, 1 unread" }),
    ).toBeInTheDocument();
  });

  it("opening the panel lists notifications and marks them read", async () => {
    renderBell();

    await userEvent.click(
      await screen.findByRole("button", { name: "Notifications, 1 unread" }),
    );

    expect(await screen.findByText("Sophie Bell")).toBeInTheDocument();
    expect(
      screen.getByText("accepted your follow request"),
    ).toBeInTheDocument();
    expect(mockMarkRead).toHaveBeenCalled();
    expect(
      screen.getByRole("button", { name: "Notifications" }),
    ).toBeInTheDocument();
  });

  it("clear all empties the panel", async () => {
    renderBell();

    await userEvent.click(
      await screen.findByRole("button", { name: "Notifications, 1 unread" }),
    );
    await userEvent.click(
      await screen.findByRole("button", { name: "Clear all" }),
    );

    expect(mockClear).toHaveBeenCalled();
    expect(screen.getByText("Nothing new yet.")).toBeInTheDocument();
  });
});
