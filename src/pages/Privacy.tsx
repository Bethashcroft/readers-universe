import { Link } from "react-router-dom";
import { usePageTitle } from "../hooks/usePageTitle";
import { contactEmail, legalLastUpdated } from "./legal";
import "./Legal.css";

function Privacy() {
  usePageTitle("Privacy Policy");

  return (
    <article className="legal">
      <h1>Privacy Policy</h1>
      <p className="legal-updated">Last updated {legalLastUpdated}</p>

      <p>
        The Readers Universe is a small, independent app for tracking your
        reading and lending books to people you trust. It's run from the
        United Kingdom. This page explains what we store, who can see it, and
        how to get it removed.
      </p>

      <h2>What we store</h2>
      <ul>
        <li>
          <strong>Your account:</strong> your username, display name, email
          address, and your password in a scrambled form we can't read. If you
          sign in with Google, we store your Google account ID, name and email
          address instead of a password.
        </li>
        <li>
          <strong>Your profile:</strong> anything you add to it, such as a bio,
          a Vinted link and a profile picture, and whether your account is
          private.
        </li>
        <li>
          <strong>Your reading:</strong> the books on your shelves, their
          format, your progress, the dates you finished them, your reading
          goals, your ratings and your reviews.
        </li>
        <li>
          <strong>Your activity:</strong> the updates we post to your feed, your
          likes and comments, who you follow, who is in your Trusted Book Club,
          your borrow requests and the messages you send about them.
        </li>
        <li>
          <strong>Goodreads imports:</strong> if you import a Goodreads export,
          we keep the books, dates, ratings and reviews from it. We don't keep
          the file itself.
        </li>
      </ul>
      <p>
        We don't use advertising, we don't track you across other sites, and we
        never sell your information.
      </p>

      <h2>Who can see it</h2>
      <ul>
        <li>
          <strong>Public account:</strong> other readers can see your profile,
          shelves, reading activity and this year's reading goal.
        </li>
        <li>
          <strong>Private account:</strong> only readers you've approved as
          followers see your shelves, activity and goal. Other readers can
          still find you by username and see your name, profile picture and
          bio.
        </li>
        <li>
          <strong>Reviews are always public</strong>, whichever you choose, and
          count towards a book's average rating.
        </li>
        <li>
          <strong>Profile pictures</strong> can be viewed by anyone who has the
          link to them.
        </li>
        <li>
          <strong>Your email address</strong> is never shown to other readers.
        </li>
        <li>
          Borrow request messages are only seen by you and the other reader.
        </li>
      </ul>

      <h2>Why we use it</h2>
      <p>
        To run the app for you: showing your shelves, your feed and your stats,
        and letting you follow, lend and borrow. We also use it to keep the app
        safe, for example to stop abuse. Under UK data protection law, that
        means we rely on providing the service you signed up for, and on our
        legitimate interest in keeping it secure.
      </p>

      <h2>Services we rely on</h2>
      <ul>
        <li>
          <strong>Netlify, Render and Neon</strong> host the website, the
          server and the database. Your information may be stored or processed
          outside the UK by these providers.
        </li>
        <li>
          <strong>Google</strong> handles "Continue with Google" if you use it,
          and serves the fonts the site uses.
        </li>
        <li>
          <strong>Open Library</strong> provides book details and covers. When
          you look up a book we send them the title, author or ISBN, never who
          you are, though your browser contacts them to load covers.
        </li>
      </ul>

      <h2>Your browser</h2>
      <p>
        We keep you signed in by saving a sign-in token in your browser's
        storage. It's removed when you log out. We don't use cookies for
        tracking or advertising.
      </p>

      <h2>How long we keep it</h2>
      <p>
        We keep your information for as long as you have an account. You can
        delete your account at any time from <strong>Edit Profile</strong>,
        which removes it and everything tied to it straight away. Books you
        added stay in the shared catalogue, because other readers may have them
        too. Copies can remain in our database provider's backups for a short
        time before they're overwritten.
      </p>

      <h2>Your rights</h2>
      <p>
        You have the right to see, correct and delete your information, and to
        object to how it's used. Most of this you can do yourself:
      </p>
      <ul>
        <li>change your profile details in Edit Profile</li>
        <li>edit or delete your reviews, and remove books from your shelves</li>
        <li>make your account private</li>
        <li>delete your whole account in Edit Profile</li>
      </ul>
      <p>
        For anything else, including a copy of your information, email{" "}
        <a href={`mailto:${contactEmail}`}>{contactEmail}</a>. The Readers
        Universe is run by one person, so please bear with us and we'll get back
        to you as soon as we can.
      </p>
      <p>
        If you're unhappy with how we've handled your information, you can
        complain to the Information Commissioner's Office at{" "}
        <a href="https://ico.org.uk">ico.org.uk</a>.
      </p>

      <h2>Age</h2>
      <p>You need to be 13 or over to use The Readers Universe.</p>

      <h2>Changes</h2>
      <p>
        If we change this policy, we'll update the date at the top. If a change
        is significant, we'll let you know before it takes effect.
      </p>

      <p className="legal-see-also">
        See also our <Link to="/terms">Terms of Use</Link>.
      </p>
    </article>
  );
}

export default Privacy;
