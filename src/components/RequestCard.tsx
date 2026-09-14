import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import type { BorrowRequestResponse } from "../api/borrow";
import { formatDate } from "../utils/dates";
import "./RequestCard.css";

type RequestCardProps = {
  request: BorrowRequestResponse;
  from: string;
  datePrefix?: string;
  showMessage?: boolean;
  actions?: ReactNode;
};

function RequestCard({
  request,
  from,
  datePrefix = "",
  showMessage = true,
  actions,
}: RequestCardProps) {
  return (
    <div className="request-card">
      <div className="request-details">
        <Link className="request-title" to={`/book/${request.bookId}`}>
          {request.bookTitle}
        </Link>
        <p className="request-from">{from}</p>
        {showMessage && request.message && (
          <p className="request-message">"{request.message}"</p>
        )}
        <p className="request-date">
          {datePrefix}
          {formatDate(request.date)}
        </p>
        <Link className="request-chat" to={`/messages/${request.id}`}>
          Open chat
        </Link>
      </div>
      {actions && <div className="request-actions">{actions}</div>}
    </div>
  );
}

export default RequestCard;
