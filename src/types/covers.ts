const maxTitleInPlaceholder = 120;

function escapeDataString(value: string): string {
  return encodeURIComponent(value).replace(
    /[!'()*]/g,
    (c) => `%${c.charCodeAt(0).toString(16).toUpperCase()}`,
  );
}

export function placeholderCover(title: string): string {
  let cut =
    title.length > maxTitleInPlaceholder ? maxTitleInPlaceholder : title.length;

  if (cut < title.length && /[\uD800-\uDBFF]/.test(title[cut - 1])) {
    cut--;
  }

  return `https://placehold.co/200x300/1a1430/a9a3cc?text=${escapeDataString(title.slice(0, cut))}`;
}
