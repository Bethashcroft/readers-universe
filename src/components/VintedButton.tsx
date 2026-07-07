interface VintedButtonProps {
  href: string;
  label: string;
  className?: string;
}

function VintedButton({ href, label, className }: VintedButtonProps) {
  return (
    <a
      className={className ? `btn-vinted ${className}` : "btn-vinted"}
      href={href}
      target="_blank"
      rel="noopener noreferrer"
    >
      <img
        className="vinted-logo"
        src="/vinted-logo.png"
        alt=""
        aria-hidden="true"
      />
      {label}
    </a>
  );
}

export default VintedButton;
