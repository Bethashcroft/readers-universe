import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import {
  getComments,
  addComment,
  editComment,
  deleteComment,
} from "../api/feed";
import type { CommentResponse } from "../api/feed";
import { timeAgo } from "../utils/dates";
import Avatar from "./Avatar";
import "./CommentThread.css";

const maxLength = 500;

type CommentThreadProps = {
  activityId: number;
  onCountChanged: (id: number, count: number) => void;
};

function CommentThread({ activityId, onCountChanged }: CommentThreadProps) {
  const [comments, setComments] = useState<CommentResponse[] | null>(null);
  const [text, setText] = useState("");
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editText, setEditText] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;

    const load = async () => {
      try {
        const found = await getComments(activityId);
        if (active) setComments(found);
      } catch (err) {
        if (active) {
          setError(err instanceof Error ? err.message : "Failed to load");
        }
      }
    };

    load();

    return () => {
      active = false;
    };
  }, [activityId]);

  const settle = (found: CommentResponse[]) => {
    setComments(found);
    onCountChanged(activityId, found.length);
  };

  const startEditing = (comment: CommentResponse) => {
    setEditingId(comment.id);
    setEditText(comment.text);
    setError("");
  };

  const handleEdit = async () => {
    const written = editText.trim();
    if (!written || editingId === null) return;

    setBusy(true);
    setError("");

    try {
      settle(await editComment(editingId, written));
      setEditingId(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save");
    } finally {
      setBusy(false);
    }
  };

  const handleAdd = async () => {
    const written = text.trim();
    if (!written) return;

    setBusy(true);
    setError("");

    try {
      settle(await addComment(activityId, written));
      setText("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to post");
    } finally {
      setBusy(false);
    }
  };

  const handleDelete = async (commentId: number) => {
    setBusy(true);
    setError("");

    try {
      settle(await deleteComment(commentId));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="comment-thread">
      {comments === null && !error && (
        <p className="comment-note">Loading comments...</p>
      )}

      {comments?.map((comment) => (
        <div key={comment.id} className="comment">
          <Link to={`/profile/${comment.userName}`}>
            <Avatar
              url={comment.avatarUrl}
              name={comment.displayName}
              size={28}
            />
          </Link>
          <div className="comment-body">
            {editingId === comment.id ? (
              <div className="comment-form">
                <input
                  type="text"
                  aria-label="Edit your comment"
                  maxLength={maxLength}
                  value={editText}
                  onChange={(e) => setEditText(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") {
                      e.preventDefault();
                      handleEdit();
                    }
                  }}
                />
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={handleEdit}
                  disabled={busy || editText.trim().length === 0}
                >
                  Save
                </button>
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setEditingId(null)}
                  disabled={busy}
                >
                  Cancel
                </button>
              </div>
            ) : (
              <>
                <p>
                  <Link
                    to={`/profile/${comment.userName}`}
                    className="comment-name"
                  >
                    {comment.displayName}
                  </Link>{" "}
                  {comment.text}
                </p>
                <div className="comment-meta">
                  <span>
                    {timeAgo(comment.date)}
                    {comment.editedDate && " (edited)"}
                  </span>
                  {comment.canEdit && (
                    <button
                      type="button"
                      className="comment-action"
                      onClick={() => startEditing(comment)}
                      disabled={busy}
                    >
                      Edit
                    </button>
                  )}
                  {comment.canDelete && (
                    <button
                      type="button"
                      className="comment-action comment-delete"
                      onClick={() => handleDelete(comment.id)}
                      disabled={busy}
                    >
                      Delete
                    </button>
                  )}
                </div>
              </>
            )}
          </div>
        </div>
      ))}

      {error && <p className="form-error">{error}</p>}

      <div className="comment-form">
        <input
          type="text"
          aria-label="Write a comment"
          placeholder="Write a comment"
          maxLength={maxLength}
          value={text}
          onChange={(e) => setText(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              e.preventDefault();
              handleAdd();
            }
          }}
        />
        <button
          type="button"
          className="btn btn-secondary"
          onClick={handleAdd}
          disabled={busy || text.trim().length === 0}
        >
          Post
        </button>
      </div>
    </div>
  );
}

export default CommentThread;
