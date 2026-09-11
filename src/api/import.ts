import { getAuthHeaders, request } from "./client";

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

export function importLibrary(
  file: File,
  preview: boolean,
): Promise<ImportSummary> {
  const body = new FormData();
  body.append("file", file);

  return request(
    `/library/import?preview=${preview}`,
    "We could not read that file.",
    {
      method: "POST",
      headers: getAuthHeaders(),
      body,
    },
  );
}
