import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import TrustButton from "./TrustButton";

const { mockTrust, mockUntrust } = vi.hoisted(() => ({
  mockTrust: vi.fn(),
  mockUntrust: vi.fn(),
}));

vi.mock("../api/trust", () => ({
  trustReader: mockTrust,
  untrustReader: mockUntrust,
}));

function renderButton(trusted: boolean, onChange = vi.fn()) {
  render(
    <TrustButton
      username="rebel"
      displayName="Rebel Ashcroft"
      trusted={trusted}
      onChange={onChange}
    />,
  );
  return onChange;
}

describe("TrustButton", () => {
  beforeEach(() => {
    mockTrust.mockReset();
    mockUntrust.mockReset();
  });

  it("asks before adding someone and warns they will be notified", async () => {
    mockTrust.mockResolvedValue({ trusted: true });
    const onChange = renderButton(false);

    await userEvent.click(screen.getByRole("button", { name: "Trust" }));

    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(
      screen.getByText("They will be notified that you added them."),
    ).toBeInTheDocument();
    expect(mockTrust).not.toHaveBeenCalled();

    await userEvent.click(screen.getByRole("button", { name: "Add to club" }));

    expect(mockTrust).toHaveBeenCalledWith("rebel");
    expect(onChange).toHaveBeenCalledWith(true);
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("cancelling changes nothing", async () => {
    const onChange = renderButton(false);

    await userEvent.click(screen.getByRole("button", { name: "Trust" }));
    await userEvent.click(screen.getByRole("button", { name: "Cancel" }));

    expect(mockTrust).not.toHaveBeenCalled();
    expect(onChange).not.toHaveBeenCalled();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("asks before removing someone too", async () => {
    mockUntrust.mockResolvedValue({ trusted: false });
    const onChange = renderButton(true);

    await userEvent.click(
      screen.getByRole("button", { name: "Remove from Trusted" }),
    );
    await userEvent.click(screen.getByRole("button", { name: "Remove" }));

    expect(mockUntrust).toHaveBeenCalledWith("rebel");
    expect(onChange).toHaveBeenCalledWith(false);
  });

  it("shows the error inside the dialog when the request fails", async () => {
    mockTrust.mockRejectedValue(new Error("Something broke"));
    const onChange = renderButton(false);

    await userEvent.click(screen.getByRole("button", { name: "Trust" }));
    await userEvent.click(screen.getByRole("button", { name: "Add to club" }));

    expect(screen.getByText("Something broke")).toBeInTheDocument();
    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(onChange).not.toHaveBeenCalled();
  });
});
