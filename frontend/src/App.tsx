import { BrowserRouter, Routes, Route } from "react-router-dom";
import { AuthProvider } from "@/context/AuthContext";
import { ThemeProvider } from "@/context/ThemeContext";
import { OnlineStatusProvider } from "@/context/OnlineStatusContext";
import { OfflineBanner } from "@/components/OfflineBanner";
import { ProtectedRoute } from "@/components/ProtectedRoute";
import { LoginPage } from "@/pages/LoginPage";
import { RegisterPage } from "@/pages/RegisterPage";
import { ForgotPasswordPage } from "@/pages/ForgotPasswordPage";
import { ProfilePage } from "@/pages/ProfilePage";
import { HomePage } from "@/pages/HomePage";
import { SurahListPage } from "@/pages/SurahListPage";
import { SurahReaderPage } from "@/pages/SurahReaderPage";
import { WirdSettingsPage } from "@/pages/WirdSettingsPage";
import { BookmarksPage } from "@/pages/BookmarksPage";
import { SearchPage } from "@/pages/SearchPage";
import { CompanionsPage } from "@/pages/CompanionsPage";
import { PrivacySettingsPage } from "@/pages/PrivacySettingsPage";
import { GroupsPage } from "@/pages/GroupsPage";
import { GroupDetailPage } from "@/pages/GroupDetailPage";

export default function App() {
  return (
    <ThemeProvider>
      <OnlineStatusProvider>
        <BrowserRouter>
          <AuthProvider>
            <OfflineBanner />
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route path="/register" element={<RegisterPage />} />
              <Route path="/forgot-password" element={<ForgotPasswordPage />} />
              <Route path="/" element={<ProtectedRoute><HomePage /></ProtectedRoute>} />
              <Route path="/profile" element={<ProtectedRoute><ProfilePage /></ProtectedRoute>} />
              <Route path="/quran" element={<ProtectedRoute><SurahListPage /></ProtectedRoute>} />
              <Route path="/quran/:surahNumber" element={<ProtectedRoute><SurahReaderPage /></ProtectedRoute>} />
              <Route path="/wird/settings" element={<ProtectedRoute><WirdSettingsPage /></ProtectedRoute>} />
              <Route path="/bookmarks" element={<ProtectedRoute><BookmarksPage /></ProtectedRoute>} />
              <Route path="/search" element={<ProtectedRoute><SearchPage /></ProtectedRoute>} />
              <Route path="/companions" element={<ProtectedRoute><CompanionsPage /></ProtectedRoute>} />
              <Route path="/privacy-settings" element={<ProtectedRoute><PrivacySettingsPage /></ProtectedRoute>} />
              <Route path="/groups" element={<ProtectedRoute><GroupsPage /></ProtectedRoute>} />
              <Route path="/groups/:groupId" element={<ProtectedRoute><GroupDetailPage /></ProtectedRoute>} />
            </Routes>
          </AuthProvider>
        </BrowserRouter>
      </OnlineStatusProvider>
    </ThemeProvider>
  );
}
