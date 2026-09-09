/**
 * format.js
 * Cómo se muestran los datos dentro de las tarjetas: el iconito que va
 * delante de cada dato y las fechas en español.
 *
 * Estaban copiados en el panel del ciudadano y en el del gestor. Con el
 * del administrador iban a ser tres.
 */

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
