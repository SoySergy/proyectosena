import { loginUser } from "../../services/authService.js";
import { isEmailValid, isMinLength } from "../../utils/validators.js";
import { redirectByRole } from "../../utils/roleGuard.js";

if (localStorage.getItem("token")) {
    redirectByRole();
}

const form = document.getElementById("loginForm");
const message = document.getElementById("message");
const btn = document.getElementById("submitBtn");

// Las otras pantallas mandan aquí con ?sesion=... cuando la sesión se cerró sin que la persona lo pidiera.
const AVISOS_DE_SESION = new Map([
    ["caducada", { texto: "Tu sesión caducó. Vuelve a iniciar sesión.", color: "red" }],
    ["contrasena", { texto: "Contraseña actualizada. Entra con tu nueva contraseña.", color: "green" }],
]);

const avisoDeSesion = AVISOS_DE_SESION.get(new URLSearchParams(window.location.search).get("sesion"));
if (avisoDeSesion) {
    message.style.color = avisoDeSesion.color;
    message.textContent = avisoDeSesion.texto;
    // Que recargar el login no repita el aviso.
    history.replaceState(null, "", window.location.pathname);
}

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
        // Falta confirmar el correo, no es un fallo de credenciales.
        //
        // Antes esto solo mostraba el aviso y ahí terminaba: a la pantalla de
        // confirmación únicamente se llegaba en el instante de registrarse, así
        // que quien cerraba la pestaña y volvía al día siguiente quedaba sin
        // salida —le pedían confirmar y no había dónde—.
        if (error.correoSinConfirmar) {
            message.style.color = "red";
            message.textContent = error.message;

            // Mismo respaldo que deja el registro, por si el enlace pierde la
            // dirección por el camino.
            localStorage.setItem("pendingVerificationEmail", data.email);

            setTimeout(() => {
                window.location.href =
                    `verify-email.html?email=${encodeURIComponent(data.email)}`;
            }, 1500);

            return;
        }

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