import { BrowserRouter, Routes, Route } from "react-router-dom";
import { BookProvider } from "./context/BookContext";
import { AuthProvider } from "./context/AuthContext";
import Layout from "./components/Layout";
import Home from "./pages/Home";
import Login from "./pages/Login";
import Register from "./pages/Register";
import Profile from "./pages/Profile";
import Shelves from "./pages/Shelves";
import AddBook from "./pages/AddBook";
import BookDetail from "./pages/BookDetail";
import Browse from "./pages/Browse";
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
