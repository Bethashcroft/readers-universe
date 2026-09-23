import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { deleteAccount } from "../api/auth";
import { useAuth } from "../context/useAuth";
import ConfirmDialog from "./ConfirmDialog";
import "./DeleteAccount.css";

type DeleteAccountProps = {
  userName: string;
};

function DeleteAccount({ userName }: DeleteAccountProps) {
  const { logout } = useAuth();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [typed, setTyped] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  const matches = typed.trim().toLowerCase() === userName.toLowerCase();

  const close = () => {
    setOpen(false);
    setTyped("");
    setError("");
  };

  const handleDelete = async () => {
    setBusy(true);
    setError("");

    try {
      await deleteAccount(typed);
      logout();
      navigate("/");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Couldn't delete your account");
      setBusy(false);
    }
  };

  return (
    <section className="delete-account">
      <h3>Delete your account</h3>
      <p>
        This permanently removes your account and everything in it. It can't be
        undone.
      </p>
      <button
        type="button"
        className="btn btn-danger"
        onClick={() => setOpen(true)}
      >
        Delete my account
      </button>

      {open && (
        <ConfirmDialog
          title="Delete your account?"
          confirmLabel="Delete forever"
          danger
          confirmDisabled={!matches}
          busy={busy}
          error={error}
          onConfirm={handleDelete}
          onCancel={close}
        >
          <p>
            Your profile, shelves, reviews, reading history, goals, feed posts,
            comments, likes, follows and messages will all be deleted straight
            away. Books you added stay in the shared catalogue, because other
            readers may have them too.
          </p>
          <label className="delete-account-confirm">
            Type <strong>{userName}</strong> to confirm
            <input
              type="text"
              value={typed}
              onChange={(e) => setTyped(e.target.value)}
              autoComplete="off"
            />
          </label>
        </ConfirmDialog>
      )}
    </section>
  );
}

export default DeleteAccount;
