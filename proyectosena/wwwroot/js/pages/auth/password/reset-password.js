// ============================================================
//  reset-password.js
//  Flujo:
//    PASO 1 – Usuario ingresa el código de 6 dígitos
//             → se verifica con el backend (verify-reset-code)
//    PASO 2 – Si el código es válido, se muestra el formulario
//             de nueva contraseña y se envía (reset-password)
//    PASO 3 – Éxito: redirige al login
// ============================================================

import { verifyResetCode, resetPassword } from "./AuthService.js";

// ── Email guardado en forgot-password.js ──────────────────────
const email = sessionStorage.getItem("resetEmail");

// Si no hay email en sesión, manda de vuelta a forgot-password
if (!email) {
    window.location.href = "forgot-password.html";
}

// ── Referencias al DOM ────────────────────────────────────────

// Paso 1: verificar código
const step1 = document.getElementById("step1");
const codeInputs = document.querySelectorAll(".code-input");   // 6 inputs individuales
const codeError = document.getElementById("codeError");
const verifyBtn = document.getElementById("verifyBtn");
const verifyLabel = document.getElementById("verifyLabel");
const verifySpinner = document.getElementById("verifySpinner");
const resendBtn = document.getElementById("resendBtn");

// Paso 2: nueva contraseña
const step2 = document.getElementById("step2");
const newPassInput = document.getElementById("newPassword");
const confirmInput = document.getElementById("confirmPassword");
const passError = document.getElementById("passError");
const resetBtn = document.getElementById("resetBtn");
const resetLabel = document.getElementById("resetLabel");
const resetSpinner = document.getElementById("resetSpinner");

// Paso 3: éxito
const step3 = document.getElementById("step3");
const goToLoginBtn = document.getElementById("goToLoginBtn");

// ── Helpers de UI ─────────────────────────────────────────────
function showStep(step) {
    [step1, step2, step3].forEach(s => s?.classList.remove("visible"));
    step?.classList.add("visible");
}

function setLoading(btn, label, spinner, loading, defaultText, loadingText) {
    btn.disabled = loading;
    spinner.style.display = loading ? "inline-block" : "none";
    label.textContent = loading ? loadingText : defaultText;
}

function showError(el, msg) {
    el.textContent = msg;
    el.style.display = "block";
}

function clearError(el) {
    el.textContent = "";
    el.style.display = "none";
}

// ── Obtiene el código completo de los 6 inputs ────────────────
function getCode() {
    return [...codeInputs].map(i => i.value).join("").trim();
}

// ── Navegar entre los 6 inputs con teclado ───────────────────
codeInputs.forEach((input, idx) => {
    input.addEventListener("input", () => {
        clearError(codeError);
        // Solo deja un carácter y salta al siguiente
        input.value = input.value.replace(/\D/g, "").slice(-1);
        if (input.value && idx < codeInputs.length - 1) {
            codeInputs[idx + 1].focus();
        }
    });

    input.addEventListener("keydown", (e) => {
        if (e.key === "Backspace" && !input.value && idx > 0) {
            codeInputs[idx - 1].focus();
        }
    });

    // Permite pegar un código completo en el primer input
    input.addEventListener("paste", (e) => {
        e.preventDefault();
        const pasted = (e.clipboardData || window.clipboardData)
            .getData("text")
            .replace(/\D/g, "")
            .slice(0, 6);
        [...pasted].forEach((char, i) => {
            if (codeInputs[i]) codeInputs[i].value = char;
        });
        const lastFilled = Math.min(pasted.length, codeInputs.length - 1);
        codeInputs[lastFilled].focus();
    });
});

// ── PASO 1: verificar código ──────────────────────────────────
verifyBtn.addEventListener("click", async () => {
    clearError(codeError);
    const code = getCode();

    if (code.length < 6) {
        showError(codeError, "Ingresa los 6 dígitos del código.");
        return;
    }

    setLoading(verifyBtn, verifyLabel, verifySpinner, true, "Verificar código", "Verificando...");

    try {
        await verifyResetCode(email, code);

        // Guarda el código validado para enviarlo junto con la nueva contraseña
        sessionStorage.setItem("resetCode", code);

        // Avanza al paso 2
        showStep(step2);
        newPassInput.focus();

    } catch (error) {
        showError(codeError, error.message || "Código inválido o expirado.");
    } finally {
        setLoading(verifyBtn, verifyLabel, verifySpinner, false, "Verificar código", "Verificando...");
    }
});

// ── PASO 2: nueva contraseña ──────────────────────────────────

// Muestra/oculta la contraseña con el ícono del ojo (opcional)
document.querySelectorAll(".toggle-password").forEach(btn => {
    btn.addEventListener("click", () => {
        const target = document.getElementById(btn.dataset.target);
        target.type = target.type === "password" ? "text" : "password";
        btn.textContent = target.type === "password" ? "👁" : "🙈";
    });
});

function validatePasswords(pass, confirm) {
    if (!pass) return "La contraseña no puede estar vacía.";
    if (pass.length < 8) return "Debe tener al menos 8 caracteres.";
    if (pass !== confirm) return "Las contraseñas no coinciden.";
    return null;
}

resetBtn.addEventListener("click", async () => {
    clearError(passError);

    const newPass = newPassInput.value;
    const confirm = confirmInput.value;
    const code = sessionStorage.getItem("resetCode");

    const validationError = validatePasswords(newPass, confirm);
    if (validationError) {
        showError(passError, validationError);
        return;
    }

    setLoading(resetBtn, resetLabel, resetSpinner, true, "Restablecer contraseña", "Guardando...");

    try {
        await resetPassword(email, code, newPass);

        // Limpia sessionStorage
        sessionStorage.removeItem("resetEmail");
        sessionStorage.removeItem("resetCode");

        // Avanza al paso 3 (éxito)
        showStep(step3);

    } catch (error) {
        showError(passError, error.message || "Ocurrió un error. Intenta de nuevo.");
    } finally {
        setLoading(resetBtn, resetLabel, resetSpinner, false, "Restablecer contraseña", "Guardando...");
    }
});

// ── PASO 3: ir al login ───────────────────────────────────────
goToLoginBtn.addEventListener("click", () => {
    window.location.href = "login.html";
});

// ── Muestra el paso 1 al cargar ───────────────────────────────
showStep(step1);