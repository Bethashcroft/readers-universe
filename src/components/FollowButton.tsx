import { useState } from "react";
import { followReader, unfollowReader } from "../api/follows";
import type { FollowState } from "../api/follows";
import "./FollowButton.css";

type FollowButtonProps = {
  username: string;
  state: FollowState;
  onChange: (state: FollowState) => void;
};

const labels: Record<FollowState, string> = {
  none: "Follow",
  requested: "Requested",
  following: "Following",
  self: "",
};

function FollowButton({ username, state, onChange }: FollowButtonProps) {
  const [busy, setBusy] = useState(false);

  if (state === "self") {
    return null;
  }

  const handleClick = async () => {
    setBusy(true);

    try {
      const result =
        state === "none"
          ? await followReader(username)
          : await unfollowReader(username);

      onChange(result.state);
    } catch (err) {
      console.error("Follow action failed:", err);
    } finally {
      setBusy(false);
    }
  };

  return (
    <button
      type="button"
      className={`btn follow-btn ${
        state === "none" ? "btn-primary" : "btn-secondary"
      }`}
      onClick={handleClick}
      disabled={busy}
    >
      {labels[state]}
    </button>
  );
}

export default FollowButton;
