import { Link } from "react-router-dom";
import { usePageTitle } from "../hooks/usePageTitle";
import { contactEmail, legalLastUpdated } from "./legal";
import "./Legal.css";

function Terms() {
  usePageTitle("Terms of Use");

  return (
    <article className="legal">
      <h1>Terms of Use</h1>
      <p className="legal-updated">Last updated {legalLastUpdated}</p>

      <p>
        These terms are the rules for using The Readers Universe. By creating
        an account, you agree to them. They sit alongside our{" "}
        <Link to="/privacy">Privacy Policy</Link>, which explains what happens
        to your information.
      </p>

      <h2>Your account</h2>
      <ul>
        <li>You need to be 13 or over.</li>
        <li>
          Keep your sign-in details to yourself. You're responsible for what
          happens on your account.
        </li>
        <li>One person per account, and don't pretend to be someone else.</li>
      </ul>

      <h2>What you post</h2>
      <p>
        Your reviews, comments, messages and profile belong to you. By posting
        them, you let us show them in the app to the people your settings
        allow. Reviews are public to every reader.
      </p>
      <p>Please don't post anything that is:</p>
      <ul>
        <li>abusive, hateful, threatening or harassing</li>
        <li>spam, or someone else's work passed off as your own</li>
        <li>illegal, or that shares someone's private details</li>
      </ul>
      <p>
        Mark reviews that give away the plot as containing spoilers. It's the
        kind thing to do.
      </p>

      <h2>Lending and borrowing</h2>
      <p>
        Lending is between you and the readers you trust. The Readers Universe
        helps you find each other and keep track, but we're not part of the
        loan.
      </p>
      <ul>
        <li>
          Only lend to people in your Trusted Book Club, and only lend books
          you're happy to hand over.
        </li>
        <li>
          Look after books you borrow, and give them back when you said you
          would.
        </li>
        <li>
          We aren't responsible for books that are lost, damaged or not
          returned. If something goes wrong, please sort it out between you.
        </li>
        <li>Ebooks and audiobooks can't be lent or sold through the app.</li>
      </ul>

      <h2>Selling</h2>
      <p>
        If you mark a book for sale, any sale happens between you and the
        buyer, usually on another site like Vinted, under that site's rules.
        We don't handle payments or deliveries.
      </p>

      <h2>Book details</h2>
      <p>
        Book information and covers come from Open Library and from readers.
        We do our best, but they won't always be right.
      </p>

      <h2>If rules are broken</h2>
      <p>
        We can remove anything that breaks these terms, and suspend or close
        accounts that break them, particularly more than once.
      </p>

      <h2>Leaving</h2>
      <p>
        You can stop using the app whenever you like, and delete your account
        at any time from <strong>Edit Profile</strong>. That removes your
        account and everything in it straight away.
      </p>

      <h2>The small print</h2>
      <ul>
        <li>
          The Readers Universe is a small independent project. We'll try to
          keep it running smoothly, but we can't promise it will always be
          available or free of mistakes, and we may change or stop features.
        </li>
        <li>
          We're not liable for losses that come from using the app, as far as
          the law allows. Nothing here limits anything that can't legally be
          limited.
        </li>
        <li>
          If we change these terms, we'll update the date at the top. If a
          change is significant, we'll let you know before it takes effect.
        </li>
        <li>These terms are governed by the law of England and Wales.</li>
      </ul>

      <p className="legal-see-also">
        Questions? Email <a href={`mailto:${contactEmail}`}>{contactEmail}</a>.
      </p>
    </article>
  );
}

export default Terms;
