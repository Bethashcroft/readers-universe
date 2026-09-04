import { useState } from "react";
import { placeholderCover } from "../types/covers";

interface BookCoverProps {
  src: string;
  title: string;
  className?: string;
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
