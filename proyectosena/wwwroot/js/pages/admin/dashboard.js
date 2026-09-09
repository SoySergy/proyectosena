import { checkAuth } from "../../utils/authGuard.js";
import { requireRole, ROLES } from "../../utils/roleGuard.js";
import { initUserMenu } from "../../utils/userMenu.js";
import { initTabs } from "../../utils/tabs.js";
import { initNotificaciones } from "../../utils/notificaciones.js";
import { API_BASE, authHeaders, leerCuerpo, mensajeDeError } from "../../services/api.js";
import { escapeHtml } from "../../utils/html.js";
import { icon, formatDate } from "../../utils/format.js";
// /js/pages/admin/dashboard.js

// ============================================================
// INICIALIZACIÓN
// ============================================================

// Sin sesión válida no se sigue.
checkAuth();

// Y con sesión, solo el administrador. Antes esta página no tenía guardia:
// se comprobó que una ciudadana con sesión la abría entera sin que nadie la
// echara. Quien no sea administrador acaba en el panel que le toca.
//
// Esto es comodidad, no seguridad: la API rechaza por su cuenta a quien no
// sea administrador (403), aunque se salte esta pantalla.
requireRole(ROLES.ADMIN);

// ============================================================
// CABECERA Y NAVEGACIÓN
// ============================================================

initUserMenu();

// La campana con el número de avisos sin leer. Es la misma en los tres
// paneles: el endpoint solo pide sesión, no un rol concreto.
initNotificaciones();

initTabs({
    // Al volver a una pestaña se piden los datos otra vez, que para eso son
    // el estado del momento. «Crear gestor» no pide nada: es un formulario.
    resumen: cargarResumen,
    "solicitudes-gestor": cargarSolicitudes,
});

// ============================================================
// SECCIÓN: RESUMEN DEL SISTEMA
// ============================================================

/**
 * Qué tarjeta se pinta con cada número que manda el servidor.
 *
 * Está en una tabla y no en nueve trozos de HTML repetidos: añadir una
 * estadística nueva es añadir una fila aquí, sin tocar la función que pinta.
 *
 * El acento es opcional. Solo lo llevan los tres que conviene distinguir de
 * un vistazo —lo que está esperando, lo que salió bien y lo que salió mal—;
 * los demás van con el verde de siempre.
 */
const TARJETAS_RESUMEN = [
    { campo: "totalRequests", etiqueta: "Solicitudes totales" },
    { campo: "pending", etiqueta: "Pendientes", acento: "warning" },
    { campo: "assigned", etiqueta: "Asignadas" },
    { campo: "inProgress", etiqueta: "En progreso" },
    { campo: "completed", etiqueta: "Completadas", acento: "success" },
    { campo: "rejected", etiqueta: "Rechazadas", acento: "danger" },
    { campo: "requestsLast30Days", etiqueta: "Últimos 30 días" },
    { campo: "activeManagers", etiqueta: "Gestores activos" },
    { campo: "activeCitizens", etiqueta: "Ciudadanos activos" },
];

async function cargarResumen() {
    const { ok, datos } = await pedirAlServidor(
        "/Admin/GetDashboardStats",
        { aviso: "stats-message", cargando: "stats-loading" },
        "No se pudo cargar el resumen."
    );

    if (!ok) return;

    // Se mira que el cuerpo traiga de verdad los nueve números antes de
    // pintarlos. Sin esta comprobación, un 200 con cualquier otra cosa
    // dentro pintaba nueve ceros sin avisar: el administrador leía que
    // no hay ninguna solicitud en el sistema, que es peor que un error.
    if (!esResumenValido(datos)) {
        mostrarMensaje("stats-message", "El servidor devolvió una respuesta inesperada.");
        return;
    }

    pintarResumen(datos);
}

/**
 * ¿El cuerpo que llegó es de verdad el resumen?
 *
 * Se apoya en la misma tabla que se usa para pintar, así que si mañana se
 * añade una estadística, la comprobación la incluye sola.
 */
function esResumenValido(datos) {
    return datos !== null
        && typeof datos === "object"
        && TARJETAS_RESUMEN.every(({ campo }) => typeof datos[campo] === "number");
}

