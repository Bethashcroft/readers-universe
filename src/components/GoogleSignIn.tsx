import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { googleSignIn } from "../api/auth";
import { useAuth } from "../context/useAuth";
import { googleClientId } from "../utils/google";
import GoogleButton from "./GoogleButton";
import "./GoogleSignIn.css";

function GoogleSignIn() {
  const { signInWith } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState("");
  const [unavailable, setUnavailable] = useState(false);

  if (!googleClientId() || unavailable) {
    return null;
  }

  const handleCredential = async (idToken: string) => {
    setError("");

    try {
      const result = await googleSignIn(idToken);

      if (result.auth) {
        signInWith(result.auth);
        navigate("/");
      } else if (result.signUp) {
        navigate("/welcome", { state: { idToken, ...result.signUp } });
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Google sign-in failed");
    }
  };

  return (
    <div className="google-sign-in">
      <GoogleButton
        onCredential={handleCredential}
        onUnavailable={() => setUnavailable(true)}
      />
      {error && <p className="form-error">{error}</p>}
      <div className="auth-divider">
        <span>or</span>
      </div>
    </div>
  );
}

export default GoogleSignIn;
