import { useEffect, useId, useRef, useState } from "react";
import "./SelectMenu.css";

export interface SelectOption {
  value: string;
  label: string;
}

interface SelectMenuProps {
  label: string;
  value: string;
  options: SelectOption[];
  onChange: (value: string) => void;
  block?: boolean;
}

function SelectMenu({
  label,
  value,
  options,
  onChange,
  block = false,
}: SelectMenuProps) {
  const [open, setOpen] = useState(false);
  const labelId = useId();
  const triggerId = useId();
  const containerRef = useRef<HTMLDivElement>(null);

  const current = options.find((o) => o.value === value) ?? options[0];

  useEffect(() => {
    if (!open) {
      return;
    }

    const handlePointerDown = (event: MouseEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    };

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setOpen(false);
      }
    };

    document.addEventListener("mousedown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("mousedown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [open]);

  return (
    <div
      className={`select-menu${block ? " select-menu-block" : ""}`}
      ref={containerRef}
    >
      <span className="select-menu-label" id={labelId}>
        {label}
      </span>
      <button
        type="button"
        id={triggerId}
        className="select-menu-trigger btn-pill"
        aria-haspopup="listbox"
        aria-labelledby={`${labelId} ${triggerId}`}
        aria-expanded={open}
        onClick={() => setOpen((wasOpen) => !wasOpen)}
      >
        {current.label}
        <svg
          className="select-menu-chevron"
          viewBox="0 0 12 8"
          aria-hidden="true"
        >
          <path d="M1 1.5 L6 6.5 L11 1.5" />
        </svg>
      </button>
      {open && (
        <div className="select-menu-panel" role="listbox" aria-labelledby={labelId}>
          {options.map((option) => (
            <button
              key={option.value}
              type="button"
              role="option"
              aria-selected={option.value === value}
              className={`select-menu-option ${option.value === value ? "selected" : ""}`}
              onClick={() => {
                onChange(option.value);
                setOpen(false);
              }}
            >
              {option.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

export default SelectMenu;
