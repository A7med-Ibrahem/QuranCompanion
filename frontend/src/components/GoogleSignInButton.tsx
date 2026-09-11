import { useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";

// Minimal typing for the bits of the Google Identity Services API we use -
// the full library ships its own types, but pulling in @types/google.accounts
// just for this one button isn't worth the extra dependency.
declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (config: {
            client_id: string;
            callback: (response: { credential: string }) => void;
          }) => void;
          renderButton: (parent: HTMLElement, options: Record<string, string>) => void;
        };
      };
    };
  }
}

export function GoogleSignInButton() {
  const { loginWithGoogle } = useAuth();
  const navigate = useNavigate();
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;
    if (!clientId || !containerRef.current) return;

    let cancelled = false;

    function render() {
      if (cancelled || !window.google || !containerRef.current) return;

      window.google.accounts.id.initialize({
        client_id: clientId,
        callback: async (response) => {
          try {
            await loginWithGoogle(response.credential);
            navigate("/", { replace: true });
          } catch {
            // A failed Google sign-in silently falls back to the normal form below -
            // the person can just try again or use email/password instead.
          }
        }
      });

      window.google.accounts.id.renderButton(containerRef.current, {
        type: "standard",
        theme: "outline",
        size: "large",
        text: "continue_with",
        shape: "pill",
        width: "320"
      });
    }

    if (window.google) {
      render();
    } else {
      // The GSI <script> in index.html loads async - poll briefly until it's ready.
      const interval = setInterval(() => {
        if (window.google) {
          clearInterval(interval);
          render();
        }
      }, 100);
      return () => {
        cancelled = true;
        clearInterval(interval);
      };
    }
  }, [loginWithGoogle, navigate]);

  if (!import.meta.env.VITE_GOOGLE_CLIENT_ID) return null;

  return (
    <div style={{ display: "flex", justifyContent: "center", margin: "16px 0" }}>
      <div ref={containerRef} />
    </div>
  );
}
