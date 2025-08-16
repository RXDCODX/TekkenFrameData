import styles from "./App.module.scss";
import NavBar from "../Components/NavBar/NavBar";
import Footer from "../Components/Footer/Footer";
import Routes from "../Routes/Routes";

function App() {
  return (
    <div className={styles.app}>
      <NavBar />
      <main className={styles.mainContent}>
        <Routes />
      </main>
      <Footer />
    </div>
  );
}

export default App;
