import "./App.css";
import NavBar from "../Components/NavBar/NavBar";
import Footer from "../Components/Footer/Footer";
import Routes from "../Routes/Routes";

function App() {
  return (
    <div className="app">
      <NavBar />
      <main className="main-content">
        <Routes />
      </main>
      <Footer />
    </div>
  );
}

export default App;
