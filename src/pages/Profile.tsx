import { useState, useEffect, useRef, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useAuth } from "../context/useAuth";
import { API_ORIGIN } from "../api/client";
import { getUserProfile, updateProfile, uploadAvatar } from "../api/profile";
import type { ProfileResponse } from "../api/profile";
import { getUserBooks } from "../api/books";
import type { BookResponse } from "../api/books";
import BookCard from "../components/BookCard";
import AvatarCropModal from "../components/AvatarCropModal";
import VintedButton from "../components/VintedButton";
import ErrorState from "../components/ErrorState";
import { usePageTitle } from "../hooks/usePageTitle";
import "../styles/forms.css";
import "./Profile.css";

function Profile() {
  const { username } = useParams();
  const { user, updateUser } = useAuth();
  const navigate = useNavigate();
  const [profile, setProfile] = useState<ProfileResponse | null>(null);
  const [books, setBooks] = useState<BookResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [userName, setUserName] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [bio, setBio] = useState("");
  const [vintedUrl, setVintedUrl] = useState("");
  const [error, setError] = useState("");
  const [loadError, setLoadError] = useState(false);
  const [uploadingAvatar, setUploadingAvatar] = useState(false);
  const [pendingAvatarFile, setPendingAvatarFile] = useState<File | null>(
    null,
  );
  const avatarInputRef = useRef<HTMLInputElement>(null);

  usePageTitle(profile ? profile.displayName : "Profile");

  const loadProfile = useCallback(async () => {
    if (!username) return;
    setLoading(true);
    setLoadError(false);
    try {
      const data = await getUserProfile(username);
      setProfile(data);
      setUserName(data.userName);
      setDisplayName(data.displayName);
      setBio(data.bio);
      setVintedUrl(data.vintedUrl);
      const userBooks = await getUserBooks(username);
      setBooks(userBooks);
    } catch (err) {
      console.error("Failed to fetch profile:", err);
      setLoadError(true);
    } finally {
      setLoading(false);
    }
  }, [username]);

  useEffect(() => {
    loadProfile();
  }, [loadProfile]);

  const handleSave = async () => {
    setError("");

    try {
      const updated = await updateProfile({
        userName,
        displayName,
        bio,
        vintedUrl,
      });
      setProfile(updated);
      setEditing(false);
      if (user && updated.userName !== user.userName) {
        updateUser({ userName: updated.userName });
        navigate(`/profile/${updated.userName}`);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update profile");
    }
  };

  const handleAvatarChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setError("");
    setPendingAvatarFile(file);
    e.target.value = "";
  };

  const handleAvatarSave = async (cropped: File) => {
    setUploadingAvatar(true);

    try {
      const updated = await uploadAvatar(cropped);
      setProfile(updated);
      setPendingAvatarFile(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to upload photo");
      setPendingAvatarFile(null);
    } finally {
      setUploadingAvatar(false);
    }
  };

  if (loading) {
    return <p>Loading profile...</p>;
  }

  if (loadError) {
    return (
      <ErrorState
        message="We couldn't load this profile. Check your connection and try again."
        onRetry={loadProfile}
      />
    );
  }

  if (!profile) {
    return <p>Profile not found</p>;
  }

  const isOwnProfile = user?.userName === profile.userName;
  const usernameLockedUntil =
    profile.usernameChangeableOn &&
    new Date(profile.usernameChangeableOn) > new Date()
      ? new Date(profile.usernameChangeableOn)
      : null;

  return (
    <div className="profile">
      <div className="profile-header">
        <div className="profile-avatar">
          {profile.avatarUrl ? (
            <img src={`${API_ORIGIN}${profile.avatarUrl}`} alt="" />
          ) : (
            profile.displayName.charAt(0)
          )}
        </div>
        <div className="profile-info">
          {editing ? (
            <div className="edit-profile-form">
              <label>Profile Photo</label>
              <input
                ref={avatarInputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                className="avatar-input"
                onChange={handleAvatarChange}
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
              {error && <p className="form-error">{error}</p>}
              <div className="edit-profile-actions">
                <button className="btn btn-primary" onClick={handleSave}>
                  Save
                </button>
                <button
                  className="btn btn-secondary"
                  onClick={() => {
                    setEditing(false);
                    setUserName(profile.userName);
                    setDisplayName(profile.displayName);
                    setBio(profile.bio);
                    setVintedUrl(profile.vintedUrl);
                  }}
                >
                  Cancel
                </button>
              </div>
            </div>
          ) : (
            <>
              <h1>{profile.displayName}</h1>
              <p className="profile-username">@{profile.userName}</p>
              <p className="profile-bio">{profile.bio || "No bio yet"}</p>
              {profile.vintedUrl && (
                <VintedButton
                  href={profile.vintedUrl}
                  label="Vinted Profile"
                  className="profile-vinted"
                />
              )}
              <div className="profile-meta">
                {isOwnProfile && (
                  <button
                    className="btn btn-secondary"
                    onClick={() => setEditing(true)}
                  >
                    Edit Profile
                  </button>
                )}
                <div className="profile-detail-card">
                  <h2>Member Since</h2>
                  <p>
                    {new Date(profile.joinedDate).toLocaleDateString("en-GB", {
                      month: "long",
                      year: "numeric",
                    })}
                  </p>
                </div>
              </div>
            </>
          )}
        </div>
      </div>

      <section className="profile-books">
        <h2>
          {isOwnProfile ? "My Books" : `${profile.displayName}'s Books`} (
          {books.length})
        </h2>
        <div className="book-grid">
          {books.map((book) => (
            <BookCard key={book.id} book={book} />
          ))}
        </div>
        {books.length === 0 && <p>No books added yet.</p>}
      </section>

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

export default Profile;
