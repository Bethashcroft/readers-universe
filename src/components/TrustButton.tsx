import { useState } from "react";
import { trustReader, untrustReader } from "../api/trust";
import ConfirmDialog from "./ConfirmDialog";
import "./TrustButton.css";

type TrustButtonProps = {
  username: string;
  displayName: string;
  trusted: boolean;
  onChange: (trusted: boolean) => void;
};

function TrustButton({
  username,
  displayName,
  trusted,
  onChange,
}: TrustButtonProps) {
  const [busy, setBusy] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const [error, setError] = useState("");

  const open = () => {
    setError("");
    setConfirming(true);
  };

  const handleConfirm = async () => {
    setBusy(true);
    setError("");

    try {
      if (trusted) {
        await untrustReader(username);
      } else {
        await trustReader(username);
      }

      onChange(!trusted);
      setConfirming(false);
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Something went wrong. Try again.",
      );
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      <button
        type="button"
        className={`btn trust-btn ${trusted ? "btn-danger" : "btn-primary"}`}
        onClick={open}
        disabled={busy}
      >
        {trusted ? "Remove from Trusted" : "Trust"}
      </button>

      {confirming &&
        (trusted ? (
          <ConfirmDialog
            title="Remove from your Trusted Book Club?"
            confirmLabel="Remove"
            busy={busy}
            error={error}
            onConfirm={handleConfirm}
            onCancel={() => setConfirming(false)}
          >
            <p>
              <strong>{displayName}</strong> will no longer be able to request
              to borrow your books.
            </p>
            <p>They won't be notified.</p>
          </ConfirmDialog>
        ) : (
          <ConfirmDialog
            title="Add to your Trusted Book Club?"
            confirmLabel="Add to club"
            busy={busy}
            error={error}
            onConfirm={handleConfirm}
            onCancel={() => setConfirming(false)}
          >
            <p>
              <strong>{displayName}</strong> will be able to request to borrow
              your books.
            </p>
            <p>They will be notified that you added them.</p>
          </ConfirmDialog>
        ))}
    </>
  );
}

export default TrustButton;
