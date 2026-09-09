import { API_BASE, authHeaders } from "../services/api.js";
import { escapeHtml } from "./html.js";
import { formatDate } from "./format.js";
import { traducirTitulo, traducirMensaje } from "./notificacionesTextos.js";

/**
 * notificaciones.js
 * La campana de la cabecera: cuántas hay sin leer y qué dicen.
 *
 * Va aparte de userMenu.js aunque compartan cabecera: aquel se ocupa de
 * quién eres y de cerrar sesión, y este de los avisos. Son dos cosas y
 * cada una cambia por sus propios motivos.
 *
 * El endpoint solo pide haber iniciado sesión, sin importar el rol, así que
 * el mismo módulo sirve para el ciudadano, el gestor y el administrador.
 */

// Más de esto se muestra como "99+": el numerito vive encima de la campana
// y con tres cifras se sale del círculo.
const MAXIMO_VISIBLE = 99;

// Cuántas se piden al abrir el panel. Con treinta y tres ya hay que hacer
// scroll; traerlas todas solo llenaría de avisos viejos que nadie mira.
const CUANTAS_MOSTRAR = 20;

// Punto de color según lo que manda el servidor en "type".
const COLOR_POR_TIPO = {
    Success: "notif-item__punto--success",
    Warning: "notif-item__punto--warning",
};

/**
 * Enciende la campana de la cabecera.
 * La página necesita #notifTrigger, #notifCount, #notifPanel y #notifList.
 */
export async function initNotificaciones() {
    conectarPanel();
    await actualizarContador();
}

function conectarPanel() {
    const boton = document.getElementById("notifTrigger");
    const panel = document.getElementById("notifPanel");

    if (!boton || !panel) return;

    boton.addEventListener("click", (e) => {
        e.stopPropagation();
        const abierto = panel.classList.toggle("is-open");
        boton.setAttribute("aria-expanded", abierto);

        if (abierto) {
            // Los dos desplegables de la cabecera caen en el mismo sitio y se
            // tapaban: como cada uno frena la propagación de su clic, el otro
            // nunca se enteraba de que debía cerrarse.
            cerrarMenuDeUsuario();

            // La lista se pide al abrir y no al cargar la página: así se ve lo
            // que hay en ese momento y no lo que había al entrar.
            cargarLista();
        }
    });

    // Cerrar al hacer clic fuera, igual que el menú de usuario
    document.addEventListener("click", () => {
        panel.classList.remove("is-open");
        boton.setAttribute("aria-expanded", "false");
    });

    // Los clics de dentro no lo cierran
    panel.addEventListener("click", (e) => e.stopPropagation());
}

function cerrarMenuDeUsuario() {
    const menu = document.getElementById("userDropdown");
    const trigger = document.getElementById("userMenuTrigger");

    if (menu) menu.classList.remove("is-open");
    if (trigger) trigger.setAttribute("aria-expanded", "false");
}

async function cargarLista() {
    const lista = document.getElementById("notifList");
    if (!lista) return;

    lista.innerHTML = `<p class="notificaciones__vacio">Cargando...</p>`;

    try {
        const respuesta = await fetch(
            `${API_BASE}/Notification/GetMyNotifications?pageSize=${CUANTAS_MOSTRAR}`,
            { headers: authHeaders() }
        );

        if (!respuesta.ok) {
            lista.innerHTML = `<p class="notificaciones__vacio">No se pudieron cargar los avisos.</p>`;
            return;
        }

        const { items } = await respuesta.json();

        if (!Array.isArray(items) || items.length === 0) {
            lista.innerHTML = `<p class="notificaciones__vacio">No tienes avisos.</p>`;
            return;
        }

        lista.innerHTML = items.map(pintarAviso).join("");
    } catch {
        lista.innerHTML = `<p class="notificaciones__vacio">No se pudo conectar con el servidor.</p>`;
    }
}

function pintarAviso(aviso) {
    // El título y el mensaje llegan del servidor en inglés y se traducen
    // aquí; el motivo de un rechazo lo escribe una persona, así que todo
    // pasa por escapeHtml antes de entrar en la plantilla.
    const titulo = escapeHtml(traducirTitulo(aviso.title));
    const mensaje = escapeHtml(traducirMensaje(aviso.message));
    const punto = COLOR_POR_TIPO[aviso.type] ?? "";
    const sinLeer = aviso.isRead ? "" : " notif-item--sin-leer";

    return `
        <div class="notif-item${sinLeer}">
            <span class="notif-item__punto ${punto}"></span>
            <div class="notif-item__cuerpo">
                <p class="notif-item__titulo">${titulo}</p>
                <p class="notif-item__mensaje">${mensaje}</p>
                <span class="notif-item__fecha">${formatDate(aviso.creationDate)}</span>
            </div>
        </div>`;
}

/** Pregunta cuántas quedan sin leer y repinta el numerito. */
async function actualizarContador() {
    const contador = document.getElementById("notifCount");
    if (!contador) return;

    try {
        const respuesta = await fetch(`${API_BASE}/Notification/GetUnreadCount`, {
            headers: authHeaders(),
        });

        // Si la sesión caducó o el servidor falla, la campana se queda sin
        // numerito y ya está: un aviso rojo en la cabecera por no poder
        // contar notificaciones sería más molesto que útil.
        if (!respuesta.ok) {
            ocultar(contador);
            return;
        }

        const { unreadCount } = await respuesta.json();

        if (typeof unreadCount === "number" && unreadCount > 0) {
            mostrar(contador, unreadCount);
        } else {
            ocultar(contador);
        }
    } catch {
        ocultar(contador);
    }
}

function mostrar(contador, cantidad) {
    contador.textContent = cantidad > MAXIMO_VISIBLE ? `${MAXIMO_VISIBLE}+` : String(cantidad);
    contador.hidden = false;

    // Para quien navega con lector de pantalla, el botón entero dice cuántas
    // hay; el numerito suelto no significaría nada.
    const boton = document.getElementById("notifTrigger");
    if (boton) {
        boton.setAttribute("aria-label", `Notificaciones: ${cantidad} sin leer`);
    }
}

function ocultar(contador) {
    contador.hidden = true;
    contador.textContent = "";

    const boton = document.getElementById("notifTrigger");
    if (boton) boton.setAttribute("aria-label", "Notificaciones");
}
