type GoogleButtonOptions = {
  theme: "outline" | "filled_blue" | "filled_black";
  size: "large" | "medium" | "small";
  shape: "pill" | "rectangular";
  text: "continue_with" | "signin_with" | "signup_with";
  width: number;
  locale: string;
};

type GoogleIdentityServices = {
  accounts: {
    id: {
      initialize: (options: {
        client_id: string;
        callback: (response: { credential: string }) => void;
      }) => void;
      renderButton: (parent: HTMLElement, options: GoogleButtonOptions) => void;
    };
  };
};

declare global {
  interface Window {
    google?: GoogleIdentityServices;
  }
}

let loading: Promise<GoogleIdentityServices> | null = null;

export function googleClientId() {
  return import.meta.env.VITE_GOOGLE_CLIENT_ID ?? "";
}

export function loadGoogle(): Promise<GoogleIdentityServices> {
  loading ??= new Promise((resolve, reject) => {
    const script = document.createElement("script");
    script.src = "https://accounts.google.com/gsi/client";
    script.async = true;
    script.onload = () =>
      window.google ? resolve(window.google) : reject(new Error("Google didn't load"));
    script.onerror = () => {
      loading = null;
      reject(new Error("Couldn't reach Google"));
    };
    document.head.appendChild(script);
  });

  return loading;
}
