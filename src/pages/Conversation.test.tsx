import { render, screen, act, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import Conversation from "./Conversation";
import type { ConversationResponse } from "../api/messages";

const {
  mockGetConversation,
  mockSendMessage,
  mockMarkConversationRead,
  mockConn,
} = vi.hoisted(() => ({
  mockGetConversation: vi.fn(),
  mockSendMessage: vi.fn(),
  mockMarkConversationRead: vi.fn().mockResolvedValue({ marked: 1 }),
  mockConn: {
    on: vi.fn(),
    off: vi.fn(),
    invoke: vi.fn().mockResolvedValue(undefined),
  },
}));

vi.mock("../api/messages", () => ({
  getConversation: mockGetConversation,
  sendMessage: mockSendMessage,
  markConversationRead: mockMarkConversationRead,
  announceMessagesRead: vi.fn(),
}));

vi.mock("../realtime/connection", () => ({
  getChatConnection: () => mockConn,
  startChatConnection: vi.fn().mockResolvedValue(mockConn),
  stopChatConnection: vi.fn(),
}));

vi.mock("../context/useAuth", () => ({
  useAuth: () => ({
    user: {
      token: "t",
      userId: "me",
      userName: "me",
      displayName: "Me",
    },
  }),
}));

const conversation: ConversationResponse = {
  bookTitle: "The Hobbit",
  otherUserName: "Rebel",
  messages: [
    {
      id: 1,
      senderId: "me",
      senderName: "Me",
      text: "Is Saturday ok?",
      date: "2026-07-14T10:00:00Z",
    },
    {
      id: 2,
      senderId: "other",
      senderName: "Rebel",
      text: "Saturday works!",
      date: "2026-07-14T10:05:00Z",
    },
  ],
};

function renderConversation() {
  return render(
    <MemoryRouter initialEntries={["/messages/5"]}>
      <Routes>
        <Route path="/messages/:requestId" element={<Conversation />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("Conversation", () => {
  beforeEach(() => {
    mockGetConversation.mockReset();
    mockSendMessage.mockReset();
    mockMarkConversationRead.mockClear();
    mockConn.on.mockClear();
    mockConn.off.mockClear();
    mockConn.invoke.mockClear();
  });

  it("renders the header and the message thread", async () => {
    mockGetConversation.mockResolvedValue(conversation);
    renderConversation();

    expect(await screen.findByText("The Hobbit")).toBeInTheDocument();
    expect(screen.getByText("Chatting with Rebel")).toBeInTheDocument();
    expect(screen.getByText("Is Saturday ok?")).toBeInTheDocument();
    expect(screen.getByText("Saturday works!")).toBeInTheDocument();
    expect(mockGetConversation).toHaveBeenCalledWith(5);
  });

  it("sends a message and appends it to the thread", async () => {
    mockGetConversation.mockResolvedValue(conversation);
    mockSendMessage.mockResolvedValue({
      id: 3,
      senderId: "me",
      senderName: "Me",
      text: "See you at 2pm",
      date: "2026-07-14T10:10:00Z",
    });
    const user = userEvent.setup();
    renderConversation();

    await screen.findByText("The Hobbit");

    await user.type(
      screen.getByPlaceholderText("Message Rebel"),
      "See you at 2pm",
    );
    await user.click(screen.getByRole("button", { name: "Send" }));

    expect(mockSendMessage).toHaveBeenCalledWith(5, "See you at 2pm");
    expect(await screen.findByText("See you at 2pm")).toBeInTheDocument();
  });

  it("appends messages that arrive over the live connection", async () => {
    mockGetConversation.mockResolvedValue(conversation);
    renderConversation();

    await screen.findByText("The Hobbit");
    await waitFor(() => expect(mockConn.on).toHaveBeenCalled());

    const handler = mockConn.on.mock.calls.find(
      ([event]) => event === "NewMessage",
    )?.[1];

    await act(async () => {
      handler({
        id: 9,
        senderId: "other",
        senderName: "Rebel",
        text: "Just arrived live!",
        date: "2026-07-14T10:15:00Z",
      });
    });

    expect(screen.getByText("Just arrived live!")).toBeInTheDocument();
  });

  it("marks a live message from them as read, but not your own echo", async () => {
    mockGetConversation.mockResolvedValue(conversation);
    renderConversation();

    await screen.findByText("The Hobbit");
    await waitFor(() => expect(mockConn.on).toHaveBeenCalled());

    const handler = mockConn.on.mock.calls.find(
      ([event]) => event === "NewMessage",
    )?.[1];

    await act(async () => {
      handler({
        id: 21,
        senderId: "other",
        senderName: "Rebel",
        text: "Theirs",
        date: "2026-07-14T10:15:00Z",
      });
    });

    expect(mockMarkConversationRead).toHaveBeenCalledWith(5);
    mockMarkConversationRead.mockClear();

    await act(async () => {
      handler({
        id: 22,
        senderId: "me",
        senderName: "Me",
        text: "Mine",
        date: "2026-07-14T10:16:00Z",
      });
    });

    expect(mockMarkConversationRead).not.toHaveBeenCalled();
  });

  it("coalesces a burst of live messages instead of one call each", async () => {
    mockGetConversation.mockResolvedValue(conversation);
    renderConversation();

    await screen.findByText("The Hobbit");
    await waitFor(() => expect(mockConn.on).toHaveBeenCalled());

    const handler = mockConn.on.mock.calls.find(
      ([event]) => event === "NewMessage",
    )?.[1];

    const message = (id: number) => ({
      id,
      senderId: "other",
      senderName: "Rebel",
      text: `Message ${id}`,
      date: "2026-07-14T10:15:00Z",
    });

    await act(async () => {
      handler(message(31));
      handler(message(32));
      handler(message(33));
    });

    // One in flight, one follow-up covering everything that arrived meanwhile.
    expect(mockMarkConversationRead).toHaveBeenCalledTimes(2);
  });

  it("leaves messages unread while the tab is in the background", async () => {
    const hidden = vi
      .spyOn(document, "visibilityState", "get")
      .mockReturnValue("hidden");

    mockGetConversation.mockResolvedValue(conversation);
    renderConversation();

    await screen.findByText("The Hobbit");
    await waitFor(() => expect(mockConn.on).toHaveBeenCalled());

    const handler = mockConn.on.mock.calls.find(
      ([event]) => event === "NewMessage",
    )?.[1];

    await act(async () => {
      handler({
        id: 44,
        senderId: "other",
        senderName: "Rebel",
        text: "While you were away",
        date: "2026-07-14T10:15:00Z",
      });
    });

    expect(mockMarkConversationRead).not.toHaveBeenCalled();
    hidden.mockRestore();
  });

  it("shows an empty state when there are no messages yet", async () => {
    mockGetConversation.mockResolvedValue({ ...conversation, messages: [] });
    renderConversation();

    expect(
      await screen.findByText(
        "No messages yet. Say hi and sort out the details!",
      ),
    ).toBeInTheDocument();
  });
});
