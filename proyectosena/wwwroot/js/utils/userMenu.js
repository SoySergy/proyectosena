import { API_BASE, authHeaders } from "../services/api.js";

/**
 * userMenu.js
 * Cabecera de usuario compartida por los paneles: saludo, correo,
 * desplegable y cierre de sesión.
 *
 * Este bloque estaba copiado igual en el panel del ciudadano y en el del
 * gestor. Al llegar el del administrador iban a ser tres copias de lo mismo,
 * así que vive aquí una sola vez.
 *
 * Los textos se escriben con textContent, nunca con innerHTML: el nombre y el
 * correo los teclea la persona al registrarse y por ahí se colaba código.
 */

const LOGIN_URL = "/pages/auth/login.html";

/**
 * Conecta la cabecera de la página y devuelve el usuario guardado.
 * La página necesita #welcomeMsg, #userEmail, #userMenuTrigger,
 * #userDropdown y #logoutBtn.
 */
export function initUserMenu() {
    const user = leerUsuario();

    mostrarIdentidad(user);
    conectarDesplegable();
    conectarCierreDeSesion();

    return user;
}

function leerUsuario() {
    try {
        return JSON.parse(localStorage.getItem("user"));
    } catch {
        // Dato corrupto: mejor una cabecera vacía que una página en blanco.
        return null;
    }
}

function mostrarIdentidad(user) {
    if (!user) return;

    const saludo = document.getElementById("welcomeMsg");
    const correo = document.getElementById("userEmail");

    if (saludo) saludo.textContent = `Hola, ${user.name} ${user.lastName}`;
    if (correo) correo.textContent = user.email ?? "";
}

function conectarDesplegable() {
    const trigger = document.getElementById("userMenuTrigger");
    const dropdown = document.getElementById("userDropdown");

    if (!trigger || !dropdown) return;

    trigger.addEventListener("click", (e) => {
        e.stopPropagation();
        const abierto = dropdown.classList.toggle("is-open");
        trigger.setAttribute("aria-expanded", abierto);

        // El panel de avisos cae en el mismo sitio de la cabecera y los dos
        // se tapaban al quedar abiertos a la vez.
        if (abierto) cerrarPanelDeAvisos();
    });

    // Cerrar al hacer clic fuera
    document.addEventListener("click", () => {
        dropdown.classList.remove("is-open");
        trigger.setAttribute("aria-expanded", "false");
    });

    // Evitar que los clics de dentro lo cierren
    dropdown.addEventListener("click", (e) => e.stopPropagation());
}

function cerrarPanelDeAvisos() {
    const panel = document.getElementById("notifPanel");
    const boton = document.getElementById("notifTrigger");

    if (panel) panel.classList.remove("is-open");
    if (boton) boton.setAttribute("aria-expanded", "false");
}

function conectarCierreDeSesion() {
    const boton = document.getElementById("logoutBtn");
    if (!boton) return;

    boton.addEventListener("click", async () => {
        // Primero se le avisa al servidor, y después se borra el token de aquí.
        //
        // Sin este aviso, cerrar sesión solo lo olvidaba este navegador: el
        // token seguía sirviendo hasta una hora, así que quien lo tuviera
        // copiado entraba sin contraseña. Se comprobó guardando un token,
        // cerrando sesión y volviéndolo a usar: el servidor respondía 200.
        await avisarAlServidor();

        localStorage.removeItem("token");
        localStorage.removeItem("user");
        window.location.href = LOGIN_URL;
    });
}

/**
 * Le pide al servidor que anule el token actual.
 *
 * Si falla —no hay red, el servidor está caído— no se detiene el cierre de
 * sesión: se sigue borrando el token de este navegador, que es lo que la
 * persona pidió. Quedarse dentro porque el aviso no salió sería peor.
 */
async function avisarAlServidor() {
    const token = localStorage.getItem("token");
    if (!token) return;

    try {
        await fetch(`${API_BASE}/Auth/Logout`, {
            method: "POST",
            headers: authHeaders(),
        });
    } catch {
        // Sin conexión no hay nada que hacer: el token caducará por su cuenta.
    }
}
