import { Link } from "react-router-dom";
import type { ActivityResponse, ActivityType } from "../api/feed";
import { timeAgo } from "../utils/dates";
import Avatar from "./Avatar";
import BookCover from "./BookCover";
import { percentThrough } from "../utils/progress";
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

function verbFor(activity: ActivityResponse) {
  if (activity.type !== "progress") return verbs[activity.type];

  const percent = percentThrough(activity.page, activity.pageCount);
  return percent === null
    ? `is on page ${activity.page} of`
    : `is ${percent}% through`;
}

type ActivityItemProps = {
  activity: ActivityResponse;
};

function ActivityItem({ activity }: ActivityItemProps) {
  const { book, targetUser, rating } = activity;

  return (
    <div className="activity-item">
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
          {rating && (
            <span className="activity-rating">
              {"★".repeat(rating)}
              {"☆".repeat(5 - rating)}
            </span>
          )}
        </p>
        <span className="activity-time">{timeAgo(activity.date)}</span>
      </div>

      {book && (
        <Link to={`/book/${book.id}`} className="activity-cover">
          <BookCover src={book.coverUrl} title={book.title} />
        </Link>
      )}
    </div>
  );
}

export default ActivityItem;
