import { checkAuth } from "../../utils/authGuard.js";
import { requireRole } from "../../utils/roleGuard.js";
import { API_BASE } from "../../services/api.js";
// /js/pages/citizen/dashboard.js
// ============================================================
// INICIALIZACIÓN
// ============================================================

// 🔐 Proteger acceso — redirige a login si no hay token válido
checkAuth();

// 🛡️ Verificar que el usuario sea Citizen (redirige si es Manager)
requireRole("Citizen");

// 👤 Leer usuario desde localStorage (guardado en login)
const user = JSON.parse(localStorage.getItem("user"));
const token = localStorage.getItem("token");

// Mostrar nombre de bienvenida en el header
if (user) {
    document.getElementById("welcomeMsg").textContent = `Hola, ${user.name} ${user.lastName}`;
}

// ============================================================
// DROPDOWN DE USUARIO
// ============================================================

const trigger = document.getElementById("userMenuTrigger");
const dropdown = document.getElementById("userDropdown");

trigger.addEventListener("click", (e) => {
    e.stopPropagation();
    const isOpen = dropdown.classList.toggle("is-open");
    trigger.setAttribute("aria-expanded", isOpen);
});

// Cerrar al hacer clic fuera
document.addEventListener("click", () => {
    dropdown.classList.remove("is-open");
    trigger.setAttribute("aria-expanded", "false");
});

// Evitar que clics dentro del dropdown lo cierren
dropdown.addEventListener("click", (e) => e.stopPropagation());

// ============================================================
// NAVEGACIÓN ENTRE SECCIONES
// ============================================================

const navButtons = document.querySelectorAll(".nav-btn");
const sections = document.querySelectorAll(".section");

navButtons.forEach(btn => {
    btn.addEventListener("click", () => {
        const targetSection = btn.dataset.section;

        // Desactivar todos los botones y secciones
        navButtons.forEach(b => b.classList.remove("active"));
        sections.forEach(s => s.classList.remove("active"));

        // Activar el botón y sección seleccionados
        btn.classList.add("active");
        document.getElementById(targetSection).classList.add("active");

        // Cargar datos al cambiar de sección
        if (targetSection === "mis-solicitudes") loadMyRequests();
        if (targetSection === "historial") loadHistory();
    });
});

// ============================================================
// LOGOUT
// ============================================================

