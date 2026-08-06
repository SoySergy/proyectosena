import { loginUser } from "../../services/authService.js";
import { isEmailValid, isMinLength } from "../../utils/validators.js";
import { redirectByRole } from "../../utils/roleGuard.js";

if (localStorage.getItem("token")) {
    redirectByRole();
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
            redirectByRole();
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

// ── Ver contraseña mientras se mantiene presionado el botón ──
const togglePasswordBtn = document.getElementById("togglePassword");
const passwordInput = document.getElementById("password");
const togglePasswordIcon = togglePasswordBtn.querySelector(".form-input-icon");

function revealPassword() {
    passwordInput.type = "text";
    togglePasswordIcon.classList.replace("icon-ojocerrado", "icon-ojo");
}

function hidePassword() {
    passwordInput.type = "password";
    togglePasswordIcon.classList.replace("icon-ojo", "icon-ojocerrado");
}

togglePasswordBtn.addEventListener("mousedown", (e) => {
    e.preventDefault();
    revealPassword();
});
togglePasswordBtn.addEventListener("mouseup", hidePassword);
togglePasswordBtn.addEventListener("mouseleave", hidePassword);
togglePasswordBtn.addEventListener("touchstart", (e) => {
    e.preventDefault();
    revealPassword();
});
togglePasswordBtn.addEventListener("touchend", hidePassword);
togglePasswordBtn.addEventListener("touchcancel", hidePassword);
togglePasswordBtn.addEventListener("keydown", (e) => {
    if (e.key === " " || e.key === "Enter") {
        e.preventDefault();
        revealPassword();
    }
});
togglePasswordBtn.addEventListener("keyup", (e) => {
    if (e.key === " " || e.key === "Enter") hidePassword();
});