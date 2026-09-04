function escapeDataString(value: string): string {
  return encodeURIComponent(value).replace(
    /[!'()*]/g,
    (c) => `%${c.charCodeAt(0).toString(16).toUpperCase()}`,
  );
}

export function placeholderCover(title: string): string {
  return `https://placehold.co/200x300/1a1430/a9a3cc?text=${escapeDataString(title)}`;
}
