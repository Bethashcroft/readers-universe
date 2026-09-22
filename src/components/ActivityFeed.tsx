import { useState, useEffect, type ReactNode } from "react";
import type { ActivityResponse, LikesResponse } from "../api/feed";
import ActivityItem from "./ActivityItem";
import "./ActivityFeed.css";

type ActivityFeedProps = {
  load: () => Promise<ActivityResponse[]>;
  empty: ReactNode;
  initialCount?: number;
};

function ActivityFeed({ load, empty, initialCount = 10 }: ActivityFeedProps) {
  const [items, setItems] = useState<ActivityResponse[] | null>(null);
  const [error, setError] = useState("");
  const [expanded, setExpanded] = useState(false);

  useEffect(() => {
    let active = true;

    const run = async () => {
      try {
        const result = await load();
        if (active) setItems(result);
      } catch (err) {
        if (active) {
          setError(err instanceof Error ? err.message : "Failed to load");
        }
      }
    };

    run();

    return () => {
      active = false;
    };
  }, [load]);

  if (error) {
    return <p className="form-error">{error}</p>;
  }

  if (items === null) {
    return <p className="activity-feed-note">Loading...</p>;
  }

  if (items.length === 0) {
    return <div className="activity-feed-empty">{empty}</div>;
  }

  const visible = expanded ? items : items.slice(0, initialCount);

  const updateItem = (id: number, changes: Partial<ActivityResponse>) =>
    setItems((current) =>
      current === null
        ? current
        : current.map((a) => (a.id === id ? { ...a, ...changes } : a)),
    );

  const updateLikes = (id: number, likes: LikesResponse) =>
    updateItem(id, likes);

  const updateCommentCount = (id: number, commentCount: number) =>
    updateItem(id, { commentCount });

  return (
    <div className="activity-feed">
      {visible.map((activity) => (
        <ActivityItem
          key={activity.id}
          activity={activity}
          onLikesChanged={updateLikes}
          onCommentCountChanged={updateCommentCount}
        />
      ))}
      {!expanded && items.length > initialCount && (
        <button
          type="button"
          className="btn btn-secondary activity-feed-more"
          onClick={() => setExpanded(true)}
        >
          Show {items.length - initialCount} more
        </button>
      )}
    </div>
  );
}

export default ActivityFeed;
