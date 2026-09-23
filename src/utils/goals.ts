export function booksAheadOfSchedule(
  target: number,
  booksRead: number,
  now = new Date(),
) {
  const start = new Date(now.getFullYear(), 0, 1).getTime();
  const end = new Date(now.getFullYear() + 1, 0, 1).getTime();
  const throughTheYear = (now.getTime() - start) / (end - start);

  return booksRead - Math.floor(target * throughTheYear);
}

export function scheduleMessage(
  target: number,
  booksRead: number,
  now = new Date(),
) {
  if (booksRead >= target) {
    return "You've hit your goal.";
  }

  const ahead = booksAheadOfSchedule(target, booksRead, now);
  const books = (n: number) => `${n} ${n === 1 ? "book" : "books"}`;

  if (ahead > 0) return `${books(ahead)} ahead of schedule`;
  if (ahead < 0) return `${books(-ahead)} behind schedule`;
  return "Right on schedule";
}
