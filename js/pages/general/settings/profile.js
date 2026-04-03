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

// ── Pre-rellenar formulario con datos del usuario ─────────────
document.getElementById("profileName").value = user?.name ?? "";
document.getElementById("profileLastName").value = user?.lastName ?? "";
document.getElementById("profileEmail").value = user?.email ?? "";
document.getElementById("profilePhone").value = user?.phoneNumber ?? "";
document.getElementById("profileAddress").value = user?.address ?? "";

// ── Utilidades ────────────────────────────────────────────────
function showMessage(text, type = "error") {
    const el = document.getElementById("profile-message");
    el.textContent = text;
    el.className = `settings-message ${type}`;
}

// ── Envío del formulario ──────────────────────────────────────
document.getElementById("profileForm").addEventListener("submit", async (e) => {
    e.preventDefault();

    const btn = document.getElementById("profileSubmitBtn");
    btn.disabled = true;
    btn.textContent = "Guardando...";
    showMessage("", "");

    const dto = {
        name: document.getElementById("profileName").value.trim() || null,
        lastName: document.getElementById("profileLastName").value.trim() || null,
        phoneNumber: document.getElementById("profilePhone").value.trim() || null,
        address: document.getElementById("profileAddress").value.trim() || null,
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

        if (!res.ok) throw new Error(data.message || "No se pudo actualizar el perfil.");

        // Actualizar datos en localStorage para que el header refleje los cambios
        const updatedUser = { ...user, ...data };
        localStorage.setItem("user", JSON.stringify(updatedUser));
        document.getElementById("welcomeMsg").textContent = `${updatedUser.name} ${updatedUser.lastName}`;

        showMessage("✅ Perfil actualizado correctamente.", "success");

    } catch (err) {
        showMessage(err.message || "Error al guardar cambios.", "error");
    } finally {
        btn.disabled = false;
        btn.textContent = "Guardar cambios";
    }
});