function pintarResumen(datos) {
    const rejilla = document.getElementById("stats-grid");
    if (!rejilla) return;

    rejilla.innerHTML = TARJETAS_RESUMEN
        .map(({ campo, etiqueta, acento }) => {
            const clases = acento ? `stat-card stat-card--${acento}` : "stat-card";

            // Los números vienen del servidor y son enteros, pero se escapan
            // igual: es lo que ya se hace en los otros paneles y no cuesta nada.
            return `
            <div class="${clases}">
                <span class="stat-card__value">${escapeHtml(datos?.[campo] ?? 0)}</span>
                <span class="stat-card__label">${escapeHtml(etiqueta)}</span>
            </div>`;
        })
        .join("");
}

document.getElementById("refreshStatsBtn")?.addEventListener("click", cargarResumen);

// ============================================================
// SECCIÓN: SOLICITUDES DE GESTOR
// ============================================================

/**
 * Qué datos del solicitante se muestran y con qué iconito, en este orden.
 * Igual que en el resumen: añadir un dato es añadir una fila.
 *
 * La motivación no está aquí porque es un texto largo y se pinta aparte,
 * debajo del resto y solo si la persona escribió algo.
 */
const DATOS_DEL_SOLICITANTE = [
    { campo: "applicantName", etiqueta: "Solicitante", icono: "usuario" },
    { campo: "applicantEmail", etiqueta: "Correo", icono: "correo" },
    { campo: "applicantDocument", etiqueta: "Documento", icono: "tipodocumento" },
    { campo: "applicantPhone", etiqueta: "Teléfono", icono: "telefono" },
];

async function cargarSolicitudes() {
    const { ok, datos } = await pedirAlServidor(
        "/ManagerApplication/GetPending",
        { aviso: "applications-message", cargando: "applications-loading" },
        "No se pudieron cargar las solicitudes."
    );

    if (!ok) return;

    // El servidor contesta {items, page, pageSize, totalItems, totalPages}.
    // Si no viene la lista, mejor decirlo que pintar una bandeja vacía y
    // hacer creer que nadie ha solicitado nada.
    if (!Array.isArray(datos?.items)) {
        mostrarMensaje("applications-message", "El servidor devolvió una respuesta inesperada.");
        return;
    }

    pintarSolicitudes(datos.items);
}

function pintarSolicitudes(solicitudes) {
    const lista = document.getElementById("applications-list");
    if (!lista) return;

    if (solicitudes.length === 0) {
        lista.innerHTML = `<p class="empty-msg">No hay solicitudes pendientes por revisar.</p>`;
        return;
    }

    lista.innerHTML = solicitudes.map(tarjetaDeSolicitud).join("");
}

function tarjetaDeSolicitud(solicitud) {
    // Todo lo que escribió la persona pasa por escapeHtml: el nombre, el
    // correo y sobre todo la motivación, que es texto libre.
    const filas = DATOS_DEL_SOLICITANTE
        .map(({ campo, etiqueta, icono }) =>
            `<p>${icon(icono)}<strong>${etiqueta}:</strong> ${escapeHtml(solicitud[campo])}</p>`)
        .join("");

    const motivacion = solicitud.motivation
        ? `<p>${icon("observacion")}<strong>Motivación:</strong> ${escapeHtml(solicitud.motivation)}</p>`
        : "";

    // El identificador va dentro de un atributo, así que también se escapa.
    const id = escapeHtml(solicitud.idApplication);

    return `
        <div class="request-card" data-solicitud="${id}">
            <div class="card-header">
                <span class="status-badge status-pending">Pendiente</span>
                <span class="card-date">Solicitada: ${formatDate(solicitud.requestDate)}</span>
            </div>
            <div class="card-body">
                ${filas}
                ${motivacion}
            </div>
            <div class="card-actions">
                <button type="button" class="btn-card btn-card--reject" data-accion="pedir-motivo">Rechazar</button>
                <button type="button" class="btn-card btn-card--approve" data-accion="aprobar">Aprobar</button>
            </div>
            <div class="reject-box" hidden>
                <label class="form-label" for="motivo-${id}">Motivo del rechazo</label>
                <textarea id="motivo-${id}" class="form-input" rows="2" maxlength="500"
                          placeholder="Explícale al solicitante por qué no se aprueba (mínimo 10 caracteres)."></textarea>
                <div class="card-actions">
                    <button type="button" class="btn-card" data-accion="cancelar">Cancelar</button>
                    <button type="button" class="btn-card btn-card--reject" data-accion="rechazar">Confirmar rechazo</button>
                </div>
            </div>
        </div>`;
}

