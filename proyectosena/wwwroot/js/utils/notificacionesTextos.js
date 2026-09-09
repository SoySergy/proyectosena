/**
 * notificacionesTextos.js
 * Pasa a español lo que el backend escribe en inglés.
 *
 * Los avisos se guardan en la base de datos con el texto ya escrito, y ese
 * texto está en inglés en tres archivos del servidor. Traducir aquí, y no
 * allá, tiene una ventaja concreta: los avisos que YA existen —treinta y
 * tres solo de una ciudadana— también salen en español, cosa que cambiar
 * el backend no conseguiría porque ya están guardados.
 *
 * Regla de oro: si un texto no está en las tablas, se muestra tal cual
 * llegó. Nunca se pierde información por no saber traducirla.
 */

// ── Títulos ───────────────────────────────────────────────────
const TITULOS = {
    "Status Updated": "Estado actualizado",
    "Request Assigned": "Solicitud asignada",
    "Request Assigned To You": "Te asignaron una solicitud",
    "Request Accepted": "Solicitud aceptada",
    "Request Rejected": "Solicitud rechazada",
    "Request Cancelled": "Solicitud cancelada",
    "Collection In Progress": "Recolección en camino",
    "Collection Completed": "Recolección completada",
    "New Collection Request Available": "Nueva solicitud disponible",
    "Manager Application Approved": "Solicitud de gestor aprobada",
    "Manager Application Rejected": "Solicitud de gestor rechazada",
};

// ── Mensajes que siempre dicen lo mismo ───────────────────────
const MENSAJES = {
    "The status of your request has been updated.":
        "El estado de tu solicitud cambió.",
    "A manager has been assigned to your collection request.":
        "Se asignó un gestor a tu solicitud de recolección.",
    "A manager has accepted your collection request and will be in touch soon.":
        "Un gestor aceptó tu solicitud y se pondrá en contacto pronto.",
    "The manager is on the way to collect your waste.":
        "El gestor va en camino a recoger tus residuos.",
    "Your waste has been successfully collected. Thank you!":
        "Se recogieron tus residuos. ¡Gracias!",
    "Unfortunately your request could not be processed. Please create a new one.":
        "No se pudo procesar tu solicitud. Puedes crear una nueva.",
    "Your collection request was cancelled.":
        "Se canceló tu solicitud de recolección.",
    "Your application to become a manager was approved. Sign in again to see your new options.":
        "Tu solicitud para ser gestor fue aprobada. Vuelve a iniciar sesión para ver tus nuevas opciones.",
};

// ── Mensajes con un trozo que cambia ──────────────────────────
// El {0} es la parte que pone el servidor: una dirección, un motivo. Se
// respeta tal cual y solo se traduce lo que la rodea.
const PATRONES = [
    {
        en: "Your application to become a manager was not approved. Reason: {0}",
        es: "Tu solicitud para ser gestor no fue aprobada. Motivo: {0}",
    },
    {
        en: "An administrator assigned you a collection request at: {0}.",
        es: "Un administrador te asignó una solicitud en: {0}.",
    },
    {
        en: "A new collection request is available at: {0}. Be the first to accept it!",
        es: "Hay una nueva solicitud disponible en: {0}. ¡Sé el primero en aceptarla!",
    },
];

/** Escapa lo que en una expresión regular significaría otra cosa. */
function escaparRegex(texto) {
    return texto.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

// Cada patrón se convierte una sola vez en su expresión regular, no en
// cada aviso que se pinta.
const PATRONES_COMPILADOS = PATRONES.map(({ en, es }) => ({
    regex: new RegExp("^" + escaparRegex(en).replace("\\{0\\}", "([\\s\\S]*)") + "$"),
    es,
}));

export function traducirTitulo(titulo) {
    return TITULOS[titulo] ?? titulo ?? "";
}

export function traducirMensaje(mensaje) {
    if (!mensaje) return "";

    const exacto = MENSAJES[mensaje];
    if (exacto) return exacto;

    for (const { regex, es } of PATRONES_COMPILADOS) {
        const coincidencia = mensaje.match(regex);
        if (coincidencia) return es.replace("{0}", coincidencia[1]);
    }

    // Sin traducción conocida: mejor en inglés que en blanco.
    return mensaje;
}
