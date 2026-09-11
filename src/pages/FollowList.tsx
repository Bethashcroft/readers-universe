import { useState, useEffect, useCallback } from "react";
import { useParams, Link } from "react-router-dom";
import { getFollowers, getFollowing } from "../api/follows";
import type { FollowListResponse, FollowState } from "../api/follows";
import FollowButton from "../components/FollowButton";
import ReaderRow from "../components/ReaderRow";
import ErrorState from "../components/ErrorState";
import { usePageTitle } from "../hooks/usePageTitle";
import "./FollowList.css";

type FollowListProps = {
  mode: "followers" | "following";
};

function FollowList({ mode }: FollowListProps) {
  const { username } = useParams();
  const [readers, setReaders] = useState<FollowListResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);

  const title = mode === "followers" ? "Followers" : "Following";

  usePageTitle(title);

  const load = useCallback(async () => {
    if (!username) return;

    setLoading(true);
    setLoadError(false);

    try {
      const data =
        mode === "followers"
          ? await getFollowers(username)
          : await getFollowing(username);
      setReaders(data);
    } catch (err) {
      console.error("Failed to load follow list:", err);
      setLoadError(true);
    } finally {
      setLoading(false);
    }
  }, [username, mode]);

  useEffect(() => {
    load();
  }, [load]);

  const handleFollowChange = (userName: string, state: FollowState) => {
    setReaders((current) =>
      current.map((reader) =>
        reader.userName === userName ? { ...reader, followState: state } : reader,
      ),
    );
  };

  if (loading) {
    return <p>Loading {title.toLowerCase()}...</p>;
  }

  if (loadError) {
    return (
      <ErrorState
        message="We couldn't load this list. Check your connection and try again."
        onRetry={load}
      />
    );
  }

  return (
    <div className="follow-list">
      <Link className="follow-list-back" to={`/profile/${username}`}>
        Back to @{username}
      </Link>
      <h1>{title}</h1>

      {readers.length === 0 ? (
        <p className="follow-list-empty">
          {mode === "followers"
            ? "No followers yet."
            : "Not following anyone yet."}
        </p>
      ) : (
        <div className="reader-rows">
          {readers.map((reader) => (
            <ReaderRow
              key={reader.userName}
              userName={reader.userName}
              displayName={reader.displayName}
              avatarUrl={reader.avatarUrl}
              bio={reader.bio}
              action={
                <FollowButton
                  username={reader.userName}
                  state={reader.followState}
                  onChange={(state) =>
                    handleFollowChange(reader.userName, state)
                  }
                />
              }
            />
          ))}
        </div>
      )}
    </div>
  );
}

export default FollowList;
