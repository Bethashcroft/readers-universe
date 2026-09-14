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
