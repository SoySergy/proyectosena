/**
 * tabs.js
 * Cambio de sección en los paneles: marca el botón pulsado, muestra su
 * sección y avisa a quien quiera cargar datos en ese momento.
 *
 * Estaba copiado en el panel del ciudadano y en el del gestor, y en los dos
 * el aviso era una escalera de «if (destino === "x") cargarX()». Aquí se pasa
 * un objeto: añadir una sección nueva es añadir una entrada, no tocar esta
 * función.
 */

/**
 * @param {Object<string, Function>} alEntrar - qué hacer al abrir cada
 *   sección, con el mismo nombre que su data-section. Las secciones que no
 *   necesitan cargar nada simplemente no aparecen.
 */
export function initTabs(alEntrar = {}) {
    const botones = document.querySelectorAll(".nav-btn");
    const secciones = document.querySelectorAll(".section");

    botones.forEach((boton) => {
        boton.addEventListener("click", () => {
            const destino = boton.dataset.section;

            botones.forEach((b) => b.classList.remove("active"));
            secciones.forEach((s) => s.classList.remove("active"));

            boton.classList.add("active");
            document.getElementById(destino)?.classList.add("active");

            alEntrar[destino]?.();
        });
    });
}
