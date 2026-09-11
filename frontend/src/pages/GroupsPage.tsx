import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { groupsApi } from "@/api/groupsApi";
import type { GroupSummary } from "@/types/group";
import { extractApiError } from "@/api/client";

export function GroupsPage() {
  const [groups, setGroups] = useState<GroupSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);

  useEffect(() => {
    groupsApi.getMine().then(setGroups).finally(() => setLoading(false));
  }, []);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError(null);
    if (!name.trim()) {
      setError("الرجاء إدخال اسم للمجموعة.");
      return;
    }
    setCreating(true);
    try {
      const group = await groupsApi.create(name.trim());
      setGroups((prev) => [group, ...prev]);
      setName("");
    } catch (err) {
      setError(extractApiError(err));
    } finally {
      setCreating(false);
    }
  }

  return (
    <div className="quran-shell">
      <header className="quran-header">
        <Link to="/companions" className="btn-link">الرفقاء</Link>
        <h1 className="quran-title">مجموعاتي</h1>
        <span />
      </header>

      <form onSubmit={handleCreate} className="search-form">
        <input
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="اسم المجموعة (مثلاً: ورد العائلة)"
          className="search-input"
        />
        <button className="btn-primary search-submit" type="submit" disabled={creating}>
          {creating ? "..." : "إنشاء"}
        </button>
      </form>
      {error && <div className="banner banner-error">{error}</div>}

      {!loading && groups.length === 0 && (
        <p className="quran-loading">لسه معملتش أي مجموعة. أنشئ واحدة وضيف رفقاءك ليها.</p>
      )}

      <div className="bookmark-list">
        {groups.map((g) => (
          <Link key={g.id} to={`/groups/${g.id}`} className="bookmark-item" style={{ textDecoration: "none", display: "block" }}>
            <div className="bookmark-item-header">
              <span style={{ fontFamily: "var(--font-display-ar)", fontSize: "1.1rem" }}>{g.name}</span>
              <span>{g.memberCount} أعضاء</span>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
