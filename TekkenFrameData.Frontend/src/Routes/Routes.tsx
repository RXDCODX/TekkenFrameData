import { BrowserRouter, Routes, Route } from "react-router";
import Main from "../Components/Main/Main";

export default function PrivateRoutes() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Main />} />
      </Routes>
    </BrowserRouter>
  );
}
