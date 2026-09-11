import { useEffect, type ReactNode } from "react";
import "./Modal.css";

type ModalProps = {
  title: string;
  onClose: () => void;
  locked?: boolean;
  className?: string;
  children: ReactNode;
};

function Modal({
  title,
  onClose,
  locked = false,
  className = "",
  children,
}: ModalProps) {
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !locked) {
        onClose();
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [locked, onClose]);

  return (
    <div className="modal-backdrop" onClick={locked ? undefined : onClose}>
      <div
        className={`modal ${className}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-title"
        onClick={(e) => e.stopPropagation()}
      >
        <h2 id="modal-title">{title}</h2>
        {children}
      </div>
    </div>
  );
}

export default Modal;
