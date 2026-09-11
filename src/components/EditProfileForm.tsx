import { useState, useRef } from "react";
import { updateProfile, uploadAvatar } from "../api/profile";
import type { ProfileResponse } from "../api/profile";
import Toggle from "./Toggle";
import AvatarCropModal from "./AvatarCropModal";
import "../styles/forms.css";
import "./EditProfileForm.css";

type EditProfileFormProps = {
  profile: ProfileResponse;
  onSave: (updated: ProfileResponse) => void;
  onAvatarChange: (updated: ProfileResponse) => void;
  onCancel: () => void;
};

function EditProfileForm({
  profile,
  onSave,
  onAvatarChange,
  onCancel,
}: EditProfileFormProps) {
  const [userName, setUserName] = useState(profile.userName);
  const [displayName, setDisplayName] = useState(profile.displayName);
  const [bio, setBio] = useState(profile.bio);
  const [vintedUrl, setVintedUrl] = useState(profile.vintedUrl);
  const [isPrivate, setIsPrivate] = useState(profile.isPrivate);
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);
  const [uploadingAvatar, setUploadingAvatar] = useState(false);
  const [pendingAvatarFile, setPendingAvatarFile] = useState<File | null>(
    null,
  );
  const avatarInputRef = useRef<HTMLInputElement>(null);

  const usernameLockedUntil =
    profile.usernameChangeableOn &&
    new Date(profile.usernameChangeableOn) > new Date()
      ? new Date(profile.usernameChangeableOn)
      : null;

  const handleSave = async () => {
    setError("");
    setSaving(true);

    try {
      const updated = await updateProfile({
        userName,
        displayName,
        bio,
        vintedUrl,
        isPrivate,
      });
      onSave(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update profile");
    } finally {
      setSaving(false);
    }
  };

  const handleAvatarPick = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setError("");
    setPendingAvatarFile(file);
    e.target.value = "";
  };

  const handleAvatarSave = async (cropped: File) => {
    setUploadingAvatar(true);

    try {
      onAvatarChange(await uploadAvatar(cropped));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to upload photo");
    } finally {
      setPendingAvatarFile(null);
      setUploadingAvatar(false);
    }
  };

  return (
    <div className="edit-profile-form">
      <label>Profile Photo</label>
      <input
        ref={avatarInputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        className="avatar-input"
        onChange={handleAvatarPick}
      />
      <button
        type="button"
        className="btn btn-primary avatar-upload-btn"
        onClick={() => avatarInputRef.current?.click()}
        disabled={uploadingAvatar}
      >
        {uploadingAvatar ? "Uploading..." : "Upload New Photo"}
      </button>

      <label htmlFor="userName">Username</label>
      <input
        type="text"
        id="userName"
        value={userName}
        onChange={(e) => setUserName(e.target.value)}
        disabled={usernameLockedUntil !== null}
      />
      {usernameLockedUntil ? (
        <p className="username-note">
          You can change your username again on{" "}
          {usernameLockedUntil.toLocaleDateString("en-GB", {
            day: "numeric",
            month: "long",
            year: "numeric",
          })}
          .
        </p>
      ) : (
        <p className="username-note">
          Choose carefully. You can change this once every 30 days.
        </p>
      )}

      <label htmlFor="displayName">Display Name</label>
      <input
        type="text"
        id="displayName"
        value={displayName}
        onChange={(e) => setDisplayName(e.target.value)}
      />

      <label htmlFor="bio">Bio</label>
      <textarea
        id="bio"
        value={bio}
        onChange={(e) => setBio(e.target.value)}
        rows={3}
      />

      <label htmlFor="vintedUrl">Vinted Profile URL</label>
      <input
        type="url"
        id="vintedUrl"
        placeholder="https://www.vinted.co.uk/member/..."
        value={vintedUrl}
        onChange={(e) => setVintedUrl(e.target.value)}
      />

      <Toggle
        id="isPrivate"
        label="Private account"
        hint="Only approved followers can see your shelves."
        checked={isPrivate}
        onChange={setIsPrivate}
      />

      {error && <p className="form-error">{error}</p>}

      <div className="edit-profile-actions">
        <button
          type="button"
          className="btn btn-primary"
          onClick={handleSave}
          disabled={saving}
        >
          {saving ? "Saving..." : "Save"}
        </button>
        <button
          type="button"
          className="btn btn-secondary"
          onClick={onCancel}
          disabled={saving}
        >
          Cancel
        </button>
      </div>

      {pendingAvatarFile && (
        <AvatarCropModal
          file={pendingAvatarFile}
          saving={uploadingAvatar}
          onCancel={() => setPendingAvatarFile(null)}
          onSave={handleAvatarSave}
        />
      )}
    </div>
  );
}

export default EditProfileForm;
