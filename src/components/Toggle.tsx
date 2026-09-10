import "./Toggle.css";

type ToggleProps = {
  id: string;
  label: string;
  hint?: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
};

function Toggle({ id, label, hint, checked, onChange }: ToggleProps) {
  return (
    <div className="toggle-row">
      <div className="toggle-text">
        <span className="toggle-label" id={`${id}-label`}>
          {label}
        </span>
        {hint && <p className="toggle-hint">{hint}</p>}
      </div>
      <button
        type="button"
        role="switch"
        aria-checked={checked}
        aria-labelledby={`${id}-label`}
        className={`toggle ${checked ? "toggle-on" : ""}`}
        onClick={() => onChange(!checked)}
      >
        <span className="toggle-knob" />
      </button>
    </div>
  );
}

export default Toggle;
