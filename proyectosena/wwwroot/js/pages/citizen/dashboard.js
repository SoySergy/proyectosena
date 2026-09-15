import { checkAuth } from "../../utils/authGuard.js";
import { requireRole, ROLES } from "../../utils/roleGuard.js";
import { API_BASE, fetchAllItems, fetchConSesion, mensajeDeError } from "../../services/api.js";
import { escapeHtml } from "../../utils/html.js";
import { initNotificaciones } from "../../utils/notificaciones.js";
import { initUserMenu } from "../../utils/userMenu.js";
import { initTabs } from "../../utils/tabs.js";
// /js/pages/citizen/dashboard.js
// ============================================================
// INICIALIZACIÓN
// ============================================================

// 🔐 Proteger acceso — redirige a login si no hay token válido
checkAuth();

// 🛡️ Verificar que el usuario sea Citizen (redirige si es Manager)
requireRole(ROLES.CITIZEN);

// Campana de avisos sin leer, la misma que en los otros paneles.
initNotificaciones();

// ============================================================
// CABECERA Y NAVEGACIÓN
// ============================================================

// El saludo, el correo del desplegable y el cierre de sesión son iguales en
// los tres paneles, así que viven en utils/userMenu.js. Devuelve el usuario
// ya leído de localStorage, que es el que usa el resto de esta pantalla.
//
// Antes esto estaba copiado aquí. Al centralizarlo se arreglan dos cosas de
// paso: el correo del desplegable, que se quedaba en «cargando...» porque
// nadie lo rellenaba, y el cierre de sesión, que ahora avisa al servidor
// para que el token quede anulado y no siga sirviendo una hora más.
const user = initUserMenu();
const token = localStorage.getItem("token");

initTabs({
    "mis-solicitudes": loadMyRequests,
    "historial": loadHistory,
    "postularme": cargarMiPostulacion,
});

// ============================================================
// UTILIDADES COMPARTIDAS
// ============================================================

/**
 * Construye los headers con el token JWT para todas las peticiones autenticadas
 */
