export const API_BASE = "http://localhost:8080/api";

// ── Cabeceras de las peticiones con sesión ────────────────────────

/**
 * Cabeceras para pedir algo que exige haber iniciado sesión.
 *
 * El token se lee aquí dentro y no se recibe por parámetro: así ninguna
 * pantalla tiene que acordarse de sacarlo de localStorage ni de escribir
 * bien la palabra "Bearer". Estaba copiado igual en el panel del ciudadano
 * y en el del gestor; con el del administrador iban a ser tres.
 */
export function authHeaders() {
    return {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${localStorage.getItem("token")}`
    };
}

// ── Lectura de las respuestas del backend ─────────────────────────
//
// El backend contesta de tres formas distintas y hay que entenderlas todas:
//
//   · texto plano        errores de negocio ("Código inválido o expirado.")
//                        y el 429 del límite de peticiones
//   · {"message": ...}   algunos errores propios
//   · ProblemDetails     validación y 500, que traen "title" y "errors",
//                        nunca "message"
//
// Leer la respuesta con res.json() a secas perdía el texto plano, y leer
// solo .message perdía ProblemDetails. Por eso un «Demasiados intentos» del
// servidor llegaba a la pantalla como «Error al enviar el código», y un 500
// se mostraba como «[object Object]».

/** Devuelve el cuerpo ya interpretado: objeto si es JSON, texto si no. */
export async function leerCuerpo(response) {
    const texto = await response.text();

    try {
        return JSON.parse(texto);
    } catch {
        return texto;
    }
}

/** Saca el mejor mensaje que traiga el cuerpo, venga en el formato que venga. */
export function mensajeDeError(cuerpo, porDefecto) {
    if (typeof cuerpo === "string" && cuerpo.trim()) return cuerpo.trim();

    if (cuerpo?.message) return cuerpo.message;

    // ProblemDetails de validación: {"errors": {"Campo": ["lo que pasa"]}}
    if (cuerpo?.errors) {
        const primero = Object.values(cuerpo.errors).flat()[0];
        if (primero) return primero;
    }

    if (cuerpo?.title) return cuerpo.title;

    return porDefecto;
}
