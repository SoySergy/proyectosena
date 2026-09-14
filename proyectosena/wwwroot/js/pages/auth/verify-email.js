import { verifyEmail, resendVerification } from "../../services/authService.js";
import { redirectByRole } from "../../utils/roleGuard.js";

const params = new URLSearchParams(window.location.search);
const email = params.get("email") || localStorage.getItem("pendingVerificationEmail");

if (!email) {
    window.location.href = "/login";
}

const userEmailLabel = document.getElementById("userEmailLabel");
if (userEmailLabel) {
    userEmailLabel.textContent = email;
}

const codeInputs = document.querySelectorAll(".code-input");
const codeError = document.getElementById("codeError");
const successMsg = document.getElementById("successMsg");
const verifyBtn = document.getElementById("verifyBtn");
const verifyLabel = document.getElementById("verifyLabel");
const verifySpinner = document.getElementById("verifySpinner");
const resendBtn = document.getElementById("resendBtn");

function getCode() {
    return [...codeInputs].map(i => i.value).join("").trim();
}

function showError(msg) {
    codeError.textContent = msg;
    codeError.style.display = "block";
    successMsg.style.display = "none";
}

function showSuccess(msg) {
    successMsg.textContent = msg;
    successMsg.style.display = "block";
    codeError.style.display = "none";
}

function clearMessages() {
    codeError.textContent = "";
    codeError.style.display = "none";
    successMsg.textContent = "";
    successMsg.style.display = "none";
}

codeInputs.forEach((input, idx) => {
    input.addEventListener("input", () => {
        clearMessages();
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

verifyBtn.addEventListener("click", async () => {
    clearMessages();
    const code = getCode();

    if (code.length < 6) {
        showError("Ingresa los 6 dígitos del código.");
        return;
    }

    verifyBtn.disabled = true;
    verifySpinner.style.display = "inline-block";
    verifyLabel.textContent = "Verificando...";

    try {
        const result = await verifyEmail(email, code);

        // Guardar sesión y usuario devuelto por el backend
        if (result && result.token) {
            localStorage.setItem("token", result.token);
            localStorage.setItem("user", JSON.stringify(result.user));
            localStorage.removeItem("pendingVerificationEmail");
        }

        showSuccess("¡Correo confirmado con éxito! Entrando al sistema...");

        setTimeout(() => {
            redirectByRole();
        }, 1200);

    } catch (error) {
        showError(error.message || "Código inválido o expirado.");
        verifyBtn.disabled = false;
        verifySpinner.style.display = "none";
        verifyLabel.textContent = "Confirmar cuenta";
    }
});

resendBtn.addEventListener("click", async () => {
    clearMessages();
    try {
        await resendVerification(email);
        showSuccess("Se ha reenviado un nuevo código a tu correo.");
    } catch (error) {
        showError(error.message || "No se pudo reenviar el código.");
    }
});
