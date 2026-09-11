import { BrowserRouter, Routes, Route } from "react-router-dom";
import { BookProvider } from "./context/BookContext";
import { AuthProvider } from "./context/AuthContext";
import Layout from "./components/Layout";
import Home from "./pages/Home";
import Login from "./pages/Login";
import Register from "./pages/Register";
import Profile from "./pages/Profile";
import FollowList from "./pages/FollowList";
import TrustedBookClub from "./pages/TrustedBookClub";
import Shelves from "./pages/Shelves";
import AddBook from "./pages/AddBook";
import BookDetail from "./pages/BookDetail";
import Browse from "./pages/Browse";
import Readers from "./pages/Readers";
import Requests from "./pages/Requests";
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
                path="/requests"
                element={
                  <ProtectedRoute>
                    <Requests />
                  </ProtectedRoute>
                }
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
