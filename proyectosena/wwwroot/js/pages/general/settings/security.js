import { checkAuth } from "/js/utils/authGuard.js";
import { API_BASE, authHeaders, fetchConSesion, leerCuerpo, mensajeDeError, volverAlLogin } from "/js/services/api.js";
import { initUserMenu } from "/js/utils/userMenu.js";

// ── Proteger acceso ───────────────────────────────────────────
checkAuth();

// Header dropdown y cierre de sesión con revocación en servidor
const user = initUserMenu();

// ── Botón volver ───────────────────────────────────────────────
document.getElementById("settingsBackLink").addEventListener("click", (e) => {
    e.preventDefault();
    history.back();
});

// Toggle mostrar/ocultar contraseña
document.querySelectorAll('.toggle-pwd').forEach(btn => {
    btn.addEventListener('click', () => {
        const targetId = btn.dataset.target;
        const input = document.getElementById(targetId);
        const icon = btn.querySelector('.form-input-icon');

        const isHidden = input.type === 'password';

        // Cambiar tipo del input
        input.type = isHidden ? 'text' : 'password';

        // Cambiar ícono: cerrado → abierto y viceversa
        icon.classList.toggle('icon-ojocerrado', !isHidden);
        icon.classList.toggle('icon-ojo', isHidden);

        // Actualizar accesibilidad
        btn.setAttribute('aria-label', isHidden ? 'Ocultar contraseña' : 'Mostrar contraseña');
    });
});

// ── Validación en tiempo real de coincidencia ─────────────────
const newPwd = document.getElementById("newPassword");
const confirmPwd = document.getElementById("confirmPassword");
const matchHint = document.getElementById("pwd-match-hint");

function checkMatch() {
    if (!confirmPwd.value) { matchHint.textContent = ""; return; }
    if (newPwd.value === confirmPwd.value) {
        matchHint.textContent = "✅ Las contraseñas coinciden";
        matchHint.style.color = "#155724";
    } else {
        matchHint.textContent = "❌ Las contraseñas no coinciden";
        matchHint.style.color = "#721c24";
    }
}

newPwd.addEventListener("input", checkMatch);
confirmPwd.addEventListener("input", checkMatch);

// ── Utilidad mensaje ──────────────────────────────────────────
function showMessage(text, type = "error") {
    const el = document.getElementById("security-message");
    el.textContent = text;
    el.className = `settings-message ${type}`;
}

// ── Envío del formulario ──────────────────────────────────────
document.getElementById("securityForm").addEventListener("submit", async (e) => {
    e.preventDefault();

    const currentPassword = document.getElementById("currentPassword").value;
    const newPassword = document.getElementById("newPassword").value;
    const confirmPassword = document.getElementById("confirmPassword").value;

    // Validar coincidencia antes de enviar
    if (newPassword !== confirmPassword) {
        showMessage("Las contraseñas nuevas no coinciden.", "error");
        return;
    }

    if (newPassword.length < 8) {
        showMessage("La nueva contraseña debe tener mínimo 8 caracteres.", "error");
        return;
    }

    const btn = document.getElementById("securitySubmitBtn");
    btn.disabled = true;
    btn.textContent = "Cambiando...";
    showMessage("", "");

    const dto = {
        currentPassword,
        newPassword
    };

    try {
        const res = await fetchConSesion(
            `${API_BASE}/user/UpdateUser`,
            {
                method: "PUT",
                headers: authHeaders(),
                body: JSON.stringify(dto)
            }
        );

        const data = await leerCuerpo(res);

        if (!res.ok) throw new Error(mensajeDeError(data, "No se pudo cambiar la contraseña."));

        // El servidor ya anuló esta sesión al cambiar la contraseña (BL-07): el token de aquí no sirve.
        volverAlLogin("contrasena");

    } catch (err) {
        showMessage(err.message || "Error al cambiar la contraseña.", "error");
    } finally {
        btn.disabled = false;
        btn.textContent = "Cambiar contraseña";
    }
});
