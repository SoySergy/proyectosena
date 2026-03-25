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

    if (!response.ok) {
        throw new Error(result);
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