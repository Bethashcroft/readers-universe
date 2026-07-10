import { useEffect } from "react";

export function usePageTitle(title: string) {
  useEffect(() => {
    document.title = `${title} — The Readers Universe`;
    return () => {
      document.title = "The Readers Universe";
    };
  }, [title]);
}
