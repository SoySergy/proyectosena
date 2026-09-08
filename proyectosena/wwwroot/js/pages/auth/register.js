import { registerUser } from "../../services/authService.js";
import { isEmailValid, isMinLength } from "../../utils/validators.js";

const form = document.getElementById("registerForm");
const message = document.getElementById("message");
const btn = document.getElementById("submitBtn");

form.addEventListener("submit", async (e) => {
    e.preventDefault();

    message.textContent = "";
    btn.disabled = true;
    btn.textContent = "Registrando...";

    const data = {
        idRole: "7D759012-4D17-46E8-BCE8-D74FDF171EAB",
        idDocumentType: document.getElementById("documentType").value,
        name: document.getElementById("name").value.trim(),
        lastName: document.getElementById("lastName").value.trim(),
        documentNumber: document.getElementById("documentNumber").value.trim(),
        phoneNumber: document.getElementById("phoneNumber").value.trim(),
        address: document.getElementById("address").value.trim(),
        email: document.getElementById("email").value.trim(),
        password: document.getElementById("password").value.trim()
    };

    // 🔍 VALIDACIONES (según tu DTO)
    if (!isMinLength(data.name, 2)) {
        return showError("El nombre debe tener mínimo 2 caracteres");
    }

    if (!isMinLength(data.lastName, 2)) {
        return showError("El apellido debe tener mínimo 2 caracteres");
    }

    if (!isMinLength(data.documentNumber, 2)) {
        return showError("Documento inválido");
    }

    if (!isMinLength(data.phoneNumber, 7)) {
        return showError("Teléfono inválido (mínimo 7 caracteres)");
    }

    if (!isMinLength(data.address, 5)) {
        return showError("Dirección muy corta");
    }

    if (!isEmailValid(data.email)) {
        return showError("Correo inválido");
    }

    if (!isMinLength(data.password, 8)) {
        return showError("La contraseña debe tener mínimo 8 caracteres");
    }

    if (!data.idDocumentType) {
        return showError("Selecciona un tipo de documento");
    }

    try {
        const result = await registerUser(data);

        // El registro ya no entrega sesión: la cuenta queda creada pero sin
        // confirmar, y el backend acaba de enviar un código al correo.
        // El token llega cuando la persona confirme, no antes.
        localStorage.setItem("pendingVerificationEmail", result.email);

        message.style.color = "green";
        message.textContent = result.message
            || "Cuenta creada. Te enviamos un código para confirmar tu correo.";

        form.reset();

    } catch (error) {
        showError(error.message || "Error al registrarse");
    } finally {
        btn.disabled = false;
        btn.textContent = "Registrarse";
    }
});

function showError(msg) {
    message.style.color = "red";
    message.textContent = msg;
    btn.disabled = false;
    btn.textContent = "Registrarse";
}