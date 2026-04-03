import { checkAuth } from "../../utils/authGuard.js";
import { requireRole } from "../../utils/roleGuard.js";
import { API_BASE } from "../../services/api.js";
//js/pages / manager / dashboard.js
// ============================================================
// INICIALIZACIÓN
// ============================================================

checkAuth();
requireRole("Manager");

const user = JSON.parse(localStorage.getItem("user"));
const token = localStorage.getItem("token");

// ============================================================
// HEADER — BIENVENIDA
// ============================================================

if (user) {
    document.getElementById("welcomeMsg").textContent = `Hola, ${user.name} ${user.lastName}`;
    document.getElementById("userEmail").textContent = user.email ?? "";
}

// ============================================================
// HEADER — DROPDOWN DE USUARIO
// ============================================================

const trigger = document.getElementById("userMenuTrigger");
const dropdown = document.getElementById("userDropdown");

trigger.addEventListener("click", (e) => {
    e.stopPropagation();
    const isOpen = dropdown.classList.toggle("is-open");
    trigger.setAttribute("aria-expanded", isOpen);
});

document.addEventListener("click", () => {
    dropdown.classList.remove("is-open");
    trigger.setAttribute("aria-expanded", "false");
});

dropdown.addEventListener("click", (e) => e.stopPropagation());

// ============================================================
// HEADER — LOGOUT
// ============================================================

document.getElementById("logoutBtn").addEventListener("click", () => {
    localStorage.removeItem("token");
    localStorage.removeItem("user");
    window.location.href = "/pages/auth/login.html";
});

// ============================================================
// NAVEGACIÓN ENTRE SECCIONES
// ============================================================

document.querySelectorAll(".nav-btn").forEach(btn => {
    btn.addEventListener("click", () => {
        const target = btn.dataset.section;

        document.querySelectorAll(".nav-btn").forEach(b => b.classList.remove("active"));
        document.querySelectorAll(".section").forEach(s => s.classList.remove("active"));

        btn.classList.add("active");
        document.getElementById(target).classList.add("active");

        if (target === "solicitudes-disponibles") loadPendingRequests();
        if (target === "mis-asignaciones") loadMyAssignments();
        if (target === "todas-solicitudes") loadAllRequests();
    });
});

// ============================================================
// UTILIDADES
// ============================================================

