import { loginUser } from "../../services/authService.js";
import { isEmailValid, isMinLength } from "../../utils/validators.js";

if (localStorage.getItem("token")) {
    window.location.href = "/pages/citizen/dashboard.html";
}

const form = document.getElementById("loginForm");
const message = document.getElementById("message");
const btn = document.getElementById("submitBtn");

form.addEventListener("submit", async (e) => {
    e.preventDefault();

    message.textContent = "";
    btn.disabled = true;
    btn.textContent = "Ingresando...";

    const data = {
        email: document.getElementById("email").value.trim(),
        password: document.getElementById("password").value.trim()
    };

    // 🔍 VALIDACIONES (según tu backend)
    if (!isEmailValid(data.email)) {
        return showError("Correo inválido");
    }

    if (!isMinLength(data.password, 8)) {
        return showError("La contraseña debe tener mínimo 8 caracteres");
    }

    try {
        const result = await loginUser(data);

        // ✅ Guardar sesión
        localStorage.setItem("token", result.token);
        localStorage.setItem("user", JSON.stringify(result.user));

        message.style.color = "green";
        message.textContent = "Login exitoso";

        // 🚀 Redirigir a dashboard
        setTimeout(() => {
            window.location.href = "/pages/citizen/dashboard.html";
        }, 1000);

    } catch (error) {
        showError(error.message || "Credenciales inválidas");
    } finally {
        btn.disabled = false;
        btn.textContent = "Ingresar";
    }
});

function showError(msg) {
    message.style.color = "red";
    message.textContent = msg;
    btn.disabled = false;
    btn.textContent = "Ingresar";
}