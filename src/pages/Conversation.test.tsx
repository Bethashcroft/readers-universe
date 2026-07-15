import { render, screen, act, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import Conversation from "./Conversation";
import type { ConversationResponse } from "../api/messages";

const { mockGetConversation, mockSendMessage, mockConn } = vi.hoisted(() => ({
  mockGetConversation: vi.fn(),
  mockSendMessage: vi.fn(),
  mockConn: {
    on: vi.fn(),
    off: vi.fn(),
    invoke: vi.fn().mockResolvedValue(undefined),
  },
}));

vi.mock("../api/messages", () => ({
  getConversation: mockGetConversation,
  sendMessage: mockSendMessage,
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

  it("shows an empty state when there are no messages yet", async () => {
    mockGetConversation.mockResolvedValue({ ...conversation, messages: [] });
    renderConversation();

    expect(
      await screen.findByText("No messages yet. Say hi and sort out the details!"),
    ).toBeInTheDocument();
  });
});
