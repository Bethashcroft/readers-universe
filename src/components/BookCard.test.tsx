import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import BookCard from "./BookCard";
import type { LibraryEntryResponse } from "../api/books";

const book: LibraryEntryResponse = {
  id: 1,
  bookId: 77,
  alreadyOnShelves: false,
  title: "The Hobbit",
  author: "J.R.R. Tolkien",
  coverUrl: "x",
  isbn: "",
  shelf: "read",
  offer: "none",
  rating: 4,
  userId: "u1",
  sellerVintedUrl: "",
  ownerName: "Owner",
  ownerUserName: "owner",
};

function renderCard(b: LibraryEntryResponse = book) {
  return render(
    <MemoryRouter>
      <BookCard book={b} />
    </MemoryRouter>,
  );
}

describe("BookCard", () => {
  it("renders the title and author", () => {
    renderCard();

    expect(screen.getByText("The Hobbit")).toBeInTheDocument();
    expect(screen.getByText("J.R.R. Tolkien")).toBeInTheDocument();
  });

  it("links to the book's detail page", () => {
    renderCard();

    expect(screen.getByRole("link")).toHaveAttribute("href", "/book/77");
  });

  it("shows the star rating", () => {
    renderCard();

    expect(screen.getByText("★★★★☆")).toBeInTheDocument();
  });

  it("shows no rating stars when the book is unrated", () => {
    renderCard({ ...book, rating: null });

    expect(screen.queryByText("★★★★☆")).not.toBeInTheDocument();
  });
});