document.getElementById("logoutBtn").addEventListener("click", () => {
    localStorage.removeItem("token");
    localStorage.removeItem("user");
    window.location.href = "/pages/auth/login.html";
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
//function showMessage(elementId, text, type = "error") {
//    const el = document.getElementById(elementId);
//    el.textContent = text;
//    el.className = `msg ${type}`;

//    // Limpiar automáticamente después de 4 segundos
//    setTimeout(() => {
//        el.textContent = "";
//        el.className = "";
//    }, 4000);
//}

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
        const response = await fetch(`${API_BASE}/collectionrequest/CreateCollectionRequest`, {
            method: "POST",
            headers: authHeaders(),
            body: JSON.stringify(dto)
        });

        const text = await response.text();
        let result;
        try { result = JSON.parse(text); } catch { result = text; }

        if (!response.ok) {
            throw new Error(result.message || result || "Error al crear la solicitud");
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
        const res = await fetch(
            `${API_BASE}/collectionrequest/GetRequestsByUser?idUser=${user.idUser}`,
            { headers: authHeaders() }
        );

        loadingEl.style.display = "none";

        if (res.status === 404) {
            listEl.innerHTML = "<p class='empty-msg'>No tienes solicitudes registradas aún.</p>";
            return;
        }

        if (!res.ok) throw new Error("Error al obtener solicitudes");

        const requests = await res.json();

        /*        listEl.innerHTML = requests.map(req => renderRequestCard(req)).join("");*/
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

    } catch (error) {
        loadingEl.style.display = "none";
        showMessage("requests-message", "Error al cargar tus solicitudes.", "error");
    }
}
//async function loadMyRequests() {
//    const listEl = document.getElementById("requests-list");
//    const loadingEl = document.getElementById("requests-loading");

//    listEl.innerHTML = "";
//    loadingEl.style.display = "block";
//    showMessage("requests-message", "");

//    try {
//        // El backend filtra por el idUser del token — traemos todas y filtramos en frontend
//        // ya que el endpoint GetCollectionRequests es solo para Admin/Manager.
//        // Usamos GetCollectionRequestById iterando los que conocemos, 
//        // pero la mejor práctica aquí es filtrar por usuario desde el historial.
//        // Dado que no hay endpoint GetByUser en CollectionRequest, obtenemos por historial
//        // y luego cargamos el detalle de cada solicitud única.
//        const historyResponse = await fetch(
//            `${API_BASE}/history/GetByUser?idUser=${user.idUser}`,
//            { headers: authHeaders() }
//        );

//        let requestIds = [];

//        if (historyResponse.ok) {
//            const histories = await historyResponse.json();
//            // Extraer IDs únicos de solicitudes del historial del usuario
//            const unique = [...new Set(histories.map(h => h.idRequest))];
//            requestIds = unique;
//        }

//        // Si no hay historial, mostrar mensaje vacío
//        if (requestIds.length === 0) {
//            loadingEl.style.display = "none";
//            listEl.innerHTML = "<p class='empty-msg'>No tienes solicitudes registradas aún.</p>";
//            return;
//        }

//        // Cargar detalle de cada solicitud
//        const detailPromises = requestIds.map(id =>
//            fetch(`${API_BASE}/collectionrequest/GetCollectionRequestById?idRequest=${id}`, {
//                headers: authHeaders()
//            }).then(r => r.ok ? r.json() : null)
//        );

//        const requests = (await Promise.all(detailPromises)).filter(Boolean);

//        loadingEl.style.display = "none";

//        if (requests.length === 0) {
//            listEl.innerHTML = "<p class='empty-msg'>No se encontraron solicitudes.</p>";
//            return;
//        }

//        // Ordenar por fecha de solicitud más reciente primero
//        requests.sort((a, b) => new Date(b.requestDate) - new Date(a.requestDate));

//        listEl.innerHTML = requests.map(req => renderRequestCard(req)).join("");

//        // Adjuntar eventos de edición a los botones renderizados
//        listEl.querySelectorAll(".edit-btn").forEach(btn => {
//            btn.addEventListener("click", () => {
//                const idRequest = btn.dataset.id;
//                const req = requests.find(r => r.idRequest === idRequest);
//                if (req) openEditModal(req);
//            });
//        });

//    } catch (error) {
//        loadingEl.style.display = "none";
//        showMessage("requests-message", "Error al cargar tus solicitudes. Verifica tu conexión.", "error");
//    }
//}

/**
 * Genera el HTML de una tarjeta de solicitud
 */
//function renderRequestCard(req) {
//    const isPending = req.currentStatus === "Pending";
//    const editBtn = isPending
//        ? `<button class="edit-btn" data-id="${req.idRequest}">✏️ Editar</button>`
//        : `<button class="edit-btn" disabled title="Solo se pueden editar solicitudes pendientes">✏️ Editar</button>`;

//    return `
//        <div class="request-card">
//            <div class="card-header">
//                <span class="status-badge status-${req.currentStatus.toLowerCase()}">${translateStatus(req.currentStatus)}</span>
//                <span class="card-date">Creada: ${formatDate(req.requestDate)}</span>
//            </div>
//            <div class="card-body">
//                <p><strong>📅 Fecha recolección:</strong> ${formatDate(req.collectionDate)}</p>
//                <p><strong>🕐 Hora:</strong> ${formatTime(req.collectionTime)}</p>
//                <p><strong>📍 Dirección:</strong> ${req.collectionAddress}</p>
//                <p><strong>📞 Teléfono:</strong> ${req.contactPhone}</p>
//                <p><strong>♻️ Residuos:</strong> ${req.wasteTypes}</p>
//                ${req.citizenObservations ? `<p><strong>📝 Observaciones:</strong> ${req.citizenObservations}</p>` : ""}
//            </div>
//            <div class="card-actions">
//                ${editBtn}
//            </div>
//        </div>
//    `;
//}

function renderRequestCard(req) {
    const isPending = req.currentStatus === "Pending";
    const editBtn = isPending
        ? `<button class="edit-btn" data-id="${req.idRequest}">${icon("lapiz")} Editar</button>`
        : `<button class="edit-btn" disabled title="Solo se pueden editar solicitudes pendientes">${icon("lapiz")} Editar</button>`;

    return `
        <div class="request-card">
            <div class="card-header">
                <span class="status-badge status-${req.currentStatus.toLowerCase()}">${translateStatus(req.currentStatus)}</span>
                <span class="card-date">Creada: ${formatDate(req.requestDate)}</span>
            </div>
            <div class="card-body">
                <p>${icon("calendario")}<strong>Fecha recolección:</strong> ${formatDate(req.collectionDate)}</p>
                <p>${icon("reloj")}<strong>Hora:</strong> ${formatTime(req.collectionTime)}</p>
                <p>${icon("direccion")}<strong>Dirección:</strong> ${req.collectionAddress}</p>
                <p>${icon("telefono")}<strong>Teléfono:</strong> ${req.contactPhone}</p>
                <p>${icon("reciclaje")}<strong>Residuos:</strong> ${req.wasteTypes}</p>
                ${req.citizenObservations ? `<p>${icon("observacion")}<strong>Observaciones:</strong> ${req.citizenObservations}</p>` : ""}
            </div>
            <div class="card-actions">
                ${editBtn}
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
        const response = await fetch(`${API_BASE}/collectionrequest/UpdateCollectionRequest`, {
            method: "PUT",
            headers: authHeaders(),
            body: JSON.stringify(dto)
        });

        const text = await response.text();
        let result;
        try { result = JSON.parse(text); } catch { result = text; }

        if (!response.ok) {
            throw new Error(result.message || result || "Error al actualizar la solicitud");
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
        // GET api/history/GetByUser?idUser={idUser}
        const response = await fetch(
            `${API_BASE}/history/GetByUser?idUser=${user.idUser}`,
            { headers: authHeaders() }
        );

        loadingEl.style.display = "none";

        if (response.status === 404) {
            listEl.innerHTML = "<p class='empty-msg'>No tienes historial de recolecciones aún.</p>";
            return;
        }

        if (!response.ok) {
            throw new Error("Error al obtener el historial");
        }

        const histories = await response.json();

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
                <span class="status-badge status-${h.newStatus.toLowerCase()}">${next}</span>
            </div>
            <div class="history-meta">
                <span>Gestionado por: <strong>${h.userName || "Sistema"}</strong></span>
                ${h.comment ? `<span class="history-comment">💬 ${h.comment}</span>` : ""}
            </div>
        </div>
    `;
}