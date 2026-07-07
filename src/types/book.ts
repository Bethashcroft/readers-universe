export type ShelfType = "currently-reading" | "read" | "tbr" | "dnf";

export type OfferType =
  | "none"
  | "available-to-borrow"
  | "lent-out"
  | "for-sale";

export const shelfLabels: Record<ShelfType, string> = {
  "currently-reading": "Currently Reading",
  read: "Read",
  tbr: "To Be Read",
  dnf: "Did Not Finish",
};

export const offerLabels: Record<OfferType, string> = {
  none: "Not offered",
  "available-to-borrow": "Available to Borrow",
  "for-sale": "For Sale",
  "lent-out": "Lent Out",
};
