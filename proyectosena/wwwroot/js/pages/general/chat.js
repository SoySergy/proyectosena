import { checkAuth } from "../../utils/authGuard.js";
import { API_BASE, authHeaders, fetchConSesion, leerCuerpo, mensajeDeError } from "../../services/api.js";
import { escapeHtml } from "../../utils/html.js";
import { initNotificaciones } from "../../utils/notificaciones.js";
import { initUserMenu } from "../../utils/userMenu.js";

// /js/pages/general/chat.js
// ============================================================
// INICIALIZACIÓN
// ============================================================

checkAuth();

const user = initUserMenu();
initNotificaciones();

// Qué solicitud es viene por la URL: esta pantalla se abre desde la tarjeta
// de una solicitud concreta, en cualquiera de los paneles.
const idRequest = new URLSearchParams(window.location.search).get("idRequest");

document.getElementById("backBtn").addEventListener("click", () => history.back());

const loadingEl = document.getElementById("chat-loading");
const messagesEl = document.getElementById("chat-messages");
const formEl = document.getElementById("chatForm");
const messageEl = document.getElementById("chat-message");
const subtitleEl = document.getElementById("chat-subtitle");
const inputEl = document.getElementById("chatInput");
const sendBtn = document.getElementById("chatSendBtn");

const INTERVALO_SONDEO_MS = 5000;
let temporizador = null;
let ultimoConteo = -1;

function mostrarError(texto) {
    messageEl.textContent = texto;
    messageEl.className = "form-hint form-hint--error";
}

function limpiarError() {
    messageEl.textContent = "";
    messageEl.className = "form-hint";
}

if (!idRequest) {
    loadingEl.style.display = "none";
    mostrarError("No se indicó ninguna solicitud para abrir su chat.");
} else {
    subtitleEl.textContent = "Los mensajes solo los ve quien pidió la recolección y quien la tiene asignada.";
    iniciar();
}

async function iniciar() {
    await cargarMensajes(true);
    // Sin WebSocket en el backend: un sondeo cada 5s es lo más simple que
    // funciona para que el otro lado vea los mensajes sin recargar la página.
    temporizador = setInterval(() => cargarMensajes(false), INTERVALO_SONDEO_MS);
}

function detenerSondeo() {
    if (temporizador) {
        clearInterval(temporizador);
        temporizador = null;
    }
}

function formatFechaHora(iso) {
    return new Date(iso).toLocaleString("es-CO", {
        day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit"
    });
}

function traducirRol(rol) {
    if (rol === "Manager") return "Gestor";
    if (rol === "Citizen") return "Ciudadano";
    return rol;
}

async function cargarMensajes(forzarRepintado) {
    try {
        const res = await fetchConSesion(
            `${API_BASE}/ChatHistory/GetMessagesByRequest?idRequest=${encodeURIComponent(idRequest)}`,
            { headers: authHeaders() }
        );

        if (!res.ok) {
            const body = await leerCuerpo(res);
            throw new Error(mensajeDeError(body, "No se pudo cargar el chat."));
        }

        const mensajes = await res.json();

        loadingEl.style.display = "none";
        messagesEl.hidden = false;
        formEl.hidden = false;

        // El sondeo repinta solo si algo cambió: si no, cada 5s se perdería
        // el scroll de quien está leyendo hacia arriba, o lo que esté
        // escribiendo a medias en el cuadro de texto.
        if (forzarRepintado || mensajes.length !== ultimoConteo) {
            renderMensajes(mensajes);
            ultimoConteo = mensajes.length;
        }

        marcarComoLeidos(mensajes);

    } catch (error) {
        loadingEl.style.display = "none";
        detenerSondeo();
        formEl.hidden = true;
        mostrarError(error.message || "No se pudo cargar el chat.");
    }
}

function renderMensajes(mensajes) {
    if (!mensajes.length) {
        messagesEl.innerHTML = "<p class='empty-msg'>Todavía no hay mensajes. Escribe el primero.</p>";
        return;
    }

    messagesEl.innerHTML = mensajes.map(m => {
        const esMio = m.idSender === user.idUser;
        const autor = esMio
            ? ""
            : `<span class="chat-bubble__autor">${escapeHtml(m.senderName)} · ${escapeHtml(traducirRol(m.senderRole))}</span>`;

        return `
            <div class="chat-bubble ${esMio ? "chat-bubble--mio" : "chat-bubble--otro"}">
                ${autor}
                <p class="chat-bubble__texto">${escapeHtml(m.message)}</p>
                <span class="chat-bubble__hora">${formatFechaHora(m.sendDate)}</span>
            </div>
        `;
    }).join("");

    messagesEl.scrollTop = messagesEl.scrollHeight;
}

/**
 * Marca como leídos los mensajes ajenos que llegaron sin leer. Verlos en
 * esta pantalla ya cuenta como leído, igual que en cualquier chat: no hace
 * falta que la otra persona haga nada más.
 */
async function marcarComoLeidos(mensajes) {
    const pendientes = mensajes.filter(m => !m.isRead && m.idSender !== user.idUser);

    for (const m of pendientes) {
        try {
            await fetchConSesion(
                `${API_BASE}/ChatHistory/MarkAsRead?idChatHistory=${m.idChatHistory}`,
                { method: "PUT", headers: authHeaders() }
            );
        } catch {
            // Un fallo aquí no es grave: se reintenta solo en el próximo sondeo.
        }
    }
}

formEl.addEventListener("submit", async (e) => {
    e.preventDefault();

    const texto = inputEl.value.trim();
    if (!texto) return;

    sendBtn.disabled = true;
    limpiarError();

    try {
        const res = await fetchConSesion(`${API_BASE}/ChatHistory/SendMessage`, {
            method: "POST",
            headers: authHeaders(),
            body: JSON.stringify({ idRequest, message: texto })
        });

        const body = await leerCuerpo(res);

        if (!res.ok) {
            throw new Error(mensajeDeError(body, "No se pudo enviar el mensaje."));
        }

        inputEl.value = "";
        await cargarMensajes(true);

    } catch (error) {
        mostrarError(error.message || "No se pudo enviar el mensaje.");
    } finally {
        sendBtn.disabled = false;
        inputEl.focus();
    }
});
