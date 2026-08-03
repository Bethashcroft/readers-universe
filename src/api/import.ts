import { BASE_URL, getAuthHeaders } from "./client";

export interface ImportSummary {
  service: string;
  committed: boolean;
  rowsFound: number;
  added: number;
  alreadyOnShelves: number;
  reviewsAdded: number;
  newToCatalogue: number;
  skippedRows: number;
  byShelf: Record<string, number>;
  sample: string[];
}

export async function importLibrary(
  file: File,
  preview: boolean,
): Promise<ImportSummary> {
  const body = new FormData();
  body.append("file", file);

  const response = await fetch(
    `${BASE_URL}/library/import?preview=${preview}`,
    {
      method: "POST",
      headers: getAuthHeaders(),
      body,
    },
  );

  if (!response.ok) {
    const data = await response.json().catch(() => null);
    throw new Error(data?.message ?? "We could not read that file.");
  }

  return response.json();
}
