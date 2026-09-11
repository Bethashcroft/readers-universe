import { useState } from "react";
import { placeholderCover } from "../types/covers";

interface BookCoverProps {
  src: string;
  title: string;
  className?: string;
}

function BookCover({ src, title, className }: BookCoverProps) {
  const [failedSrc, setFailedSrc] = useState<string | null>(null);
  const failed = failedSrc === src;

  return (
    <img
      className={className}
      src={failed || !src ? placeholderCover(title) : src}
      alt={`Cover of ${title}`}
      loading="lazy"
      onError={() => setFailedSrc(src)}
    />
  );
}

export default BookCover;
