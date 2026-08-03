import { useState } from "react";

interface BookCoverProps {
  src: string;
  title: string;
  className?: string;
}

function placeholderCover(title: string): string {
  return `https://placehold.co/200x300/1a1430/a9a3cc?text=${encodeURIComponent(title)}`;
}

function BookCover({ src, title, className }: BookCoverProps) {
  const [failed, setFailed] = useState(false);

  return (
    <img
      className={className}
      src={failed || !src ? placeholderCover(title) : src}
      alt={`Cover of ${title}`}
      loading="lazy"
      onError={() => setFailed(true)}
    />
  );
}

export default BookCover;
