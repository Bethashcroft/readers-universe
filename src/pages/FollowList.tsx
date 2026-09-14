import { useState, useEffect, useCallback } from "react";
import { useParams, Link } from "react-router-dom";
import { useAuth } from "../context/useAuth";
import {
  getFollowers,
  getFollowing,
  getFollowRequests,
  approveFollow,
  declineFollow,
} from "../api/follows";
import type {
  FollowListResponse,
  FollowRequestResponse,
  FollowState,
} from "../api/follows";
import FollowButton from "../components/FollowButton";
import ReaderRow from "../components/ReaderRow";
import ErrorState from "../components/ErrorState";
import { usePageTitle } from "../hooks/usePageTitle";
import { formatDate } from "../utils/dates";
import "./FollowList.css";

type FollowListProps = {
  mode: "followers" | "following";
};

function FollowList({ mode }: FollowListProps) {
  const { username } = useParams();
  const { user } = useAuth();
  const [readers, setReaders] = useState<FollowListResponse[]>([]);
  const [requests, setRequests] = useState<FollowRequestResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [error, setError] = useState("");
  const [deciding, setDeciding] = useState("");

  const title = mode === "followers" ? "Followers" : "Following";
  const ownFollowers = mode === "followers" && user?.userName === username;

  usePageTitle(title);

  const load = useCallback(async () => {
    if (!username) return;

    setLoading(true);
    setLoadError(false);

    try {
      const [list, pending] = await Promise.all([
        mode === "followers" ? getFollowers(username) : getFollowing(username),
        ownFollowers ? getFollowRequests() : Promise.resolve([]),
      ]);
      setReaders(list);
      setRequests(pending);
    } catch (err) {
      console.error("Failed to load follow list:", err);
      setLoadError(true);
    } finally {
      setLoading(false);
    }
  }, [username, mode, ownFollowers]);

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

  const handleDecision = async (userName: string, approve: boolean) => {
    setError("");
    setDeciding(userName);

    try {
      if (approve) {
        await approveFollow(userName);
      } else {
        await declineFollow(userName);
      }

      await load();
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to update follow request",
      );
    } finally {
      setDeciding("");
    }
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
      {error && <p className="form-error">{error}</p>}

      {requests.length > 0 && (
        <section className="follow-list-section">
          <h2 className="eyebrow">Requests ({requests.length})</h2>
          <div className="reader-rows">
            {requests.map((request) => (
              <ReaderRow
                key={request.userName}
                userName={request.userName}
                displayName={request.displayName}
                avatarUrl={request.avatarUrl}
                meta={formatDate(request.requestedDate)}
                action={
                  <>
                    <button
                      className="btn btn-primary"
                      onClick={() => handleDecision(request.userName, true)}
                      disabled={deciding === request.userName}
                    >
                      Approve
                    </button>
                    <button
                      className="btn btn-secondary"
                      onClick={() => handleDecision(request.userName, false)}
                      disabled={deciding === request.userName}
                    >
                      Decline
                    </button>
                  </>
                }
              />
            ))}
          </div>
        </section>
      )}

      {readers.length === 0 ? (
        <p className="follow-list-empty">
          {mode === "followers"
            ? "No followers yet."
            : "Not following anyone yet."}
        </p>
      ) : (
        <section className="follow-list-section">
          {requests.length > 0 && (
            <h2 className="eyebrow">Following you ({readers.length})</h2>
          )}
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
        </section>
      )}
    </div>
  );
}

export default FollowList;
