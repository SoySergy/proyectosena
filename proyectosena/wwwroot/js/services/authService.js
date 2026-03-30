import { API_BASE } from "./api.js";

// REGISTER
// Envía los datos del formulario al backend para crear un usuario
export async function registerUser(data) {
    const response = await fetch(`${API_BASE}/Auth/Register`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(data)
    });

    const text = await response.text();

    let result;

    try {
        result = JSON.parse(text);
    } catch {
        result = text;
    }

    // maneja todos los formatos de error de ASP.NET
    if (!response.ok) {
        if (result?.message) throw new Error(result.message);
        if (result?.errors) {
            const firstError = Object.values(result.errors).flat()[0];
            throw new Error(firstError || "Error de validación");
        }
        if (result?.title) throw new Error(result.title);
        throw new Error(typeof result === "string" ? result : "Error en el registro");
    }
    return result;
}

// Login
export async function loginUser(data) {
    let response;

    try {
        response = await fetch(`${API_BASE}/auth/login`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(data)
        });
    } catch (networkError) {
        console.error("ERROR DE RED:", networkError);
        throw new Error("No se pudo conectar con el servidor");
    }

    // 👇 Leer como texto primero, luego intentar parsear JSON
    const text = await response.text();
    let result;
    try {
        result = JSON.parse(text);
    } catch {
        result = text; // Si no es JSON, usar el texto plano directamente
    }

    if (!response.ok) {
        // result puede ser objeto {message:...} o string directo
        throw new Error(result.message || result || "Error en login");
    }

    return result;
}

/**
* Paso 1: Solicita el código OTP al backend.
* POST /api/auth/forgot-password
*/
export async function forgotPassword(email) {
    const res = await fetch(`${API_BASE}/forgot-password`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email }),
    });

    if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        throw new Error(data.message || "Error al enviar el código.");
    }
}

/**
 * Paso 2: Verifica que el código ingresado sea válido.
 * POST /api/auth/verify-reset-code
 */
export async function verifyResetCode(email, code) {
    const res = await fetch(`${API_BASE}/verify-reset-code`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, code }),
    });

    if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        throw new Error(data.message || "Código inválido o expirado.");
    }
}

/**
 * Paso 3: Envía el código verificado + la nueva contraseña.
 * POST /api/auth/reset-password
 */
export async function resetPassword(email, code, newPassword) {
    const res = await fetch(`${API_BASE}/reset-password`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, code, newPassword }),
    });

    if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        throw new Error(data.message || "Error al restablecer la contraseña.");
    }
}