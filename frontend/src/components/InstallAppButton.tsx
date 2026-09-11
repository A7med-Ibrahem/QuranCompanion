import { useEffect, useState } from "react";

/**
 * The browser's native "install" affordance (small icon in the address bar,
 * or nothing at all on iOS Safari) is easy to miss and only shows up once
 * the browser's own engagement heuristics are satisfied. This renders a
 * persistent, always-visible floating button instead, so installing the app
 * is never something the user has to go hunting for.
 */

interface BeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: "accepted" | "dismissed" }>;
}

function isStandalone() {
  return (
    window.matchMedia("(display-mode: standalone)").matches ||
    // iOS Safari doesn't support display-mode: standalone detection the same way
    (window.navigator as unknown as { standalone?: boolean }).standalone === true
  );
}

function isIos() {
  return /iphone|ipad|ipod/i.test(window.navigator.userAgent);
}

export function InstallAppButton() {
  const [deferredPrompt, setDeferredPrompt] = useState<BeforeInstallPromptEvent | null>(null);
  const [installed, setInstalled] = useState(isStandalone());
  const [showIosHint, setShowIosHint] = useState(false);

  useEffect(() => {
    // Chrome/Edge/Android fire this instead of showing their own icon
    // immediately - we capture it so *we* control when/how it's offered.
    const handleBeforeInstall = (e: Event) => {
      e.preventDefault();
      setDeferredPrompt(e as BeforeInstallPromptEvent);
    };
    const handleInstalled = () => {
      setInstalled(true);
      setDeferredPrompt(null);
    };

    window.addEventListener("beforeinstallprompt", handleBeforeInstall);
    window.addEventListener("appinstalled", handleInstalled);

    return () => {
      window.removeEventListener("beforeinstallprompt", handleBeforeInstall);
      window.removeEventListener("appinstalled", handleInstalled);
    };
  }, []);

  // Already installed - nothing to offer, keep the UI clean.
  if (installed) return null;

  const handleClick = async () => {
    if (deferredPrompt) {
      await deferredPrompt.prompt();
      const choice = await deferredPrompt.userChoice;
      if (choice.outcome === "accepted") setInstalled(true);
      setDeferredPrompt(null);
      return;
    }
    // iOS Safari never fires beforeinstallprompt - only path there is the
    // manual Share > Add to Home Screen flow, so guide the user to it.
    if (isIos()) {
      setShowIosHint(true);
    }
    // Otherwise: browser doesn't support installability yet, or its own
    // eligibility rules aren't met - button stays visible, just a no-op tap.
  };

  return (
    <>
      <button
        type="button"
        className="install-app-btn"
        onClick={handleClick}
        aria-label="تثبيت التطبيق"
        title="تثبيت التطبيق"
      >
        ⬇️ تثبيت التطبيق
      </button>

      {showIosHint && (
        <div
          className="install-app-ios-hint"
          role="dialog"
          aria-modal="true"
          onClick={() => setShowIosHint(false)}
        >
          <div className="install-app-ios-hint-card" onClick={(e) => e.stopPropagation()}>
            <p>لتثبيت التطبيق على آيفون:</p>
            <p>
              اضغط زر المشاركة <strong>⬆️</strong> في شريط المتصفح، ثم اختر{" "}
              <strong>"إضافة إلى الشاشة الرئيسية"</strong>.
            </p>
            <button
              type="button"
              className="install-app-ios-hint-close"
              onClick={() => setShowIosHint(false)}
            >
              حسنًا
            </button>
          </div>
        </div>
      )}
    </>
  );
}