function authHeaders() {
    return {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${token}`
    };
}

function showMessage(elementId, text, type = "error") {
    const el = document.getElementById(elementId);
    if (!el) return;
    el.textContent = text;
    el.className = `form-hint ${type}`;
    if (text) {
        setTimeout(() => { el.textContent = ""; el.className = "form-hint"; }, 4000);
    }
}

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

function statusClass(status) {
    return `status-${(status || "none").toLowerCase()}`;
}

function formatDate(iso) {
    if (!iso) return "—";
    return new Date(iso).toLocaleDateString("es-CO", { year: "numeric", month: "short", day: "numeric" });
}

function formatTime(t) {
    return t ? t.substring(0, 5) : "—";
}

/** Genera un <span> con ícono SVG para usar dentro de tarjetas */
function icon(name) {
    return `<span class="card-icon icon-${name}" aria-hidden="true"></span>`;
}

// ============================================================
// SECCIÓN 1 — SOLICITUDES DISPONIBLES (Pending)
// ============================================================

document.getElementById("refreshPendingBtn").addEventListener("click", loadPendingRequests);
loadPendingRequests();

async function loadPendingRequests() {
    const list = document.getElementById("pending-list");
    const loading = document.getElementById("pending-loading");
    list.innerHTML = "";
    loading.style.display = "block";
    showMessage("pending-message", "");

    try {
        const res = await fetch(`${API_BASE}/collectionrequest/GetPendingRequests`, {
            headers: authHeaders()
        });

        loading.style.display = "none";

        if (res.status === 404) {
            list.innerHTML = "<p class='empty-msg'>No hay solicitudes pendientes en este momento.</p>";
            return;
        }

        if (!res.ok) throw new Error("Error al obtener solicitudes pendientes");

        const requests = await res.json();

        if (!requests?.length) {
            list.innerHTML = "<p class='empty-msg'>No hay solicitudes pendientes.</p>";
            return;
        }

        requests.sort((a, b) => new Date(b.requestDate) - new Date(a.requestDate));
        list.innerHTML = requests.map(r => renderPendingCard(r)).join("");

        list.querySelectorAll(".btn-accept").forEach(btn => {
            btn.addEventListener("click", () => acceptRequest(btn.dataset.id, btn));
        });

    } catch (err) {
        loading.style.display = "none";
        showMessage("pending-message", err.message || "Error al cargar solicitudes.", "error");
    }
}

function renderPendingCard(req) {
    return `
        <div class="request-card">
            <div class="card-header">
                <span class="status-badge status-pending">Pendiente</span>
                <span class="card-date">Creada: ${formatDate(req.requestDate)}</span>
            </div>
            <div class="card-body">
                <p>${icon("usuario")}<strong>Ciudadano:</strong> ${req.citizenName} ${req.citizenLastName}</p>
                <p>${icon("calendario")}<strong>Fecha:</strong> ${formatDate(req.collectionDate)}</p>
                <p>${icon("reloj")}<strong>Hora:</strong> ${formatTime(req.collectionTime)}</p>
                <p>${icon("direccion")}<strong>Dirección:</strong> ${req.collectionAddress}</p>
                <p>${icon("telefono")}<strong>Teléfono:</strong> ${req.contactPhone}</p>
                <p>${icon("reciclaje")}<strong>Residuos:</strong> ${req.wasteTypes}</p>
                ${req.citizenObservations ? `<p>${icon("observacion")}<strong>Notas:</strong> ${req.citizenObservations}</p>` : ""}
            </div>
            <div class="card-actions">
                <button class="btn-accept" data-id="${req.idRequest}">Aceptar solicitud</button>
            </div>
        </div>
    `;
}

async function acceptRequest(idRequest, btn) {
    btn.disabled = true;
    btn.textContent = "Aceptando...";

    try {
        const res = await fetch(
            `${API_BASE}/collectionrequest/AcceptRequest?idRequest=${idRequest}&idManager=${user.idUser}`,
            { method: "POST", headers: authHeaders() }
        );

        const data = await res.json().catch(() => ({}));

        if (!res.ok) throw new Error(data.message || "No se pudo aceptar la solicitud. Ya fue tomada.");

        showMessage("pending-message", "Solicitud aceptada. Aparece ahora en 'Mis Asignaciones'.", "success");
        setTimeout(loadPendingRequests, 1500);

    } catch (err) {
        showMessage("pending-message", err.message, "error");
        btn.disabled = false;
        btn.textContent = "Aceptar solicitud";
    }
}

// ============================================================
// SECCIÓN 2 — MIS ASIGNACIONES
// ============================================================

document.getElementById("refreshAssignedBtn").addEventListener("click", loadMyAssignments);

async function loadMyAssignments() {
    const list = document.getElementById("assigned-list");
    const loading = document.getElementById("assigned-loading");
    list.innerHTML = "";
    loading.style.display = "block";
    showMessage("assigned-message", "");

    try {
        const res = await fetch(`${API_BASE}/collectionrequest/GetCollectionRequests`, {
            headers: authHeaders()
        });

        loading.style.display = "none";

        if (res.status === 404) {
            list.innerHTML = "<p class='empty-msg'>No tienes asignaciones activas.</p>";
            return;
        }

        if (!res.ok) throw new Error("Error al obtener asignaciones");

        const all = await res.json();
        const mine = all.filter(r =>
            r.currentStatus === "Assigned" || r.currentStatus === "InProgress"
        );

        if (!mine.length) {
            list.innerHTML = "<p class='empty-msg'>No tienes asignaciones activas en este momento.</p>";
            return;
        }

        mine.sort((a, b) => new Date(b.requestDate) - new Date(a.requestDate));
        list.innerHTML = mine.map(r => renderAssignedCard(r)).join("");

        list.querySelectorAll(".btn-status").forEach(btn => {
            btn.addEventListener("click", () => openStatusModal(btn.dataset.id, btn.dataset.status));
        });

    } catch (err) {
        loading.style.display = "none";
        showMessage("assigned-message", err.message || "Error al cargar asignaciones.", "error");
    }
}

function renderAssignedCard(req) {
    return `
        <div class="request-card">
            <div class="card-header">
                <span class="status-badge ${statusClass(req.currentStatus)}">${translateStatus(req.currentStatus)}</span>
                <span class="card-date">Creada: ${formatDate(req.requestDate)}</span>
            </div>
            <div class="card-body">
                <p>${icon("usuario")}<strong>Ciudadano:</strong> ${req.citizenName} ${req.citizenLastName}</p>
                <p>${icon("calendario")}<strong>Fecha:</strong> ${formatDate(req.collectionDate)}</p>
                <p>${icon("reloj")}<strong>Hora:</strong> ${formatTime(req.collectionTime)}</p>
                <p>${icon("direccion")}<strong>Dirección:</strong> ${req.collectionAddress}</p>
                <p>${icon("telefono")}<strong>Teléfono:</strong> ${req.contactPhone}</p>
                <p>${icon("reciclaje")}<strong>Residuos:</strong> ${req.wasteTypes}</p>
                ${req.citizenObservations ? `<p>${icon("observacion")}<strong>Notas:</strong> ${req.citizenObservations}</p>` : ""}
            </div>
            <div class="card-actions">
                <button class="btn-status" data-id="${req.idRequest}" data-status="${req.currentStatus}">
                    Cambiar estado
                </button>
            </div>
        </div>
    `;
}

// ============================================================
// SECCIÓN 3 — TODAS LAS SOLICITUDES
// ============================================================

document.getElementById("refreshAllBtn").addEventListener("click", loadAllRequests);
document.getElementById("statusFilter").addEventListener("change", renderFilteredList);

let allRequests = [];

async function loadAllRequests() {
    const list = document.getElementById("all-list");
    const loading = document.getElementById("all-loading");
    list.innerHTML = "";
    loading.style.display = "block";
    showMessage("all-message", "");

    try {
        const res = await fetch(`${API_BASE}/collectionrequest/GetCollectionRequests`, {
            headers: authHeaders()
        });

        loading.style.display = "none";

        if (res.status === 404) {
            list.innerHTML = "<p class='empty-msg'>No hay solicitudes registradas.</p>";
            return;
        }

        if (!res.ok) throw new Error("Error al obtener todas las solicitudes");

        allRequests = await res.json();
        allRequests.sort((a, b) => new Date(b.requestDate) - new Date(a.requestDate));
        renderFilteredList();

    } catch (err) {
        loading.style.display = "none";
        showMessage("all-message", err.message || "Error al cargar solicitudes.", "error");
    }
}

function renderFilteredList() {
    const list = document.getElementById("all-list");
    const filter = document.getElementById("statusFilter").value;

    const filtered = filter
        ? allRequests.filter(r => r.currentStatus === filter)
        : allRequests;

    if (!filtered.length) {
        list.innerHTML = "<p class='empty-msg'>No hay solicitudes con ese estado.</p>";
        return;
    }

    list.innerHTML = filtered.map(r => renderAllCard(r)).join("");

    list.querySelectorAll(".btn-status").forEach(btn => {
        btn.addEventListener("click", () => openStatusModal(btn.dataset.id, btn.dataset.status));
    });
}

function renderAllCard(req) {
    const isPending = req.currentStatus === "Pending";
    const isTerminal = req.currentStatus === "Completed" || req.currentStatus === "Rejected";
    const statusBtn = (!isPending && !isTerminal)
        ? `<button class="btn-status" data-id="${req.idRequest}" data-status="${req.currentStatus}">Cambiar estado</button>`
        : "";

    return `
        <div class="request-card">
            <div class="card-header">
                <span class="status-badge ${statusClass(req.currentStatus)}">${translateStatus(req.currentStatus)}</span>
                <span class="card-date">Creada: ${formatDate(req.requestDate)}</span>
            </div>
            <div class="card-body">
                <p>${icon("usuario")}<strong>Ciudadano:</strong> ${req.citizenName} ${req.citizenLastName}</p>
                <p>${icon("calendario")}<strong>Fecha:</strong> ${formatDate(req.collectionDate)}</p>
                <p>${icon("reloj")}<strong>Hora:</strong> ${formatTime(req.collectionTime)}</p>
                <p>${icon("direccion")}<strong>Dirección:</strong> ${req.collectionAddress}</p>
                <p>${icon("telefono")}<strong>Teléfono:</strong> ${req.contactPhone}</p>
                <p>${icon("reciclaje")}<strong>Residuos:</strong> ${req.wasteTypes}</p>
            </div>
            ${statusBtn ? `<div class="card-actions">${statusBtn}</div>` : ""}
        </div>
    `;
}

// ============================================================
// MODAL — CAMBIAR ESTADO
// ============================================================

const statusModal = document.getElementById("statusModal");
const statusModalOverlay = document.getElementById("statusModalOverlay");

function openStatusModal(idRequest, currentStatus) {
    document.getElementById("modalRequestId").value = idRequest;
    showMessage("status-modal-message", "");
    document.getElementById("statusComment").value = "";

    const select = document.getElementById("newStatusSelect");
    const next = { Assigned: "InProgress", InProgress: "Completed" };
    select.value = next[currentStatus] ?? "Completed";

    statusModal.style.display = "flex";
    statusModalOverlay.style.display = "block";
}

function closeStatusModal() {
    statusModal.style.display = "none";
    statusModalOverlay.style.display = "none";
}

document.getElementById("closeStatusModalBtn").addEventListener("click", closeStatusModal);
document.getElementById("cancelStatusBtn").addEventListener("click", closeStatusModal);
statusModalOverlay.addEventListener("click", closeStatusModal);

document.getElementById("saveStatusBtn").addEventListener("click", async () => {
    const btn = document.getElementById("saveStatusBtn");
    const idRequest = document.getElementById("modalRequestId").value;
    const newStatus = document.getElementById("newStatusSelect").value;
    const comment = document.getElementById("statusComment").value.trim() || null;

    btn.disabled = true;
    btn.textContent = "Guardando...";
    showMessage("status-modal-message", "");

    try {
        const params = new URLSearchParams({
            idRequest,
            newStatus,
            idManager: user.idUser,
            ...(comment ? { comment } : {})
        });

        const res = await fetch(
            `${API_BASE}/collectionrequest/UpdateStatus?${params.toString()}`,
            { method: "PATCH", headers: authHeaders() }
        );

        const data = await res.json().catch(() => ({}));

        if (!res.ok) throw new Error(data.message || "Error al actualizar el estado");

        showMessage("status-modal-message", "Estado actualizado correctamente.", "success");

        setTimeout(() => {
            closeStatusModal();
            const activeSection = document.querySelector(".section.active")?.id;
            if (activeSection === "mis-asignaciones") loadMyAssignments();
            if (activeSection === "todas-solicitudes") loadAllRequests();
        }, 1200);

    } catch (err) {
        showMessage("status-modal-message", err.message || "No se pudo actualizar el estado.", "error");
    } finally {
        btn.disabled = false;
        btn.textContent = "Guardar";
    }
});