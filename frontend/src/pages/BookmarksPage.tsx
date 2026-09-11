import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { bookmarksApi } from "@/api/bookmarksApi";
import type { Bookmark } from "@/types/bookmark";

export function BookmarksPage() {
  const [bookmarks, setBookmarks] = useState<Bookmark[]>([]);
  const [loading, setLoading] = useState(true);
  const [editingNote, setEditingNote] = useState<string | null>(null); // "surah-ayah" key
  const [noteDraft, setNoteDraft] = useState("");

  useEffect(() => {
    load();
  }, []);

  function load() {
    setLoading(true);
    bookmarksApi.getAll().then(setBookmarks).finally(() => setLoading(false));
  }

  async function handleRemove(surah: number, ayah: number) {
    await bookmarksApi.remove(surah, ayah);
    setBookmarks((prev) => prev.filter((b) => !(b.surahNumber === surah && b.ayahNumber === ayah)));
  }

  async function handleSaveNote(surah: number, ayah: number) {
    const updated = await bookmarksApi.updateNote(surah, ayah, noteDraft);
    setBookmarks((prev) =>
      prev.map((b) => (b.surahNumber === surah && b.ayahNumber === ayah ? updated : b))
    );
    setEditingNote(null);
  }

  return (
    <div className="quran-shell">
      <header className="quran-header">
        <Link to="/" className="btn-link">الرئيسية</Link>
        <h1 className="quran-title">محفوظاتي</h1>
        <span />
      </header>

      {loading && <p className="quran-loading">جارٍ التحميل...</p>}

      {!loading && bookmarks.length === 0 && (
        <p className="quran-loading">لسه معملتش أي حفظ لآية. من شاشة القراءة، دوس على رقم الآية واختر "حفظ الآية".</p>
      )}

      <div className="bookmark-list">
        {bookmarks.map((b) => {
          const key = `${b.surahNumber}-${b.ayahNumber}`;
          const isEditing = editingNote === key;
          return (
            <div key={key} className="bookmark-item">
              <div className="bookmark-item-header">
                <span>سورة {b.surahArabicName} — آية {b.ayahNumber}</span>
                <span>{new Date(b.createdAtUtc).toLocaleDateString("ar-EG")}</span>
              </div>
              <div className="bookmark-item-text">{b.ayahText}</div>

              {b.note && !isEditing && <div className="bookmark-item-note">{b.note}</div>}

              {isEditing && (
                <div className="field">
                  <input
                    value={noteDraft}
                    onChange={(e) => setNoteDraft(e.target.value)}
                    placeholder="اكتب ملاحظتك الخاصة هنا..."
                  />
                </div>
              )}

              <div className="bookmark-item-actions">
                <Link className="btn-link" to={`/quran/${b.surahNumber}?ayah=${b.ayahNumber}`}>
                  الذهاب للآية
                </Link>
                {isEditing ? (
                  <button className="btn-link" onClick={() => handleSaveNote(b.surahNumber, b.ayahNumber)}>
                    حفظ الملاحظة
                  </button>
                ) : (
                  <button
                    className="btn-link"
                    onClick={() => {
                      setEditingNote(key);
                      setNoteDraft(b.note ?? "");
                    }}
                  >
                    {b.note ? "تعديل الملاحظة" : "إضافة ملاحظة"}
                  </button>
                )}
                <button className="btn-link" onClick={() => handleRemove(b.surahNumber, b.ayahNumber)}>
                  حذف
                </button>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
