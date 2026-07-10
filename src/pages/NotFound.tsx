import { Link } from "react-router-dom";
import { usePageTitle } from "../hooks/usePageTitle";
import "./NotFound.css";

function NotFound() {
  usePageTitle("Lost in Space");

  return (
    <div className="not-found">
      <svg className="not-found-art" viewBox="0 0 130 130" aria-hidden="true">
        <circle cx="65" cy="58" r="32" />
        <ellipse
          cx="65"
          cy="64"
          rx="56"
          ry="17"
          transform="rotate(-18 65 64)"
        />
      </svg>
      <p className="not-found-code">404</p>
      <h1>Lost in space</h1>
      <p className="not-found-text">
        This page has drifted off into the void. Let's get you back to
        familiar stars.
      </p>
      <Link to="/" className="btn btn-primary">
        Take me home
      </Link>
    </div>
  );
}

export default NotFound;
