import type { ReactNode } from "react";
import Modal from "./Modal";
import "./ConfirmDialog.css";

type ConfirmDialogProps = {
  title: string;
  confirmLabel: string;
  busy?: boolean;
  error?: string;
  onConfirm: () => void;
  onCancel: () => void;
  children: ReactNode;
};

function ConfirmDialog({
  title,
  confirmLabel,
  busy = false,
  error = "",
  onConfirm,
  onCancel,
  children,
}: ConfirmDialogProps) {
  return (
    <Modal title={title} onClose={onCancel} locked={busy}>
      <div className="confirm-body">{children}</div>
      {error && <p className="confirm-error">{error}</p>}
      <div className="confirm-actions">
        <button
          type="button"
          className="btn btn-secondary"
          onClick={onCancel}
          disabled={busy}
        >
          Cancel
        </button>
        <button
          type="button"
          className="btn btn-primary"
          onClick={onConfirm}
          disabled={busy}
        >
          {busy ? "Working..." : confirmLabel}
        </button>
      </div>
    </Modal>
  );
}

export default ConfirmDialog;
