import { checkAuth } from "../../utils/authGuard.js";

// 🔐 Proteger acceso
checkAuth();

// 👤 Mostrar usuario
const user = JSON.parse(localStorage.getItem("user"));

if (user) {
    document.getElementById("welcome").textContent =
        `Bienvenido, ${user.name}`;
}

// 🚪 Logout
const btn = document.getElementById("logoutBtn");

btn.addEventListener("click", () => {
    localStorage.removeItem("token");
    localStorage.removeItem("user");

    window.location.href = "/pages/auth/login.html";
});