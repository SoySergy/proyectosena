// ── A qué dirección se le piden los datos ─────────────────────────
//
// En el ordenador, Docker levanta dos cosas separadas: Nginx sirve estas
// páginas en el puerto 8081 y la API escucha en el 8080. Ahí hace falta
// escribir la dirección entera, porque son dos sitios distintos.
//
// Al publicarlo, no: la propia API sirve estas páginas desde su carpeta
// wwwroot (Program.cs usa UseStaticFiles), así que la web y los datos
// viven en el mismo dominio. Una ruta relativa vale para cualquier
// dirección —la de Render, un dominio propio, el que sea— sin volver a
// tocar este archivo ni compilar nada distinto.
export const API_BASE = window.location.port === "8081"
    ? "http://localhost:8080/api"
    : "/api";

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

// ── Sesión anulada o vencida ──────────────────────────────────────

const LOGIN_URL = "/pages/auth/login.html";

/** Borra la sesión de este navegador y lleva al login con el motivo: "caducada" o "contrasena". */
export function volverAlLogin(motivo) {
    localStorage.removeItem("token");
    localStorage.removeItem("user");
    window.location.href = `${LOGIN_URL}?sesion=${motivo}`;
}

/**
 * fetch para peticiones con sesión. Un 401 con un token vigente por fecha es
 * una sesión que el servidor anuló (logout en otra pestaña, cambio de
 * contraseña, baja): todas las pantallas salen al login igual.
 */
export async function fetchConSesion(url, options = {}) {
    const respuesta = await fetch(url, options);

    if (respuesta.status === 401) {
        volverAlLogin("caducada");
        // Nunca se resuelve: la página ya se va y nadie debe pintar un error encima.
        return new Promise(() => {});
    }

    return respuesta;
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

/**
 * Consulta endpoints paginados iterando sobre todas las páginas existentes (pageSize=100)
 * para asegurar que el cliente obtenga el universo completo de registros antes de filtrar o renderizar.
 *
 * @param {string} mensajeError - lo que se muestra si el servidor no da un
 * mensaje propio. Antes cada pantalla escribía el suyo en su propio
 * `if (!res.ok) throw`; al pasar a esta función todas caían en el mismo
 * "Error al consultar los datos" genérico, así que aquí también se puede
 * decir qué se estaba pidiendo.
 */
export async function fetchAllItems(url, options = {}, mensajeError = "Error al consultar los datos") {
    const separator = url.includes("?") ? "&" : "?";
    let page = 1;
    let allItems = [];
    let totalPages = 1;

    do {
        const fullUrl = `${url}${separator}page=${page}&pageSize=100`;
        const res = await fetchConSesion(fullUrl, options);
        if (!res.ok) {
            const body = await leerCuerpo(res);
            throw new Error(mensajeDeError(body, mensajeError));
        }
        const data = await res.json();
        if (Array.isArray(data)) {
            return data;
        }
        if (data && Array.isArray(data.items)) {
            allItems = allItems.concat(data.items);
            totalPages = data.totalPages || 1;
        } else {
            break;
        }
        page++;
    } while (page <= totalPages);

    return allItems;
}
