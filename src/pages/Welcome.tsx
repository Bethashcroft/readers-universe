import React, { useState } from "react";
import { Link, Navigate, useLocation, useNavigate } from "react-router-dom";
import { googleRegister } from "../api/auth";
import type { GoogleSignUpDetails } from "../api/auth";
import { useAuth } from "../context/useAuth";
import { usePageTitle } from "../hooks/usePageTitle";
import "../styles/forms.css";
import "./Welcome.css";

type WelcomeState = GoogleSignUpDetails & { idToken: string };

function Welcome() {
  usePageTitle("Welcome");

  const location = useLocation();
  const navigate = useNavigate();
  const { signInWith } = useAuth();
  const details = location.state as WelcomeState | null;

  const [userName, setUserName] = useState(details?.suggestedUserName ?? "");
  const [displayName, setDisplayName] = useState(details?.displayName ?? "");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  if (!details?.idToken) {
    return <Navigate to="/login" replace />;
  }

  const handleSubmit = async (e: React.SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    setBusy(true);
    setError("");

    try {
      signInWith(await googleRegister(details.idToken, userName, displayName));
      navigate("/");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Couldn't create your account");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="auth-page">
      <form className="auth-form" onSubmit={handleSubmit}>
        <h1>Welcome to The Readers Universe</h1>
        <p className="welcome-email">Signing up as {details.email}</p>
        {error && <p className="form-error">{error}</p>}

        <label htmlFor="welcome-username">Choose your username</label>
        <div className="welcome-handle">
          <span aria-hidden="true">@</span>
          <input
            id="welcome-username"
            type="text"
            value={userName}
            onChange={(e) => setUserName(e.target.value.trim())}
            autoComplete="username"
            required
          />
        </div>
        <p className="welcome-hint">
          5 to 20 characters: letters, numbers, dots and underscores. You can
          change it later.
        </p>

        <label htmlFor="welcome-display-name">Display name</label>
        <input
          id="welcome-display-name"
          type="text"
          value={displayName}
          onChange={(e) => setDisplayName(e.target.value)}
          autoComplete="name"
        />

        <button type="submit" disabled={busy || !userName}>
          {busy ? "Creating your account..." : "Continue"}
        </button>
        <p className="form-agree">
          By continuing you agree to our <Link to="/terms">Terms</Link> and{" "}
          <Link to="/privacy">Privacy Policy</Link>.
        </p>
      </form>
    </div>
  );
}

export default Welcome;
