import { useState, useEffect, useRef, useCallback } from "react";
import { useParams, useNavigate, Link } from "react-router-dom";
import { useAuth } from "../context/useAuth";
import { API_ORIGIN } from "../api/client";
import { getUserProfile, updateProfile, uploadAvatar } from "../api/profile";
import type { ProfileResponse } from "../api/profile";
import { getUserBooks } from "../api/books";
import type { LibraryEntryResponse } from "../api/books";
import type { FollowState } from "../api/follows";
import BookCard from "../components/BookCard";
import FollowButton from "../components/FollowButton";
import Toggle from "../components/Toggle";
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
  const [books, setBooks] = useState<LibraryEntryResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [userName, setUserName] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [bio, setBio] = useState("");
  const [vintedUrl, setVintedUrl] = useState("");
  const [isPrivate, setIsPrivate] = useState(false);
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
      setIsPrivate(data.isPrivate);
      setBooks(data.canView ? await getUserBooks(username) : []);
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

  const handleFollowChange = (state: FollowState) => {
    setProfile((current) => {
      if (!current) return current;

      const wasFollowing = current.followState === "following";
      const nowFollowing = state === "following";
      const change = Number(nowFollowing) - Number(wasFollowing);

      return {
        ...current,
        followState: state,
        followerCount: current.followerCount + change,
      };
    });

    if (profile?.isPrivate) {
      loadProfile();
    }
  };

  const applyOwnProfile = (updated: ProfileResponse) => {
    setProfile((current) =>
      current
        ? {
            ...updated,
            followState: current.followState,
            followerCount: current.followerCount,
            followingCount: current.followingCount,
          }
        : updated,
    );
  };

  const handleSave = async () => {
    setError("");

    if (!profile) return;

    try {
      const updated = await updateProfile({
        userName,
        displayName,
        bio,
        vintedUrl,
        isPrivate,
      });
      applyOwnProfile(updated);
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
      applyOwnProfile(updated);
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
              <Toggle
                id="isPrivate"
                label="Private account"
                hint="Only approved followers can see your shelves."
                checked={isPrivate}
                onChange={setIsPrivate}
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
                    setIsPrivate(profile.isPrivate);
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
                {isOwnProfile ? (
                  <button
                    className="btn btn-secondary"
                    onClick={() => setEditing(true)}
                  >
                    Edit Profile
                  </button>
                ) : (
                  <FollowButton
                    username={profile.userName}
                    state={profile.followState}
                    onChange={handleFollowChange}
                  />
                )}
                <Link
                  className="profile-detail-card profile-detail-link"
                  to={`/profile/${profile.userName}/followers`}
                >
                  <h2>Followers</h2>
                  <p>{profile.followerCount}</p>
                </Link>
                <Link
                  className="profile-detail-card profile-detail-link"
                  to={`/profile/${profile.userName}/following`}
                >
                  <h2>Following</h2>
                  <p>{profile.followingCount}</p>
                </Link>
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
        {profile.canView ? (
          <>
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
          </>
        ) : (
          <div className="profile-locked">
            <svg
              className="profile-locked-icon"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <rect x="5" y="10" width="14" height="10" rx="2" />
              <path d="M8 10 V7 a4 4 0 0 1 8 0 v3" />
            </svg>
            <h2>This account is private</h2>
            <p>Follow {profile.displayName} to see their shelves.</p>
          </div>
        )}
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