function authHeaders() {
    return {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${token}`
    };
}

/**
 * Muestra un mensaje de éxito o error en un contenedor dado
 * @param {string} elementId - id del div donde se muestra el mensaje
 * @param {string} text - texto del mensaje
 * @param {"success"|"error"} type - tipo de mensaje
 */
function showMessage(elementId, text, type = "error") {
    const el = document.getElementById(elementId);
    if (!el) return;

    if (!text) {
        el.textContent = "";
        el.className = "";
        return;
    }

    el.textContent = text;
    el.className = `msg ${type}`;
    setTimeout(() => {
        el.textContent = "";
        el.className = "";
    }, 4000);
}

/**
 * Traduce los estados del backend a español para mostrar al usuario
 */
function translateStatus(status) {
    const map = {
        Pending: "Pendiente",
        Assigned: "Asignado",
        InProgress: "En progreso",
        Completed: "Completado",
        Rejected: "Rechazado"
    };
    return map[status] || status;
}

/**
 * Formatea una fecha ISO a formato local legible
 */
function formatDate(isoString) {
    if (!isoString) return "—";
    const date = new Date(isoString);
    return date.toLocaleDateString("es-CO", {
        year: "numeric", month: "long", day: "numeric"
    });
}

/**
 * Formatea solo la parte de hora de un string HH:mm o HH:mm:ss
 */
function formatTime(timeString) {
    if (!timeString) return "—";
    return timeString.substring(0, 5); // Retorna HH:mm
}

/** Genera un <span> con ícono SVG para usar dentro de tarjetas */
function icon(name) {
    return `<span class="card-icon icon-${name}" aria-hidden="true"></span>`;
}

// ============================================================
// SECCIÓN 1 — CREAR SOLICITUD
// ============================================================

const createForm = document.getElementById("createRequestForm");
const submitBtn = document.getElementById("submitRequestBtn");

createForm.addEventListener("submit", async (e) => {
    e.preventDefault();

    submitBtn.disabled = true;
    submitBtn.textContent = "Enviando...";
    showMessage("form-message", "");

    // Construir el DTO que espera el backend: CreateCollectionRequestDto
    const dto = {
        idUser: user.idUser,
        collectionDate: document.getElementById("collectionDate").value,
        collectionTime: document.getElementById("collectionTime").value,
        collectionAddress: document.getElementById("collectionAddress").value.trim(),
        contactPhone: document.getElementById("contactPhone").value.trim(),
        wasteTypes: document.getElementById("wasteTypes").value.trim(),
        citizenObservations: document.getElementById("citizenObservations").value.trim() || null
    };

    try {
        const response = await fetchConSesion(`${API_BASE}/collectionrequest/CreateCollectionRequest`, {
            method: "POST",
            headers: authHeaders(),
            body: JSON.stringify(dto)
        });

        const text = await response.text();
        let result;
        try { result = JSON.parse(text); } catch { result = text; }

        if (!response.ok) {
            throw new Error(mensajeDeError(result, "Error al crear la solicitud"));
        }

        // Éxito: limpiar formulario y mostrar confirmación
        showMessage("form-message", "✅ ¡Solicitud creada exitosamente! Los gestores han sido notificados.", "success");
        createForm.reset();

    } catch (error) {
        showMessage("form-message", error.message || "No se pudo crear la solicitud. Intenta de nuevo.", "error");
    } finally {
        submitBtn.disabled = false;
        submitBtn.textContent = "Crear Solicitud";
    }
});

// ============================================================
// SECCIÓN 2 — MIS SOLICITUDES ACTUALES
// ============================================================

document.getElementById("refreshRequestsBtn").addEventListener("click", loadMyRequests);

async function loadMyRequests() {
    const listEl = document.getElementById("requests-list");
    const loadingEl = document.getElementById("requests-loading");

    listEl.innerHTML = "";
    loadingEl.style.display = "block";
    showMessage("requests-message", "");

    try {
        const requests = await fetchAllItems(
            `${API_BASE}/collectionrequest/GetRequestsByUser`,
            { headers: authHeaders() },
            "Error al obtener solicitudes"
        );

        loadingEl.style.display = "none";

        const activeRequests = requests.filter(r =>
            r.currentStatus === "Pending" ||
            r.currentStatus === "Assigned" ||
            r.currentStatus === "InProgress"
        );

        if (!activeRequests.length) {
            listEl.innerHTML = "<p class='empty-msg'>No tienes solicitudes activas en este momento.</p>";
            return;
        }

        listEl.innerHTML = activeRequests.map(req => renderRequestCard(req)).join("");
        /////


        listEl.querySelectorAll(".edit-btn").forEach(btn => {
            btn.addEventListener("click", () => {
                //const req = requests.find(r => r.idRequest === btn.dataset.id);
                //if (req) openEditModal(req);
                const req = activeRequests.find(r => r.idRequest === btn.dataset.id);
                if (req) openEditModal(req);
            });
        });

        listEl.querySelectorAll(".cancel-btn").forEach(btn => {
            btn.addEventListener("click", () => cancelRequest(btn.dataset.id, btn));
        });

    } catch (error) {
        loadingEl.style.display = "none";
        showMessage("requests-message", "Error al cargar tus solicitudes.", "error");
    }
}

/**
 * Cancela una solicitud propia. El botón que la llama solo existe mientras
 * la solicitud sigue en Pending; el backend igual lo comprueba de nuevo
 * (RequestCancelResult.NotCancellable) por si llegó a tomarla un gestor
 * justo en este instante.
 */
async function cancelRequest(idRequest, btn) {
    if (!window.confirm("¿Seguro que quieres cancelar esta solicitud? No se puede deshacer.")) {
        return;
    }

    btn.disabled = true;
    btn.textContent = "Cancelando...";

    try {
        const response = await fetchConSesion(
            `${API_BASE}/collectionrequest/CancelRequest?idRequest=${idRequest}`,
            { method: "PATCH", headers: authHeaders() }
        );

        // CancelRequest devuelve texto plano en sus errores (403, 404, 409),
        // no JSON, así que se lee igual que en el resto de este archivo.
        const text = await response.text();
        let result;
        try { result = JSON.parse(text); } catch { result = text; }

        if (!response.ok) {
            throw new Error(mensajeDeError(result, "No se pudo cancelar la solicitud."));
        }

        showMessage("requests-message", "Solicitud cancelada.", "success");
        setTimeout(loadMyRequests, 1500);

    } catch (error) {
        showMessage("requests-message", error.message || "No se pudo cancelar la solicitud.", "error");
        btn.disabled = false;
        btn.textContent = "Cancelar";
    }
}

/**
 * Genera el HTML de una tarjeta de solicitud
 */
function renderRequestCard(req) {
    const isPending = req.currentStatus === "Pending";
    const editBtn = isPending
        ? `<button class="edit-btn" data-id="${escapeHtml(req.idRequest)}">${icon("lapiz")} Editar</button>`
        : `<button class="edit-btn" disabled title="Solo se pueden editar solicitudes pendientes">${icon("lapiz")} Editar</button>`;

    // Cancelar solo es posible desde Pending —lo decide la misma máquina de
    // estados del backend que decide si se puede editar—, así que comparte
    // la condición con editBtn.
    //
    // Sin clase de estilo propia a propósito: no hay ninguna definida para
    // esto en el CSS del panel. Se ve con el botón por defecto del navegador
    // hasta que se le dé una.
    const cancelBtn = isPending
        ? `<button class="cancel-btn" data-id="${escapeHtml(req.idRequest)}">Cancelar</button>`
        : `<button class="cancel-btn" disabled title="Solo se pueden cancelar solicitudes pendientes">Cancelar</button>`;

    // El chat exige un gestor asignado (lo comprueba IsParticipant en el
    // backend): mientras la solicitud esté Pending no hay con quién hablar.
    const chatBtn = !isPending
        ? `<a class="btn btn-secondary" href="/chat?idRequest=${encodeURIComponent(req.idRequest)}">${icon("mensaje")} Chat</a>`
        : "";

    return `
        <div class="request-card">
            <div class="card-header">
                <span class="status-badge status-${escapeHtml(req.currentStatus.toLowerCase())}">${translateStatus(req.currentStatus)}</span>
                <span class="card-date">Creada: ${formatDate(req.requestDate)}</span>
            </div>
            <div class="card-body">
                <p>${icon("calendario")}<strong>Fecha recolección:</strong> ${formatDate(req.collectionDate)}</p>
                <p>${icon("reloj")}<strong>Hora:</strong> ${formatTime(req.collectionTime)}</p>
                <p>${icon("direccion")}<strong>Dirección:</strong> ${escapeHtml(req.collectionAddress)}</p>
                <p>${icon("telefono")}<strong>Teléfono:</strong> ${escapeHtml(req.contactPhone)}</p>
                <p>${icon("reciclaje")}<strong>Residuos:</strong> ${escapeHtml(req.wasteTypes)}</p>
                ${req.citizenObservations ? `<p>${icon("observacion")}<strong>Observaciones:</strong> ${escapeHtml(req.citizenObservations)}</p>` : ""}
            </div>
            <div class="card-actions">
                ${chatBtn}
                ${editBtn}
                ${cancelBtn}
            </div>
        </div>
    `;
}

// ============================================================
// MODAL — EDITAR SOLICITUD
// ============================================================

const editModal = document.getElementById("editModal");
const modalOverlay = document.getElementById("modalOverlay");

/**
 * Abre el modal y pre-rellena los campos con los datos actuales de la solicitud
 */
function openEditModal(req) {
    document.getElementById("editIdRequest").value = req.idRequest;

    // Formatear la fecha para el input type="date" (YYYY-MM-DD)
    const dateStr = req.collectionDate ? req.collectionDate.split("T")[0] : "";
    document.getElementById("editCollectionDate").value = dateStr;

    // Hora — tomar solo HH:mm
    document.getElementById("editCollectionTime").value = formatTime(req.collectionTime);
    document.getElementById("editCollectionAddress").value = req.collectionAddress;
    document.getElementById("editContactPhone").value = req.contactPhone;
    document.getElementById("editWasteTypes").value = req.wasteTypes;
    document.getElementById("editCitizenObservations").value = req.citizenObservations || "";

    showMessage("edit-message", "");

    editModal.style.display = "flex";
    modalOverlay.style.display = "block";
}

function closeEditModal() {
    editModal.style.display = "none";
    modalOverlay.style.display = "none";
}

document.getElementById("closeModalBtn").addEventListener("click", closeEditModal);
document.getElementById("cancelEditBtn").addEventListener("click", closeEditModal);
modalOverlay.addEventListener("click", closeEditModal);

// Submit del formulario de edición
document.getElementById("editRequestForm").addEventListener("submit", async (e) => {
    e.preventDefault();

    const saveBtn = document.getElementById("saveEditBtn");
    saveBtn.disabled = true;
    saveBtn.textContent = "Guardando...";
    showMessage("edit-message", "");

    // Construir DTO: UpdateCollectionRequestDto
    // Solo se envían campos con valor (todos son opcionales excepto idRequest)
    const dto = {
        idRequest: document.getElementById("editIdRequest").value
    };

    const date = document.getElementById("editCollectionDate").value;
    const time = document.getElementById("editCollectionTime").value;
    const address = document.getElementById("editCollectionAddress").value.trim();
    const phone = document.getElementById("editContactPhone").value.trim();
    const waste = document.getElementById("editWasteTypes").value.trim();
    const obs = document.getElementById("editCitizenObservations").value.trim();

    if (date) dto.collectionDate = date;
    if (time) dto.collectionTime = time;
    if (address) dto.collectionAddress = address;
    if (phone) dto.contactPhone = phone;
    if (waste) dto.wasteTypes = waste;
    if (obs) dto.citizenObservations = obs;

    try {
        const response = await fetchConSesion(`${API_BASE}/collectionrequest/UpdateCollectionRequest`, {
            method: "PUT",
            headers: authHeaders(),
            body: JSON.stringify(dto)
        });

        const text = await response.text();
        let result;
        try { result = JSON.parse(text); } catch { result = text; }

        if (!response.ok) {
            throw new Error(mensajeDeError(result, "Error al actualizar la solicitud"));
        }

        showMessage("edit-message", "✅ Solicitud actualizada correctamente.", "success");

        // Cerrar modal y refrescar lista después de 1.5s
        setTimeout(() => {
            closeEditModal();
            loadMyRequests();
        }, 1500);

    } catch (error) {
        showMessage("edit-message", error.message || "No se pudo actualizar la solicitud.", "error");
    } finally {
        saveBtn.disabled = false;
        saveBtn.textContent = "Guardar cambios";
    }
});

// ============================================================
// SECCIÓN 3 — HISTORIAL DE RECOLECCIONES
// ============================================================

document.getElementById("refreshHistoryBtn").addEventListener("click", loadHistory);

async function loadHistory() {
    const listEl = document.getElementById("history-list");
    const loadingEl = document.getElementById("history-loading");

    listEl.innerHTML = "";
    loadingEl.style.display = "block";
    showMessage("history-message", "");

    try {
        const histories = await fetchAllItems(
            `${API_BASE}/history/GetMyHistory`,
            { headers: authHeaders() },
            "Error al obtener el historial"
        );

        loadingEl.style.display = "none";

        if (!histories || histories.length === 0) {
            listEl.innerHTML = "<p class='empty-msg'>No tienes historial de recolecciones aún.</p>";
            return;
        }

        // Ordenar del más reciente al más antiguo
        histories.sort((a, b) => new Date(b.changeDate) - new Date(a.changeDate));

        listEl.innerHTML = histories.map(h => renderHistoryRow(h)).join("");

    } catch (error) {
        loadingEl.style.display = "none";
        showMessage("history-message", "Error al cargar el historial. Verifica tu conexión.", "error");
    }
}

/**
 * Genera el HTML de una fila del historial
 */
function renderHistoryRow(h) {
    const prev = h.previousStatus ? translateStatus(h.previousStatus) : "—";
    const next = translateStatus(h.newStatus);

    return `
        <div class="history-row">
            <div class="history-date">${formatDate(h.changeDate)}</div>
            <div class="history-change">
                <span class="status-badge status-${(h.previousStatus || "none").toLowerCase()}">${prev}</span>
                <span class="history-arrow">→</span>
                <span class="status-badge status-${escapeHtml(h.newStatus.toLowerCase())}">${next}</span>
            </div>
            <div class="history-meta">
                <span>Gestionado por: <strong>${escapeHtml(h.userName || "Sistema")}</strong></span>
                ${h.comment ? `<span class="history-comment">💬 ${escapeHtml(h.comment)}</span>` : ""}
            </div>
        </div>
    `;
}

// ============================================================
// SECCIÓN 4 — POSTULARME A GESTOR
// ============================================================

const applyForm = document.getElementById("applyForm");
const applyStatusEl = document.getElementById("apply-status");

/**
 * Consulta en qué va la última postulación de este ciudadano y decide si el
 * formulario para enviar una nueva se muestra: no tiene sentido dejar
 * mandar otra mientras hay una en revisión, ni si ya se la aprobaron —el
 * backend rechaza los dos casos con 409, esto es solo comodidad.
 */
async function cargarMiPostulacion() {
    const loadingEl = document.getElementById("apply-loading");

    showMessage("apply-message", "");
    applyStatusEl.innerHTML = "";
    loadingEl.style.display = "block";

    try {
        const response = await fetchConSesion(`${API_BASE}/ManagerApplication/GetMyApplication`, {
            headers: authHeaders()
        });

        loadingEl.style.display = "none";

        // 404 no es un error aquí: significa que nunca ha postulado, y el
        // formulario vacío es justo lo que corresponde mostrar.
        if (response.status === 404) {
            applyForm.style.display = "";
            return;
        }

        if (!response.ok) {
            throw new Error("Error al consultar tu postulación.");
        }

        pintarEstadoPostulacion(await response.json());

    } catch (error) {
        loadingEl.style.display = "none";
        showMessage("apply-message", error.message || "Error al consultar tu postulación.", "error");
    }
}

/**
 * Pinta el estado de la última postulación y muestra u oculta el
 * formulario según corresponda.
 */
function pintarEstadoPostulacion(app) {
    const estados = {
        Pending: { etiqueta: "En revisión", clase: "status-pending" },
        Approved: { etiqueta: "Aprobada", clase: "status-completed" },
        Rejected: { etiqueta: "Rechazada", clase: "status-rejected" },
    };
    const estado = estados[app.status] || { etiqueta: app.status, clase: "status-none" };

    let aviso = "";
    if (app.status === "Pending") {
        aviso = "<p>Tu postulación está en revisión. Te avisaremos por notificación cuando el administrador decida.</p>";
    } else if (app.status === "Approved") {
        aviso = "<p>¡Tu postulación fue aprobada! Cierra sesión y vuelve a entrar para ver las opciones de gestor.</p>";
    } else if (app.status === "Rejected" && app.reviewComment) {
        aviso = `<p>Tu postulación no fue aprobada. Motivo: ${escapeHtml(app.reviewComment)}</p>`;
    }

    applyStatusEl.innerHTML = `
        <div class="request-card">
            <div class="card-header">
                <span class="status-badge ${estado.clase}">${estado.etiqueta}</span>
                <span class="card-date">Enviada: ${formatDate(app.requestDate)}</span>
            </div>
            <div class="card-body">
                <p><strong>Tu motivación:</strong> ${escapeHtml(app.motivation)}</p>
                ${aviso}
            </div>
        </div>
    `;

    // Solo tiene sentido volver a postular tras un rechazo. Pendiente o
    // Aprobada: nada que ganar con enviar otra.
    applyForm.style.display = (app.status === "Rejected") ? "" : "none";
}

applyForm.addEventListener("submit", async (e) => {
    e.preventDefault();

    const motivationInput = document.getElementById("motivation");
    const motivation = motivationInput.value.trim();

    if (motivation.length < 20) {
        showMessage("apply-message", "Cuéntanos un poco más: al menos 20 caracteres.", "error");
        return;
    }

    const btn = document.getElementById("submitApplyBtn");
    btn.disabled = true;
    btn.textContent = "Enviando...";
    showMessage("apply-message", "");

    try {
        const response = await fetchConSesion(`${API_BASE}/ManagerApplication/Apply`, {
            method: "POST",
            headers: authHeaders(),
            body: JSON.stringify({ motivation })
        });

        const text = await response.text();
        let result;
        try { result = JSON.parse(text); } catch { result = text; }

        if (!response.ok) {
            throw new Error(mensajeDeError(result, "No se pudo enviar la postulación."));
        }

        motivationInput.value = "";

        // Se recarga ANTES de mostrar el aviso: cargarMiPostulacion empieza
        // por limpiar el mensaje de la sección, así que ponerlo primero se
        // borraría solo apenas terminara de cargar.
        await cargarMiPostulacion();
        showMessage("apply-message", "Postulación enviada. Te avisaremos cuando el administrador decida.", "success");

    } catch (error) {
        showMessage("apply-message", error.message || "No se pudo enviar la postulación.", "error");
    } finally {
        btn.disabled = false;
        btn.textContent = "Enviar postulación";
    }
});