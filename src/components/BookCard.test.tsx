import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import BookCard from "./BookCard";
import type { BookResponse } from "../api/books";

const book: BookResponse = {
  id: 1,
  title: "The Hobbit",
  author: "J.R.R. Tolkien",
  coverUrl: "x",
  shelf: "read",
  rating: 4,
  userId: "u1",
};

function renderCard(b: BookResponse = book) {
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

    expect(screen.getByRole("link")).toHaveAttribute("href", "/book/1");
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
