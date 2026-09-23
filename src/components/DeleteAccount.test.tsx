import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import DeleteAccount from "./DeleteAccount";

const { mockDeleteAccount, mockLogout } = vi.hoisted(() => ({
  mockDeleteAccount: vi.fn(),
  mockLogout: vi.fn(),
}));

vi.mock("../api/auth", () => ({ deleteAccount: mockDeleteAccount }));

vi.mock("../context/useAuth", () => ({
  useAuth: () => ({ logout: mockLogout }),
}));

function renderDelete() {
  return render(
    <MemoryRouter initialEntries={["/profile/bookdragon"]}>
      <Routes>
        <Route
          path="/profile/bookdragon"
          element={<DeleteAccount userName="bookdragon" />}
        />
        <Route path="/" element={<p>Home page</p>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("DeleteAccount", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("won't delete until you type your username", async () => {
    renderDelete();

    await userEvent.click(
      screen.getByRole("button", { name: "Delete my account" }),
    );
    const confirm = screen.getByRole("button", { name: "Delete forever" });

    expect(confirm).toBeDisabled();

    await userEvent.type(screen.getByLabelText(/to confirm/), "bookdrag");
    expect(confirm).toBeDisabled();

    await userEvent.type(screen.getByLabelText(/to confirm/), "on");
    expect(confirm).toBeEnabled();
  });

  it("deletes the account, signs you out and takes you home", async () => {
    mockDeleteAccount.mockResolvedValue(undefined);
    renderDelete();

    await userEvent.click(
      screen.getByRole("button", { name: "Delete my account" }),
    );
    await userEvent.type(screen.getByLabelText(/to confirm/), "BookDragon");
    await userEvent.click(screen.getByRole("button", { name: "Delete forever" }));

    expect(mockDeleteAccount).toHaveBeenCalledWith("BookDragon");
    expect(mockLogout).toHaveBeenCalled();
    expect(await screen.findByText("Home page")).toBeInTheDocument();
  });

  it("keeps you signed in and says why if it fails", async () => {
    mockDeleteAccount.mockRejectedValue(
      new Error("We couldn't delete your account. Please try again."),
    );
    renderDelete();

    await userEvent.click(
      screen.getByRole("button", { name: "Delete my account" }),
    );
    await userEvent.type(screen.getByLabelText(/to confirm/), "bookdragon");
    await userEvent.click(screen.getByRole("button", { name: "Delete forever" }));

    expect(
      await screen.findByText(/couldn't delete your account/),
    ).toBeInTheDocument();
    expect(mockLogout).not.toHaveBeenCalled();
  });
});
