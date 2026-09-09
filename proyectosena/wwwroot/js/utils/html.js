// ============================================================
//  html.js — utilidades para pintar texto sin abrir agujeros
// ============================================================

/**
 * Convierte a texto seguro cualquier valor que venga del servidor antes de
 * meterlo en una plantilla que termina en innerHTML.
 *
 * Hacía falta porque las tarjetas de los paneles interpolaban directamente la
 * dirección, los residuos y las observaciones que escribe el ciudadano. Se
 * comprobó: una dirección con `<img src=x onerror="...">` ejecutaba ese código
 * en la pantalla del gestor al abrir sus solicitudes.
 *
 * El & va primero a propósito: si se reemplazara al final, volvería a escapar
 * los & que acaban de generar los otros reemplazos.
 */
export function escapeHtml(valor) {
    if (valor === null || valor === undefined) return "";

    return String(valor)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");
}
