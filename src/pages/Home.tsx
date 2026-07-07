import { Link } from "react-router-dom";
import { useAuth } from "../context/useAuth";
import Dashboard from "./Dashboard";
import "./Home.css";

const steps = [
  {
    title: "Build your shelves",
    text: "Add the books you own, are reading, or want to read. Organise them across Currently Reading, TBR, Read and more.",
  },
  {
    title: "Discover nearby",
    text: "Browse books that readers around you have put up to borrow or buy, and search by title, author or genre.",
  },
  {
    title: "Borrow & connect",
    text: "Request a book, arrange the swap through in-app messaging, then share your reviews with the community.",
  },
];

const genres = [
  "Fantasy",
  "Romance",
  "Sci-Fi",
  "Mystery",
  "Non-Fiction",
  "Horror",
  "Young Adult",
  "Poetry",
  "Thriller",
  "Classics",
];

function Home() {
  const { user } = useAuth();

  if (user) {
    return <Dashboard />;
  }

  return (
    <div className="home">
      <section className="hero">
        <svg
          className="decor decor-planet"
          viewBox="0 0 130 130"
          strokeWidth="1.5"
        >
          <circle cx="65" cy="58" r="32" />
          <ellipse
            cx="65"
            cy="64"
            rx="56"
            ry="17"
            transform="rotate(-18 65 64)"
          />
        </svg>
        <svg className="decor decor-book" viewBox="0 0 130 100" strokeWidth="1.5">
          <path d="M65 22 C 48 11, 22 11, 9 19 L 9 78 C 22 70, 48 70, 65 80" />
          <path d="M65 22 C 82 11, 108 11, 121 19 L 121 78 C 108 70, 82 70, 65 80" />
          <line x1="65" y1="22" x2="65" y2="80" />
        </svg>
        <svg
          className="decor decor-star decor-star-1"
          viewBox="0 0 40 40"
          strokeWidth="1.5"
        >
          <path d="M20 3 L23.5 16.5 L37 20 L23.5 23.5 L20 37 L16.5 23.5 L3 20 L16.5 16.5 Z" />
        </svg>
        <svg
          className="decor decor-star decor-star-2"
          viewBox="0 0 40 40"
          strokeWidth="1.5"
        >
          <path d="M20 3 L23.5 16.5 L37 20 L23.5 23.5 L20 37 L16.5 23.5 L3 20 L16.5 16.5 Z" />
        </svg>
        <h1>
          Your reading and social life -{" "}
          <span className="hero-accent">connected</span>
        </h1>
        <p>
          Track your books, share reviews, borrow from readers nearby, and
          explore genre universes with a community that loves reading as much as
          you do.
        </p>
        <div className="hero-actions">
          <Link to="/register" className="btn btn-primary">
            Get Started
          </Link>
          <Link to="/login" className="btn btn-secondary">
            Login
          </Link>
        </div>
      </section>

      <section className="how-it-works">
        <h2 className="section-title">How it works</h2>
        <div className="steps">
          {steps.map((step, index) => (
            <div key={step.title} className="step">
              <span className="step-number">{index + 1}</span>
              <h3>{step.title}</h3>
              <p>{step.text}</p>
            </div>
          ))}
        </div>
      </section>

      <section className="universe">
        <h2 className="section-title">Explore genre universes</h2>
        <p className="section-subtitle">
          Find your people in the worlds you love to read.
        </p>
        <div className="genre-pills">
          {genres.map((genre) => (
            <span key={genre} className="genre-pill">
              {genre}
            </span>
          ))}
        </div>
        <div className="universe-cta">
          <h3>Ready to meet your next favourite book?</h3>
          <p>Join readers near you sharing the books they love.</p>
          <Link to="/register" className="btn">
            Get Started
          </Link>
        </div>
      </section>
    </div>
  );
}

export default Home;
