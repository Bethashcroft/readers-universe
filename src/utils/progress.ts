export function percentThrough(page: number | null, pageCount: number | null) {
  if (!page || !pageCount) return null;
  return Math.min(100, Math.round((page / pageCount) * 100));
}
