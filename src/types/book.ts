export type ShelfType =
  | "currently-reading"
  | "read"
  | "tbr"
  | "want-to-read"
  | "dnf";

export type OfferType =
  | "none"
  | "available-to-borrow"
  | "lent-out"
  | "for-sale";

export type FormatType = "" | "physical" | "ebook" | "audiobook";

export const formatLabels: Record<FormatType, string> = {
  "": "Not set",
  physical: "Physical",
  ebook: "Ebook",
  audiobook: "Audiobook",
};

export const formatOptions: FormatType[] = [
  "",
  "physical",
  "ebook",
  "audiobook",
];

export const formatChoices = formatOptions.map((value) => ({
  value,
  label: formatLabels[value],
}));

export const cannotOfferMessage =
  "Ebooks and audiobooks can't be lent out or sold.";

const cannotOffer: FormatType[] = ["ebook", "audiobook"];

export function canOffer(format: string) {
  return !cannotOffer.includes(format as FormatType);
}

export function formatLabel(format: string) {
  return formatLabels[format as FormatType] ?? format;
}

export const ratingOptions = [
  { value: "", label: "No rating" },
  { value: "1", label: "★☆☆☆☆" },
  { value: "2", label: "★★☆☆☆" },
  { value: "3", label: "★★★☆☆" },
  { value: "4", label: "★★★★☆" },
  { value: "5", label: "★★★★★" },
];

export const shelfLabels: Record<ShelfType, string> = {
  "currently-reading": "Currently Reading",
  read: "Read",
  tbr: "To Be Read",
  "want-to-read": "Want to Read",
  dnf: "Did Not Finish",
};

export const offerLabels: Record<OfferType, string> = {
  none: "Not offered",
  "available-to-borrow": "Available to Borrow",
  "for-sale": "For Sale",
  "lent-out": "Lent Out",
};

export const selectableOffers: OfferType[] = [
  "none",
  "available-to-borrow",
  "for-sale",
];

export const shelfChoices = Object.entries(shelfLabels).map(
  ([value, label]) => ({ value, label }),
);

export const offerChoices = selectableOffers.map((value) => ({
  value,
  label: offerLabels[value],
}));

const offerBadges: Record<OfferType, string> = {
  none: "",
  "available-to-borrow": "badge badge-green",
  "for-sale": "badge badge-indigo",
  "lent-out": "badge badge-amber",
};

export function offerBadgeClass(offer: string) {
  return offerBadges[offer as OfferType] ?? "badge badge-violet";
}

export function offerLabel(offer: string) {
  return offerLabels[offer as OfferType] ?? offer;
}

const statusBadges: Record<string, string> = {
  pending: "badge badge-amber",
  accepted: "badge badge-green",
  declined: "badge badge-red",
  returned: "badge badge-violet",
};

export function statusBadgeClass(status: string) {
  return statusBadges[status] ?? "badge badge-violet";
}
