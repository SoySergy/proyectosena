export function checkAuth() {
    const token = localStorage.getItem("token");

    // ❌ Si no hay token → fuera
    if (!token) {
        window.location.href = "/pages/auth/login.html";
        return;
    }

    // 🔍 (Opcional pero recomendado) validar expiración del JWT
    try {
        const payload = JSON.parse(atob(token.split(".")[1]));

        const exp = payload.exp * 1000; // convertir a ms
        const now = Date.now();

        if (now > exp) {
            // token expirado
            localStorage.removeItem("token");
            localStorage.removeItem("user");

            window.location.href = "/pages/auth/login.html";
        }

    } catch (error) {
        // token inválido
        localStorage.removeItem("token");
        localStorage.removeItem("user");

        window.location.href = "/pages/auth/login.html";
    }
}