document.getElementById("refreshApplicationsBtn")?.addEventListener("click", cargarSolicitudes);

// ============================================================
// SECCIÓN: APROBAR Y RECHAZAR
// ============================================================

// El texto mínimo que el servidor exige en el motivo del rechazo
// (RejectManagerApplicationDto lo valida con MinLength(10)). Se comprueba
// aquí también para no gastar un viaje al servidor y para poder decirlo
// mientras la persona todavía tiene el cursor puesto.
const MINIMO_DEL_MOTIVO = 10;

/**
 * Un solo oyente para toda la bandeja, en vez de uno por botón.
 *
 * Las tarjetas se vuelven a pintar cada vez que se recarga la lista: con
 * oyentes por botón habría que volver a colgarlos en cada repintado, y los
 * de las tarjetas viejas se quedarían sueltos.
 */
document.getElementById("applications-list")?.addEventListener("click", (evento) => {
    const boton = evento.target.closest("[data-accion]");
    if (!boton) return;

    const tarjeta = boton.closest("[data-solicitud]");
    if (!tarjeta) return;

    const id = tarjeta.dataset.solicitud;
    const caja = tarjeta.querySelector(".reject-box");

    switch (boton.dataset.accion) {
        case "aprobar":
            decidir(tarjeta, `/ManagerApplication/Approve?idApplication=${encodeURIComponent(id)}`, null,
                "Solicitud aprobada. Ya es gestor o gestora.", "No se pudo aprobar la solicitud.");
            break;

        case "pedir-motivo":
            caja.hidden = false;
            caja.querySelector("textarea").focus();
            break;

        case "cancelar":
            caja.hidden = true;
            caja.querySelector("textarea").value = "";
            break;

        case "rechazar": {
            const motivo = caja.querySelector("textarea").value.trim();

            if (motivo.length < MINIMO_DEL_MOTIVO) {
                mostrarMensaje("applications-message",
                    `El motivo debe tener al menos ${MINIMO_DEL_MOTIVO} caracteres.`);
                return;
            }

            decidir(tarjeta, `/ManagerApplication/Reject?idApplication=${encodeURIComponent(id)}`, { reason: motivo },
                "Solicitud rechazada. Se le avisó al solicitante.", "No se pudo rechazar la solicitud.");
            break;
        }
    }
});

/**
 * Manda la decisión al servidor y recarga la bandeja.
 *
 * Aprobar y rechazar solo se diferencian en la ruta y en si llevan cuerpo,
 * así que comparten esta función en vez de repetir el mismo trato de errores
 * y el mismo apagado de botones dos veces.
 */
async function decidir(tarjeta, ruta, cuerpo, textoDeExito, textoDeFallo) {
    const botones = tarjeta.querySelectorAll(".btn-card");
    botones.forEach((b) => (b.disabled = true));

    try {
        // El 409 que puede llegar aquí es «alguien ya revisó esa solicitud»,
        // y lo cuenta el propio servidor; por eso se muestra su mensaje.
        const { ok } = await pedirAlServidor(
            ruta,
            { aviso: "applications-message", metodo: "PATCH", cuerpo },
            textoDeFallo
        );

        if (!ok) return;

        // La solicitud deja de estar pendiente, así que desaparece de la
        // bandeja. Se recarga para que la lista diga la verdad.
        //
        // El aviso va DESPUÉS y se espera a la recarga a propósito: recargar
        // empieza por limpiar el aviso de la sección, así que un mensaje
        // puesto antes se borraba solo y la aprobación parecía no hacer nada.
        await cargarSolicitudes();

        // Los números del resumen también cambian al aprobar: hay un gestor
        // activo más y un ciudadano menos. Ese escribe en su propio aviso.
        cargarResumen();

        mostrarMensaje("applications-message", textoDeExito, "success");
    } finally {
        botones.forEach((b) => (b.disabled = false));
    }
}

// ============================================================
// SECCIÓN: CREAR GESTOR
// ============================================================

