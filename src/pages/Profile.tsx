import { useState, useEffect, useCallback } from "react";
import { useParams, useNavigate, Link } from "react-router-dom";
import { useAuth } from "../context/useAuth";
import { getUserProfile } from "../api/profile";
import type { ProfileResponse } from "../api/profile";
import { getUserBooks } from "../api/books";
import type { LibraryEntryResponse } from "../api/books";
import type { FollowState } from "../api/follows";
import Avatar from "../components/Avatar";
import BookCard from "../components/BookCard";
import FollowButton from "../components/FollowButton";
import TrustButton from "../components/TrustButton";
import EditProfileForm from "../components/EditProfileForm";
import VintedButton from "../components/VintedButton";
import ErrorState from "../components/ErrorState";
import { usePageTitle } from "../hooks/usePageTitle";
import "./Profile.css";

function Profile() {
  const { username } = useParams();
  const { user, updateUser } = useAuth();
  const navigate = useNavigate();
  const [profile, setProfile] = useState<ProfileResponse | null>(null);
  const [books, setBooks] = useState<LibraryEntryResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [loadError, setLoadError] = useState(false);

  usePageTitle(profile ? profile.displayName : "Profile");

  const loadProfile = useCallback(async () => {
    if (!username) return;
    setLoading(true);
    setLoadError(false);
    setEditing(false);
    try {
      const data = await getUserProfile(username);
      setProfile(data);
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

  const handleSaved = (updated: ProfileResponse) => {
    applyOwnProfile(updated);
    setEditing(false);

    if (user && updated.userName !== user.userName) {
      updateUser({ userName: updated.userName });
      navigate(`/profile/${updated.userName}`);
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

  return (
    <div className="profile">
      <div className="profile-header">
        <Avatar url={profile.avatarUrl} name={profile.displayName} size={80} />
        <div className="profile-info">
          {editing && isOwnProfile ? (
            <EditProfileForm
              profile={profile}
              onSave={handleSaved}
              onAvatarChange={applyOwnProfile}
              onCancel={() => setEditing(false)}
            />
          ) : (
            <>
              <h1>{profile.displayName}</h1>
              <p className="profile-username">@{profile.userName}</p>
              <p className="profile-bio">{profile.bio || "No bio yet"}</p>
              {profile.trustsMe && (
                <p className="profile-trusts-me">
                  You're in {profile.displayName}'s Trusted Book Club, so you
                  can borrow their books.
                </p>
              )}
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
                  <>
                    <FollowButton
                      username={profile.userName}
                      state={profile.followState}
                      onChange={handleFollowChange}
                    />
                    <TrustButton
                      username={profile.userName}
                      displayName={profile.displayName}
                      trusted={profile.trusted}
                      onChange={(trusted) =>
                        setProfile(
                          (current) => current && { ...current, trusted },
                        )
                      }
                    />
                  </>
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
    </div>
  );
}

export default Profile;
