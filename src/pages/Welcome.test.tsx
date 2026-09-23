import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import Welcome from "./Welcome";

const { mockGoogleRegister, mockSignInWith } = vi.hoisted(() => ({
  mockGoogleRegister: vi.fn(),
  mockSignInWith: vi.fn(),
}));

vi.mock("../api/auth", () => ({ googleRegister: mockGoogleRegister }));

vi.mock("../context/useAuth", () => ({
  useAuth: () => ({ signInWith: mockSignInWith }),
}));

const fromGoogle = {
  idToken: "google-token",
  suggestedUserName: "sophiebell",
  displayName: "Sophie Bell",
  email: "sophie@gmail.com",
};

function renderWelcome(state: object | null) {
  return render(
    <MemoryRouter initialEntries={[{ pathname: "/welcome", state }]}>
      <Routes>
        <Route path="/welcome" element={<Welcome />} />
        <Route path="/" element={<p>Home page</p>} />
        <Route path="/login" element={<p>Login page</p>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("Welcome", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("suggests a username and signs you in once you've chosen", async () => {
    const auth = { token: "t", userId: "u1", userName: "bookdragon", displayName: "Sophie" };
    mockGoogleRegister.mockResolvedValue(auth);
    renderWelcome(fromGoogle);

    const handle = screen.getByLabelText("Choose your username");
    expect(handle).toHaveValue("sophiebell");
    expect(screen.getByText("Signing up as sophie@gmail.com")).toBeInTheDocument();

    await userEvent.clear(handle);
    await userEvent.type(handle, "bookdragon");
    await userEvent.clear(screen.getByLabelText("Display name"));
    await userEvent.type(screen.getByLabelText("Display name"), "Sophie");
    await userEvent.click(screen.getByRole("button", { name: "Continue" }));

    expect(mockGoogleRegister).toHaveBeenCalledWith(
      "google-token",
      "bookdragon",
      "Sophie",
    );
    expect(mockSignInWith).toHaveBeenCalledWith(auth);
    expect(await screen.findByText("Home page")).toBeInTheDocument();
  });

  it("shows why a username was refused", async () => {
    mockGoogleRegister.mockRejectedValue(new Error("That username is taken."));
    renderWelcome(fromGoogle);

    await userEvent.click(screen.getByRole("button", { name: "Continue" }));

    expect(await screen.findByText("That username is taken.")).toBeInTheDocument();
    expect(mockSignInWith).not.toHaveBeenCalled();
  });

  it("sends you to log in if you arrive without coming from Google", () => {
    renderWelcome(null);

    expect(screen.getByText("Login page")).toBeInTheDocument();
  });
});
