import { checkAuth } from "/js/utils/authGuard.js";
import { API_BASE } from "/js/services/api.js";

// ── Proteger acceso ───────────────────────────────────────────
checkAuth();

const user = JSON.parse(localStorage.getItem("user"));
const token = localStorage.getItem("token");

// ── Header dropdown ───────────────────────────────────────────
if (user) {
    document.getElementById("welcomeMsg").textContent = `${user.name} ${user.lastName}`;
    document.getElementById("userEmail").textContent = user.email ?? "";
}

const trigger = document.getElementById("userMenuTrigger");
const dropdown = document.getElementById("userDropdown");
trigger.addEventListener("click", (e) => {
    e.stopPropagation();
    const open = dropdown.classList.toggle("is-open");
    trigger.setAttribute("aria-expanded", open);
});
document.addEventListener("click", () => {
    dropdown.classList.remove("is-open");
    trigger.setAttribute("aria-expanded", "false");
});
dropdown.addEventListener("click", (e) => e.stopPropagation());

document.getElementById("logoutBtn").addEventListener("click", () => {
    localStorage.removeItem("token");
    localStorage.removeItem("user");
    window.location.href = "/pages/auth/login.html";
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
        const res = await fetch(
            `${API_BASE}/user/UpdateUser?idUser=${user.idUser}`,
            {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": `Bearer ${token}`
                },
                body: JSON.stringify(dto)
            }
        );

        const data = await res.json().catch(() => ({}));

        if (!res.ok) throw new Error(data.message || "No se pudo cambiar la contraseña.");

        showMessage("✅ Contraseña actualizada correctamente. Por seguridad, vuelve a iniciar sesión.", "success");

        // Limpiar el formulario
        document.getElementById("securityForm").reset();
        matchHint.textContent = "";

    } catch (err) {
        showMessage(err.message || "Error al cambiar la contraseña.", "error");
    } finally {
        btn.disabled = false;
        btn.textContent = "Cambiar contraseña";
    }
});
