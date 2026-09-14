import { volverAlLogin } from "../services/api.js";

export function checkAuth() {
    const token = localStorage.getItem("token");

    // ❌ Si no hay token → fuera
    if (!token) {
        window.location.href = "/login";
        return;
    }

    // 🔍 (Opcional pero recomendado) validar expiración del JWT
    try {
        const payload = JSON.parse(atob(token.split(".")[1]));

        const exp = payload.exp * 1000; // convertir a ms
        const now = Date.now();

        if (now > exp) {
            // token expirado
            volverAlLogin("caducada");
        }

    } catch (error) {
        // token inválido
        volverAlLogin("caducada");
    }
}