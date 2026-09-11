import { useOnlineStatus } from "@/context/OnlineStatusContext";

/**
 * Shown across the whole app whenever the browser reports no connection.
 * Doesn't block anything - surahs you've already opened once, and your
 * bookmarks/settings already loaded this session, keep working; only
 * features that genuinely need a live server (search, tafsir you haven't
 * viewed before, companions) won't. Reading progress and Wird completions
 * you record while offline are queued and sent automatically once back online.
 */
export function OfflineBanner() {
  const isOnline = useOnlineStatus();
  if (isOnline) return null;

  return (
    <div className="offline-banner">
      📡 أنت غير متصل بالإنترنت — بتقرا من المحتوى المحفوظ، وأي تقدّم هيتزامن تلقائيًا لما يرجع الاتصال.
    </div>
  );
}
