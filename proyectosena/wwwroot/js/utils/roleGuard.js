/**
 * roleGuard.js
 * Maneja el enrutamiento y protección de páginas según el rol del usuario.
 * Roles válidos: "Citizen" | "Manager"
 */

// ── Rutas por rol ─────────────────────────────────────────────
const ROLE_DASHBOARDS = {
    Citizen: "/pages/citizen/dashboard.html",
    Manager: "/pages/manager/dashboard.html",
};

const LOGIN_URL = "/pages/auth/login.html";

// ── Función privada: obtener rol actual ───────────────────────
function getCurrentRole() {
    const token = localStorage.getItem("token");
    if (!token) return null;

    try {
        // Validar expiración del JWT
        const payload = JSON.parse(atob(token.split(".")[1]));
        if (Date.now() > payload.exp * 1000) {
            // Token expirado: limpiar sesión
            localStorage.removeItem("token");
            localStorage.removeItem("user");
            return null;
        }

        // Leer roleName del objeto user guardado en localStorage
        const user = JSON.parse(localStorage.getItem("user"));
        return user?.roleName ?? null;

    } catch {
        localStorage.removeItem("token");
        localStorage.removeItem("user");
        return null;
    }
}

// ── redirectByRole ────────────────────────────────────────────
/**
 * Lee el rol del usuario y lo redirige al dashboard correcto.
 * Usar después de un login exitoso.
 *
 * @example
 * import { redirectByRole } from "../../utils/roleGuard.js";
 * redirectByRole();
 */
export function redirectByRole() {
    const role = getCurrentRole();

    if (!role) {
        window.location.href = LOGIN_URL;
        return;
    }

    const destination = ROLE_DASHBOARDS[role];
    if (destination) {
        window.location.href = destination;
    } else {
        // Rol desconocido: enviar a login por seguridad
        console.warn(`Rol desconocido: "${role}". Redirigiendo a login.`);
        window.location.href = LOGIN_URL;
    }
}

// ── requireRole ───────────────────────────────────────────────
/**
 * Verifica que el usuario autenticado tenga el rol requerido.
 * Si no tiene token → redirige a login.
 * Si tiene otro rol → redirige a su propio dashboard.
 *
 * Llamar al inicio de cada página protegida.
 *
 * @param {string} allowedRole - "Citizen" o "Manager"
 *
 * @example
 * import { requireRole } from "../../utils/roleGuard.js";
 * requireRole("Manager");
 */
export function requireRole(allowedRole) {
    const role = getCurrentRole();

    // Sin sesión → login
    if (!role) {
        window.location.href = LOGIN_URL;
        return;
    }

    // Rol correcto → dejar pasar
    if (role === allowedRole) return;

    // Rol incorrecto → redirigir a su propio dashboard
    const destination = ROLE_DASHBOARDS[role] ?? LOGIN_URL;
    window.location.href = destination;
}
