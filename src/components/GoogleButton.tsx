import { useEffect, useEffectEvent, useRef } from "react";
import { googleClientId, loadGoogle } from "../utils/google";

type GoogleButtonProps = {
  onCredential: (idToken: string) => void;
  onUnavailable: () => void;
};

function GoogleButton({ onCredential, onUnavailable }: GoogleButtonProps) {
  const container = useRef<HTMLDivElement>(null);
  const handleCredential = useEffectEvent(onCredential);
  const handleUnavailable = useEffectEvent(onUnavailable);

  useEffect(() => {
    let active = true;

    loadGoogle()
      .then((google) => {
        if (!active || !container.current) return;

        google.accounts.id.initialize({
          client_id: googleClientId(),
          callback: (response) => handleCredential(response.credential),
        });

        google.accounts.id.renderButton(container.current, {
          theme: "filled_black",
          size: "large",
          shape: "pill",
          text: "continue_with",
          width: 320,
          locale: "en-GB",
        });
      })
      .catch((err) => {
        console.error("Google sign-in unavailable:", err);
        if (active) handleUnavailable();
      });

    return () => {
      active = false;
    };
  }, []);

  return <div ref={container} className="google-button" />;
}

export default GoogleButton;
