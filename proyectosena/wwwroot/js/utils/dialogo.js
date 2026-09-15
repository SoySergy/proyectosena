/**
 * dialogo.js
 * Abre y cierra los modales de los paneles de forma que se puedan usar con el
 * teclado y que un lector de pantalla los anuncie como diálogo (M6).
 *
 * El marcado lo pone cada página en el contenedor del modal: role="dialog",
 * aria-modal="true" y aria-labelledby apuntando a su título. Este módulo se
 * ocupa del comportamiento:
 *   · al abrir, el foco entra al diálogo y Tab no se escapa a la página de atrás;
 *   · Escape lo cierra, igual que su botón y el fondo;
 *   · al cerrar, el foco vuelve al botón que lo abrió.
 *
 * Antes cada panel lo mostraba y lo ocultaba a mano con style.display: con el
 * teclado se seguía tabulando por la página tapada, y Escape no hacía nada.
 */

const ENFOCABLES = [
    "a[href]",
    "button:not([disabled])",
    "input:not([disabled]):not([type='hidden'])",
    "select:not([disabled])",
    "textarea:not([disabled])",
    "[tabindex]:not([tabindex='-1'])",
].join(",");

/**
 * Prepara un modal que ya está en la página y devuelve { abrir, cerrar }.
 * Se muestran con los mismos valores de display que ya usaban los paneles.
 */
export function crearDialogo(modal, fondo) {
    let disparador = null;

    /** Muestra el modal y lleva el foco a primerCampo, o al primer control que haya. */
    function abrir(primerCampo) {
        disparador = document.activeElement;

        modal.style.display = "flex";
        fondo.style.display = "block";
        document.addEventListener("keydown", alTeclear);

        (primerCampo ?? enfocables()[0])?.focus();
    }

    function cerrar() {
        modal.style.display = "none";
        fondo.style.display = "none";
        document.removeEventListener("keydown", alTeclear);

        // Si la lista se repintó mientras estaba abierto, ese botón ya no existe
        if (disparador?.isConnected) disparador.focus();
        disparador = null;
    }

    function alTeclear(evento) {
        if (evento.key === "Escape") {
            evento.preventDefault();
            cerrar();
            return;
        }

        if (evento.key === "Tab") retenerFoco(evento);
    }

    // Tab desde el último control vuelve al primero, y Mayús+Tab desde el
    // primero va al último: el foco no sale a la página que queda detrás
    function retenerFoco(evento) {
        const lista = enfocables();
        if (lista.length === 0) return;

        const primero = lista[0];
        const ultimo = lista[lista.length - 1];
        const actual = document.activeElement;

        if (!modal.contains(actual)) {
            evento.preventDefault();
            primero.focus();
        } else if (evento.shiftKey && actual === primero) {
            evento.preventDefault();
            ultimo.focus();
        } else if (!evento.shiftKey && actual === ultimo) {
            evento.preventDefault();
            primero.focus();
        }
    }

    // Solo los que se ven: un control oculto no puede recibir el foco
    function enfocables() {
        return [...modal.querySelectorAll(ENFOCABLES)].filter((el) => el.getClientRects().length > 0);
    }

    return { abrir, cerrar };
}
