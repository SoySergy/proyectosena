import { checkAuth } from "/js/utils/authGuard.js";
import { API_BASE, authHeaders, fetchConSesion, leerCuerpo, mensajeDeError } from "/js/services/api.js";
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
        const res = await fetchConSesion(
            `${API_BASE}/user/UpdateUser?idUser=${user.idUser}`,
            {
                method: "PUT",
                headers: authHeaders(),
                body: JSON.stringify(dto)
            }
        );

        const data = await leerCuerpo(res);

        if (!res.ok) throw new Error(mensajeDeError(data, "No se pudo actualizar el perfil."));

        // Actualizar datos en localStorage para que el header refleje los cambios
        const updatedUser = { ...user, ...data };
        localStorage.setItem("user", JSON.stringify(updatedUser));
        // Mismo formato que pone userMenu.js al cargar la página: sin el
        // "Hola, " se veía distinto justo después de guardar.
        document.getElementById("welcomeMsg").textContent = `Hola, ${updatedUser.name} ${updatedUser.lastName}`;

        showMessage("✅ Perfil actualizado correctamente.", "success");

    } catch (err) {
        showMessage(err.message || "Error al guardar cambios.", "error");
    } finally {
        btn.disabled = false;
        btn.textContent = "Guardar cambios";
    }
});
