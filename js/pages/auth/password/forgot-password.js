// ============================================================
//  forgot-password.js
//  Flujo: usuario ingresa email → backend envía código de 6 dígitos
//         → redirige a reset-password.html para ingresar código + nueva contraseña
// ============================================================

import { forgotPassword } from "./AuthService.js";

// ── Referencias al DOM ──────────────────────────────────────
const form = document.getElementById("forgotForm");
const emailInput = document.getElementById("email");
const emailError = document.getElementById("emailError");
const submitBtn = document.getElementById("submitBtn");
const btnLabel = document.getElementById("btnLabel");
const btnSpinner = document.getElementById("btnSpinner");
const successState = document.getElementById("successState");
const successEmail = document.getElementById("successEmail");
const goToVerifyBtn = document.getElementById("goToVerifyBtn");

// ── Helpers de UI ───────────────────────────────────────────

/** Muestra u oculta el spinner y deshabilita el botón mientras carga */
function setLoading(loading) {
    submitBtn.disabled = loading;
    btnSpinner.style.display = loading ? "inline-block" : "none";
    btnLabel.textContent = loading ? "Enviando..." : "Enviar código";
}

/** Muestra un mensaje de error bajo el input de email */
function showEmailError(msg) {
    emailError.textContent = msg;
    emailError.style.display = "block";
    emailInput.classList.add("input--error");
}

/** Limpia el error del input de email */
function clearEmailError() {
    emailError.textContent = "";
    emailError.style.display = "none";
    emailInput.classList.remove("input--error");
}

/** Oculta el formulario y muestra el estado de éxito */
function showSuccess(email) {
    form.style.display = "none";
    successEmail.textContent = email;
    successState.classList.add("visible");
}

// ── Validación local del email ───────────────────────────────
function validateEmail(value) {
    if (!value.trim()) return "El correo es obligatorio.";
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(value)) return "Ingresa un correo electrónico válido.";
    return null; // Sin errores
}

// ── Evento: limpiar error cuando el usuario empieza a escribir ──
emailInput.addEventListener("input", clearEmailError);

// ── Evento: submit del formulario ───────────────────────────
form.addEventListener("submit", async (e) => {
    e.preventDefault();
    clearEmailError();

    const email = emailInput.value.trim();

    // Validación local antes de llamar al backend
    const validationError = validateEmail(email);
    if (validationError) {
        showEmailError(validationError);
        emailInput.focus();
        return;
    }

    setLoading(true);

    try {
        // Llama al endpoint POST /api/auth/forgot-password
        await forgotPassword(email);

        // Guarda el email en sessionStorage para usarlo en reset-password.html
        sessionStorage.setItem("resetEmail", email);

        // Muestra el estado de éxito
        showSuccess(email);

    } catch (error) {
        // El backend puede devolver un error si el correo no existe,
        // pero por seguridad también puedes mostrar éxito igualmente
        // (para no revelar qué emails están registrados).
        // Aquí mostramos el mensaje real del backend:
        showEmailError(error.message || "Ocurrió un error. Intenta de nuevo.");
    } finally {
        setLoading(false);
    }
});

// ── Botón "Ingresar código" → redirige a reset-password.html ──
goToVerifyBtn.addEventListener("click", () => {
    window.location.href = "reset-password.html";
});