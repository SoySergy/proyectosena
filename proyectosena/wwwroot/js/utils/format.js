/**
 * format.js
 * Cómo se muestran los datos dentro de las tarjetas: el iconito que va
 * delante de cada dato, las fechas y horas, y la traducción de los estados
 * de una solicitud.
 *
 * Estaban copiados en el panel del ciudadano y en el del gestor. Con el
 * del administrador iban a ser tres (M10 · M11).
 */

import { escapeHtml } from "./html.js";

/**
 * Iconito para poner delante de un dato dentro de una tarjeta.
 *
 * El nombre sale del catálogo de clases .icon-* de components/form.css
 * (usuario, correo, telefono, tipodocumento, calendario, observacion...).
 * Se llama siempre con un nombre escrito a mano, nunca con algo que venga
 * del servidor: aquí no se escapa nada.
 */
export function icon(nombre) {
    return `<span class="card-icon icon-${nombre}" aria-hidden="true"></span>`;
}

/**
 * Fecha en español de Colombia.
 *
 * El mes va corto ("7 sept 2026") o largo ("7 de septiembre de 2026") según
 * se pida. Se deja elegir a propósito: el panel del ciudadano usa el largo y
 * el del gestor el corto, y unificarlos cambiaría el aspecto de una de las
 * dos pantallas, que no es decisión de este archivo.
 */
export function formatDate(iso, { mesLargo = false } = {}) {
    if (!iso) return "—";

    return new Date(iso).toLocaleDateString("es-CO", {
        year: "numeric",
        month: mesLargo ? "long" : "short",
        day: "numeric",
    });
}

/** Solo la parte de hora de un string HH:mm o HH:mm:ss. */
export function formatTime(timeString) {
    if (!timeString) return "—";
    return timeString.substring(0, 5);
}

// ── Estado de una solicitud (CollectionRequestStatus) ─────────────────
//
// El orden y las etiquetas viven en un solo sitio: antes cada panel tenía su
// propia copia del mapa, y quien agregara un estado en el backend
// (CollectionRequestController.cs, ValidStatuses) tenía que acordarse de
// tocar hasta cuatro sitios de frontend para que se viera bien en todos.

export const ESTADOS_SOLICITUD = ["Pending", "Assigned", "InProgress", "Completed", "Rejected"];

const ETIQUETA_POR_ESTADO = {
    Pending: "Pendiente",
    Assigned: "Asignado",
    InProgress: "En progreso",
    Completed: "Completado",
    Rejected: "Rechazado",
};

// Las dos reciben el estado tal como llega del servidor y su resultado va
// directo a innerHTML: un estado que no esté en el mapa se escapa, igual que
// cualquier otro dato del servidor.

/** Traduce un estado del backend a español para mostrarlo. */
export function translateStatus(status) {
    return ETIQUETA_POR_ESTADO[status] ?? escapeHtml(status);
}

/** Clase CSS del badge de estado ("status-pending", "status-none" si falta). */
export function statusClass(status) {
    return `status-${escapeHtml((status || "none").toLowerCase())}`;
}
