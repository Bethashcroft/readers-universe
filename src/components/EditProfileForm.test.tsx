import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import EditProfileForm from "./EditProfileForm";
import type { ProfileResponse } from "../api/profile";

const { mockUpdateProfile, mockUploadAvatar } = vi.hoisted(() => ({
  mockUpdateProfile: vi.fn(),
  mockUploadAvatar: vi.fn(),
}));

vi.mock("../api/profile", () => ({
  updateProfile: mockUpdateProfile,
  uploadAvatar: mockUploadAvatar,
}));

const profile: ProfileResponse = {
  userName: "beth",
  displayName: "Beth",
  bio: "Reads too much.",
  vintedUrl: "",
  avatarUrl: "",
  joinedDate: "2026-01-01T00:00:00Z",
  usernameChangeableOn: null,
  isPrivate: false,
  followState: "self",
  followerCount: 0,
  followingCount: 0,
  canView: true,
  trusted: false,
  trustsMe: false,
};

function renderForm(overrides: Partial<ProfileResponse> = {}) {
  const onSave = vi.fn();
  const onCancel = vi.fn();
  render(
    <EditProfileForm
      profile={{ ...profile, ...overrides }}
      onSave={onSave}
      onAvatarChange={vi.fn()}
      onCancel={onCancel}
    />,
  );
  return { onSave, onCancel };
}

describe("EditProfileForm", () => {
  beforeEach(() => {
    mockUpdateProfile.mockReset();
    mockUploadAvatar.mockReset();
  });

  it("saves the edited fields, including the private toggle", async () => {
    const saved = { ...profile, displayName: "Beth A", isPrivate: true };
    mockUpdateProfile.mockResolvedValue(saved);
    const { onSave } = renderForm();

    const displayName = screen.getByLabelText("Display Name");
    await userEvent.clear(displayName);
    await userEvent.type(displayName, "Beth A");
    await userEvent.click(
      screen.getByRole("switch", { name: "Private account" }),
    );
    await userEvent.click(screen.getByRole("button", { name: "Save" }));

    expect(mockUpdateProfile).toHaveBeenCalledWith({
      userName: "beth",
      displayName: "Beth A",
      bio: "Reads too much.",
      vintedUrl: "",
      isPrivate: true,
    });
    expect(onSave).toHaveBeenCalledWith(saved);
  });

  it("cancel closes without saving", async () => {
    const { onSave, onCancel } = renderForm();

    await userEvent.click(screen.getByRole("button", { name: "Cancel" }));

    expect(onCancel).toHaveBeenCalled();
    expect(mockUpdateProfile).not.toHaveBeenCalled();
    expect(onSave).not.toHaveBeenCalled();
  });

  it("shows the server's error and stays open", async () => {
    mockUpdateProfile.mockRejectedValue(new Error("Username is taken"));
    const { onSave } = renderForm();

    await userEvent.click(screen.getByRole("button", { name: "Save" }));

    expect(screen.getByText("Username is taken")).toBeInTheDocument();
    expect(onSave).not.toHaveBeenCalled();
  });

  it("locks the username when it was changed recently", () => {
    const nextWeek = new Date(Date.now() + 7 * 86400000).toISOString();
    renderForm({ usernameChangeableOn: nextWeek });

    expect(screen.getByLabelText("Username")).toBeDisabled();
    expect(
      screen.getByText(/You can change your username again on/),
    ).toBeInTheDocument();
  });
});