/**
 * Los campos del formulario y lo que el servidor exige de cada uno.
 *
 * Los mínimos y máximos son los mismos de CreateManagerDto. Se comprueban
 * aquí además de allá porque los avisos que devuelve la validación de ASP.NET
 * vienen en inglés («The field Name must be a string...»), y eso no se le
 * puede poner delante a nadie. El servidor sigue siendo el que manda: esto
 * solo evita el viaje y escribe el aviso en español.
 */
const CAMPOS_DEL_GESTOR = [
    { id: "managerName", campo: "name", etiqueta: "El nombre", min: 2, max: 70 },
    { id: "managerLastName", campo: "lastName", etiqueta: "El apellido", min: 2, max: 70 },
    { id: "managerEmail", campo: "email", etiqueta: "El correo", min: 5, max: 100, correo: true },
    { id: "managerDocumentType", campo: "idDocumentType", etiqueta: "El tipo de documento", min: 1, max: 40 },
    { id: "managerDocumentNumber", campo: "documentNumber", etiqueta: "El número de documento", min: 2, max: 20 },
    { id: "managerPhone", campo: "phoneNumber", etiqueta: "El teléfono", min: 7, max: 20 },
    { id: "managerAddress", campo: "address", etiqueta: "La dirección", min: 5, max: 200 },
];

/**
 * Llena el desplegable de tipos de documento desde la API.
 *
 * No se escriben a mano en el HTML a propósito: los identificadores son
 * GUID del servidor y, si el catálogo cambia, unas opciones fijas apuntarían
 * a algo que ya no existe. Se comprobó que pasa: al quitar «Tarjeta de
 * identidad» quedó una opción muerta en el registro.
 */
async function cargarTiposDeDocumento() {
    const select = document.getElementById("managerDocumentType");
    if (!select) return;

    const { ok, datos } = await pedirAlServidor(
        "/DocumentType/GetDocumentTypes",
        { aviso: "create-message" },
        "No se pudieron cargar los tipos de documento."
    );

    if (!ok || !Array.isArray(datos)) {
        select.innerHTML = `<option value="">No se pudieron cargar</option>`;
        return;
    }

    select.innerHTML = `<option value="">Selecciona...</option>` + datos
        .map((t) => `<option value="${escapeHtml(t.idDocumentType)}">${escapeHtml(t.documentName)}</option>`)
        .join("");
}

/** Recoge lo escrito y devuelve { datos } o { error } con el primer fallo. */
function leerFormularioDeGestor() {
    const datos = {};

    for (const { id, campo, etiqueta, min, max, correo } of CAMPOS_DEL_GESTOR) {
        const valor = (document.getElementById(id)?.value ?? "").trim();

        if (valor.length < min) {
            return {
                error: min === 1
                    ? `${etiqueta} es obligatorio.`
                    : `${etiqueta} debe tener al menos ${min} caracteres.`,
            };
        }

        if (valor.length > max) {
            return { error: `${etiqueta} no puede pasar de ${max} caracteres.` };
        }

        // Comprobación mínima, la de verdad la hace el servidor con
        // [EmailAddress]. Aquí solo se atrapa el despiste evidente.
        if (correo && !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(valor)) {
            return { error: "El correo no tiene un formato válido." };
        }

        datos[campo] = valor;
    }

    return { datos };
}

async function crearGestor(evento) {
    evento.preventDefault();

    const { datos, error } = leerFormularioDeGestor();

    if (error) {
        mostrarMensaje("create-message", error);
        return;
    }

    const boton = document.getElementById("createManagerBtn");
    const textoOriginal = boton.textContent;
    boton.disabled = true;
    boton.textContent = "Creando...";

    try {
        const { ok, datos: respuesta } = await pedirAlServidor(
            "/Admin/CreateManager",
            { aviso: "create-message", metodo: "POST", cuerpo: datos },
            "No se pudo crear el gestor."
        );

        if (!ok) return;

        // El formulario se vacía porque la cuenta ya existe: dejarlo lleno
        // invita a darle otra vez y chocar con «el correo ya está registrado».
        //
        // reset() deja el desplegable en «Selecciona...» pero no borra sus
        // opciones, así que no hace falta volver a pedirle el catálogo al
        // servidor: sería una petición que no cambia nada.
        document.getElementById("createManagerForm").reset();

        // Hay un gestor activo más, así que los números del resumen cambian.
        cargarResumen();

        mostrarMensaje(
            "create-message",
            respuesta?.message ?? "Gestor creado. Se le envió un código a su correo.",
            "success"
        );
    } finally {
        boton.disabled = false;
        boton.textContent = textoOriginal;
    }
}

