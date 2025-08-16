import { BrowserRouter, Routes, Route } from "react-router-dom";
import WelcomePage from "../Components/WelcomePage/WelcomePage";
import CharacterPage from "../Components/CharacterPage/CharacterPage";
import MovePage from "../Components/MovePage/MovePage";
import UserManagementPage from "../Components/UserManagementPage/UserManagementPage";
import LoginPage from "../Components/LoginPage/LoginPage";
import AuthCallback from "../Components/AuthCallback/AuthCallback";

export default function AppRoutes() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<WelcomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/auth-callback" element={<AuthCallback />} />
        <Route path="/characters" element={<CharacterPage />} />
        <Route path="/moves" element={<MovePage />} />
        <Route path="/admin/users" element={<UserManagementPage />} />
      </Routes>
    </BrowserRouter>
  );
}
