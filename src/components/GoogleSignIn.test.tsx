import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Routes, Route, useLocation } from "react-router-dom";
import GoogleSignIn from "./GoogleSignIn";

const { mockGoogleSignIn, mockSignInWith } = vi.hoisted(() => ({
  mockGoogleSignIn: vi.fn(),
  mockSignInWith: vi.fn(),
}));

vi.mock("../api/auth", () => ({ googleSignIn: mockGoogleSignIn }));

vi.mock("../context/useAuth", () => ({
  useAuth: () => ({ signInWith: mockSignInWith }),
}));

vi.mock("./GoogleButton", () => ({
  default: ({ onCredential }: { onCredential: (token: string) => void }) => (
    <button type="button" onClick={() => onCredential("google-token")}>
      Continue with Google
    </button>
  ),
}));

function WelcomeStub() {
  const state = useLocation().state as { suggestedUserName: string };
  return <p>Welcome page for {state.suggestedUserName}</p>;
}

function renderLogin() {
  return render(
    <MemoryRouter initialEntries={["/login"]}>
      <Routes>
        <Route path="/login" element={<GoogleSignIn />} />
        <Route path="/" element={<p>Home page</p>} />
        <Route path="/welcome" element={<WelcomeStub />} />
      </Routes>
    </MemoryRouter>,
  );
}

const auth = {
  token: "t",
  userId: "u1",
  userName: "bookdragon",
  displayName: "Sophie Bell",
};

describe("GoogleSignIn", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv("VITE_GOOGLE_CLIENT_ID", "client-123");
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("stays hidden until Google is set up", () => {
    vi.stubEnv("VITE_GOOGLE_CLIENT_ID", "");
    renderLogin();

    expect(
      screen.queryByRole("button", { name: "Continue with Google" }),
    ).not.toBeInTheDocument();
    expect(screen.queryByText("or")).not.toBeInTheDocument();
  });

  it("signs a returning reader straight in", async () => {
    mockGoogleSignIn.mockResolvedValue({ auth, signUp: null });
    renderLogin();

    await userEvent.click(
      screen.getByRole("button", { name: "Continue with Google" }),
    );

    expect(mockGoogleSignIn).toHaveBeenCalledWith("google-token");
    expect(mockSignInWith).toHaveBeenCalledWith(auth);
    expect(await screen.findByText("Home page")).toBeInTheDocument();
  });

  it("sends someone new to choose a username", async () => {
    mockGoogleSignIn.mockResolvedValue({
      auth: null,
      signUp: {
        suggestedUserName: "sophiebell",
        displayName: "Sophie Bell",
        email: "sophie@gmail.com",
      },
    });
    renderLogin();

    await userEvent.click(
      screen.getByRole("button", { name: "Continue with Google" }),
    );

    expect(
      await screen.findByText("Welcome page for sophiebell"),
    ).toBeInTheDocument();
    expect(mockSignInWith).not.toHaveBeenCalled();
  });

  it("shows what went wrong", async () => {
    mockGoogleSignIn.mockRejectedValue(
      new Error("An account already uses that email. Sign in with your password instead."),
    );
    renderLogin();

    await userEvent.click(
      screen.getByRole("button", { name: "Continue with Google" }),
    );

    expect(
      await screen.findByText(/already uses that email/),
    ).toBeInTheDocument();
  });
});