document.getElementById("createManagerForm")?.addEventListener("submit", crearGestor);

// ============================================================
// UTILIDADES DE LA PANTALLA
// ============================================================

/**
 * Habla con el servidor y deja puesto el aviso si algo sale mal.
 *
 * Devuelve { ok, datos } y no el cuerpo a secas a propósito: hay llamadas
 * que responden 200 sin nada dentro —aprobar una solicitud, por ejemplo— y
 * con el cuerpo pelado no había forma de distinguir «salió bien y no manda
 * nada» de «falló». Con ok esa duda desaparece.
 *
 * Así cada sección se ocupa solo de lo suyo —mirar que los datos tengan
 * sentido y pintarlos— sin repetir el mismo bloque de sesión caducada,
 * error del servidor y falta de red en cada una.
 */
async function pedirAlServidor(ruta, { aviso, cargando = null, metodo = "GET", cuerpo = null }, textoDeFallo) {
    mostrarMensaje(aviso, "");
    if (cargando) alternarCargando(cargando, true);

    try {
        const respuesta = await fetch(`${API_BASE}${ruta}`, {
            method: metodo,
            headers: authHeaders(),
            body: cuerpo ? JSON.stringify(cuerpo) : undefined,
        });

        // La sesión caduca a las dos horas. Sin este caso el aviso era el
        // genérico, que no le dice a nadie que lo que tiene que hacer es
        // volver a entrar. Se comprobó: un 401 llega sin cuerpo, así que no
        // hay ningún mensaje del servidor que rescatar.
        // La sesión caduca a las dos horas. Sin este caso el aviso era el
        // genérico, que no le dice a nadie que lo que tiene que hacer es
        // volver a entrar. Se comprobó: un 401 llega sin cuerpo, así que no
        // hay ningún mensaje del servidor que rescatar.
        if (respuesta.status === 401) {
            mostrarMensaje(aviso, "Tu sesión caducó. Vuelve a iniciar sesión.");
            return { ok: false };
        }

        const respondio = await leerCuerpo(respuesta);

        if (!respuesta.ok) {
            mostrarMensaje(aviso, mensajeDeError(respondio, textoDeFallo));
            return { ok: false };
        }

        return { ok: true, datos: respondio };
    } catch {
        // Aquí solo se cae si el servidor no contesta: un error de red, la API
        // apagada o CORS. El mensaje lo dice en ese idioma, no en el del backend.
        mostrarMensaje(aviso, "No se pudo conectar con el servidor.");
        return { ok: false };
    } finally {
        if (cargando) alternarCargando(cargando, false);
    }
}

/**
 * Muestra un aviso y lo borra solo a los 4 segundos.
 *
 * Usa las clases .msg del panel del ciudadano porque es la hoja que esta
 * página enlaza. El panel del gestor tiene las suyas con .form-hint, pero
 * esa hoja no se carga aquí y el aviso saldría sin ningún estilo.
 */
// Un reloj de borrado por cada contenedor de avisos. Sin esto, el reloj del
// aviso anterior borraba el siguiente antes de tiempo: se midió que el
// segundo mensaje duraba 1,8 segundos en pantalla en vez de 4.
const relojesDeAviso = new Map();

function mostrarMensaje(id, texto, tipo = "error") {
    const el = document.getElementById(id);
    if (!el) return;

    clearTimeout(relojesDeAviso.get(id));

    if (!texto) {
        el.textContent = "";
        el.className = "";
        return;
    }

    el.textContent = texto;
    el.className = `msg ${tipo}`;

    relojesDeAviso.set(id, setTimeout(() => {
        el.textContent = "";
        el.className = "";
    }, 4000));
}

function alternarCargando(id, visible) {
    const el = document.getElementById(id);
    if (el) el.style.display = visible ? "block" : "none";
}

// ============================================================
// PRIMERA CARGA
// ============================================================

// La pestaña que se ve al entrar es el resumen, así que se pide ya.
cargarResumen();

// El desplegable de tipos de documento se llena desde el principio: la
// pestaña de crear gestor no avisa al abrirse y el campo diría «Cargando...»
// hasta que alguien lo tocara.
cargarTiposDeDocumento();
