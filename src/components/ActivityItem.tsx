import { useState } from "react";
import { Link } from "react-router-dom";
import { likeActivity, unlikeActivity } from "../api/feed";
import type { ActivityResponse, ActivityType, LikesResponse } from "../api/feed";
import { timeAgo } from "../utils/dates";
import Avatar from "./Avatar";
import BookCover from "./BookCover";
import CommentThread from "./CommentThread";
import { percentThrough } from "../utils/progress";
import { formatLabels } from "../types/book";
import "./ActivityItem.css";

const verbs: Record<Exclude<ActivityType, "progress">, string> = {
  "started-reading": "started reading",
  finished: "finished",
  "did-not-finish": "didn't finish",
  "wants-to-read": "wants to read",
  reviewed: "reviewed",
  offered: "is offering",
  followed: "started following",
};

const showsFormat: ActivityType[] = [
  "started-reading",
  "progress",
  "finished",
  "did-not-finish",
];

function verbFor(activity: ActivityResponse) {
  if (activity.type !== "progress") return verbs[activity.type];

  const percent = percentThrough(activity.page, activity.pageCount);
  return percent === null
    ? `is on page ${activity.page} of`
    : `is ${percent}% through`;
}

type ActivityItemProps = {
  activity: ActivityResponse;
  onLikesChanged: (id: number, likes: LikesResponse) => void;
  onCommentCountChanged: (id: number, count: number) => void;
};

function ActivityItem({
  activity,
  onLikesChanged,
  onCommentCountChanged,
}: ActivityItemProps) {
  const { book, targetUser, rating } = activity;
  const [busy, setBusy] = useState(false);
  const [showComments, setShowComments] = useState(false);

  const toggleLike = async () => {
    setBusy(true);
    try {
      const likes = activity.likedByMe
        ? await unlikeActivity(activity.id)
        : await likeActivity(activity.id);
      onLikesChanged(activity.id, likes);
    } catch (err) {
      console.error("Like failed:", err);
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="activity-item">
      <div className="activity-row">
      <Link to={`/profile/${activity.userName}`} className="activity-avatar">
        <Avatar url={activity.avatarUrl} name={activity.displayName} size={40} />
      </Link>

      <div className="activity-text">
        <p>
          <Link to={`/profile/${activity.userName}`} className="activity-name">
            {activity.displayName}
          </Link>{" "}
          {verbFor(activity)}{" "}
          {book && (
            <Link to={`/book/${book.id}`} className="activity-subject">
              {book.title}
            </Link>
          )}
          {targetUser && (
            <Link
              to={`/profile/${targetUser.userName}`}
              className="activity-subject"
            >
              {targetUser.displayName}
            </Link>
          )}
          {activity.type === "offered" && " to borrow"}
          {activity.format && showsFormat.includes(activity.type) && (
            <span className="badge badge-violet activity-format">
              {formatLabels[activity.format]}
            </span>
          )}
          {rating && (
            <span className="activity-rating">
              {"★".repeat(rating)}
              {"☆".repeat(5 - rating)}
            </span>
          )}
        </p>
        <div className="activity-meta">
          <span className="activity-time">{timeAgo(activity.date)}</span>
          <button
            type="button"
            className={`activity-like${activity.likedByMe ? " liked" : ""}`}
            aria-pressed={activity.likedByMe}
            aria-label={activity.likedByMe ? "Unlike" : "Like"}
            onClick={toggleLike}
            disabled={busy}
          >
            <svg viewBox="0 0 24 24" aria-hidden="true">
              <path d="M12 21s-7-4.6-9.3-9A5.5 5.5 0 0 1 12 6.3 5.5 5.5 0 0 1 21.3 12C19 16.4 12 21 12 21z" />
            </svg>
            {activity.likeCount > 0 && <span>{activity.likeCount}</span>}
          </button>
          <button
            type="button"
            className="activity-comment"
            aria-expanded={showComments}
            onClick={() => setShowComments((open) => !open)}
          >
            <svg viewBox="0 0 24 24" aria-hidden="true">
              <path d="M21 12a8 8 0 0 1-8 8H4l2-3a8 8 0 1 1 15-5z" />
            </svg>
            {activity.commentCount > 0 ? activity.commentCount : "Comment"}
          </button>
        </div>
      </div>

      {book && (
        <Link to={`/book/${book.id}`} className="activity-cover">
          <BookCover src={book.coverUrl} title={book.title} />
        </Link>
      )}
      </div>

      {showComments && (
        <CommentThread
          activityId={activity.id}
          onCountChanged={onCommentCountChanged}
        />
      )}
    </div>
  );
}

export default ActivityItem;
