import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import Avatar from "./Avatar";
import "./ReaderRow.css";

type ReaderRowProps = {
  userName: string;
  displayName: string;
  avatarUrl: string;
  bio?: string;
  meta?: ReactNode;
  action?: ReactNode;
};

function ReaderRow({
  userName,
  displayName,
  avatarUrl,
  bio,
  meta,
  action,
}: ReaderRowProps) {
  return (
    <div className="reader-row">
      <Link className="reader-row-link" to={`/profile/${userName}`}>
        <Avatar url={avatarUrl} name={displayName} size={48} />
        <span className="reader-row-text">
          <span className="reader-row-name">{displayName}</span>
          <span className="reader-row-handle">@{userName}</span>
          {bio && <span className="reader-row-bio">{bio}</span>}
          {meta && <span className="reader-row-meta">{meta}</span>}
        </span>
      </Link>
      {action && <div className="reader-row-action">{action}</div>}
    </div>
  );
}

export default ReaderRow;
