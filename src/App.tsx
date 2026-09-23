import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { BookProvider } from "./context/BookContext";
import { AuthProvider } from "./context/AuthContext";
import Layout from "./components/Layout";
import Home from "./pages/Home";
import Login from "./pages/Login";
import Register from "./pages/Register";
import Welcome from "./pages/Welcome";
import Privacy from "./pages/Privacy";
import Terms from "./pages/Terms";
import Profile from "./pages/Profile";
import FollowList from "./pages/FollowList";
import TrustedBookClub from "./pages/TrustedBookClub";
import Shelves from "./pages/Shelves";
import AddBook from "./pages/AddBook";
import ReadingGoals from "./pages/ReadingGoals";
import BookDetail from "./pages/BookDetail";
import Browse from "./pages/Browse";
import Readers from "./pages/Readers";
import Borrowing from "./pages/Borrowing";
import Conversation from "./pages/Conversation";
import NotFound from "./pages/NotFound";
import ProtectedRoute from "./components/ProtectedRoute";

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <BookProvider>
          <Routes>
            <Route element={<Layout />}>
              <Route path="/" element={<Home />} />
              <Route path="/login" element={<Login />} />
              <Route path="/register" element={<Register />} />
              <Route path="/welcome" element={<Welcome />} />
              <Route path="/privacy" element={<Privacy />} />
              <Route path="/terms" element={<Terms />} />
              <Route
                path="/profile/:username"
                element={
                  <ProtectedRoute>
                    <Profile />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/profile/:username/followers"
                element={
                  <ProtectedRoute>
                    <FollowList mode="followers" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/profile/:username/following"
                element={
                  <ProtectedRoute>
                    <FollowList mode="following" />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/trusted-book-club"
                element={
                  <ProtectedRoute>
                    <TrustedBookClub />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/shelves"
                element={
                  <ProtectedRoute>
                    <Shelves />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/reading-goals"
                element={
                  <ProtectedRoute>
                    <ReadingGoals />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/add-book"
                element={
                  <ProtectedRoute>
                    <AddBook />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/book/:id"
                element={
                  <ProtectedRoute>
                    <BookDetail />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/browse"
                element={
                  <ProtectedRoute>
                    <Browse />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/readers"
                element={
                  <ProtectedRoute>
                    <Readers />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/borrowing"
                element={
                  <ProtectedRoute>
                    <Borrowing />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/requests"
                element={<Navigate to="/borrowing" replace />}
              />
              <Route
                path="/messages/:requestId"
                element={
                  <ProtectedRoute>
                    <Conversation />
                  </ProtectedRoute>
                }
              />
              <Route path="*" element={<NotFound />} />
            </Route>
          </Routes>
        </BookProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
