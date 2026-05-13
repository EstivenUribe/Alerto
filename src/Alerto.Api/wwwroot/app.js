// ─────────────────────────────────────────────────────────── STATE ──────────
const state = {
  accessToken:  localStorage.getItem("alerto.accessToken")  || "",
  refreshToken: localStorage.getItem("alerto.refreshToken") || "",
  username:     localStorage.getItem("alerto.username")     || "",
  role:         localStorage.getItem("alerto.role")         || "",
  citizenGeofenceId: localStorage.getItem("alerto.geofenceId") || "",
  activePanel: null,
  geofences: [],
  // Leaflet maps (lazy init)
  weatherMapMain: null,
  weatherMapOp:   null,
  geofenceMapL:   null,
  zoneMapL:       null,
  zoneMarkers:    [],
  // Weather panel geofence overlays: [{id, layer}]
  weatherGeofenceLayers: [],
  weatherUserMarker: null,
  // Geofence draw map
  drawMap:        null,
  drawPoints:     [],
  drawMarkers:    [],
  drawPolyline:   null,
  drawPolygon:    null,
};

const RISK_COLORS = {
  // Niveles de riesgo meteorológico (español — devuelto por la API)
  Bajo:     "#087f5b",
  Moderado: "#e6a817",
  Alto:     "#e06000",
  Crítico:  "#a61e2b",
  // Severidad de alertas (inglés — enum del dominio)
  Low:      "#087f5b",
  Moderate: "#e6a817",
  High:     "#e06000",
  Severe:   "#e06000",
  Critical: "#a61e2b",
};

const SEVERITY_META = {
  Low:      { label: "Bajo",     tip: "Bajo: situación controlada, sin impacto inmediato sobre la comunidad." },
  Moderate: { label: "Moderado", tip: "Moderado: puede afectar actividades al aire libre; mantener precaución." },
  High:     { label: "Alto",     tip: "Alto: impacto significativo probable; activar protocolos de emergencia." },
  Severe:   { label: "Severo",   tip: "Severo: riesgo real de daño; ejecutar acciones preventivas de inmediato." },
  Critical: { label: "Crítico",  tip: "Crítico: peligro inminente para personas o infraestructura; posible evacuación." },
};

const STATUS_META = {
  Pending:     { label: "Pendiente",  tip: "Pendiente: recibida, esperando revisión de un operador." },
  Approved:    { label: "Aprobada",   tip: "Aprobada: validada por un operador y activa en el sistema." },
  Rejected:    { label: "Rechazada",  tip: "Rechazada: descartada por no cumplir los criterios de validación." },
  Broadcasted: { label: "Difundida",  tip: "Difundida: transmitida a los canales de notificación de la comunidad." },
  Cancelled:   { label: "Cancelada",  tip: "Cancelada: anulada después de su aprobación." },
};

// Normaliza nivel de riesgo al español (por si la caché devuelve el valor en inglés)
const RISK_ES = {
  Low: "Bajo", Moderate: "Moderado", High: "Alto", Critical: "Crítico",
  Bajo: "Bajo", Moderado: "Moderado", Alto: "Alto", Crítico: "Crítico",
};

const RISK_META = {
  Bajo:     "Bajo: sin precipitación significativa en este momento.",
  Moderado: "Moderado: lluvia ligera presente o alta probabilidad de lluvia próxima.",
  Alto:     "Alto: lluvia moderada a intensa; evitar zonas de riesgo de inundación.",
  Crítico:  "Crítico: precipitación muy intensa; activar protocolos de emergencia.",
};

const ROLE_META = {
  Admin:       { label: "Administrador", tip: "Administrador: gestiona usuarios, geocercas y toda la plataforma." },
  Operator:    { label: "Operador",      tip: "Operador: revisa y aprueba alertas ciudadanas." },
  Citizen:     { label: "Ciudadano",     tip: "Ciudadano: reporta emergencias y consulta alertas de su zona." },
  Analyst:     { label: "Analista",      tip: "Analista: analiza datos y puede aprobar alertas." },
  Auditor:     { label: "Auditor",       tip: "Auditor: acceso de solo lectura para revisión y control." },
  RulesEngine: { label: "Motor de Reglas", tip: "Motor de Reglas: cliente automático que genera alertas por integración." },
};

function severityBadge(severity) {
  const m = SEVERITY_META[severity] || { label: severity, tip: "" };
  return `<span class="badge ${severity}" title="${m.tip}">${m.label}</span>`;
}

function statusBadge(status) {
  const m = STATUS_META[status] || { label: status, tip: "" };
  return `<span class="badge ${status}" title="${m.tip}">${m.label}</span>`;
}

function roleBadge(role) {
  const m = ROLE_META[role] || { label: role, tip: "" };
  return `<span class="badge ${role}" title="${m.tip}">${m.label}</span>`;
}

function roleLabel(role) {
  return ROLE_META[role]?.label ?? role;
}

const DEMO_CREDENTIALS = {
  admin:    { username: "admin",    password: "AlertoAdmin123!" },
  operador: { username: "operador", password: "Alerto2026!" },
  ciudadano:{ username: "ciudadano",password: "Alerto2026!" },
};

// Navigation config per role
const NAV = {
  Admin: [
    { id: "alerts",     label: "Alertas",      emoji: "🔔", onEnter: loadAlerts },
    { id: "newAlert",   label: "Nueva alerta", emoji: "➕", onEnter: loadGeofencesSelect },
    { id: "users",      label: "Usuarios",     emoji: "👥", onEnter: loadUsers },
    { id: "geofences",  label: "Geocercas",    emoji: "🗺️", onEnter: loadGeofences },
    { id: "weather",    label: "Clima",        emoji: "🌦️", onEnter: initWeatherPanel },
  ],
  Operator: [
    { id: "pending",    label: "Pendientes",   emoji: "⏳", onEnter: loadPendingAlerts },
    { id: "alerts",     label: "Alertas",      emoji: "🔔", onEnter: loadAlerts },
    { id: "weather",    label: "Clima",        emoji: "🌦️", onEnter: initWeatherPanel },
  ],
  Citizen: [
    { id: "myzone",     label: "Mi zona",      emoji: "📍", onEnter: loadMyZonePanel },
    { id: "report",     label: "Reportar",     emoji: "📢", onEnter: loadReportPanel },
    { id: "weather",    label: "Clima",        emoji: "🌦️", onEnter: initWeatherPanel },
  ],
};

// ──────────────────────────────────────────────────────── HTTP HELPERS ───────
function authHeaders() {
  return state.accessToken ? { Authorization: `Bearer ${state.accessToken}` } : {};
}

async function request(path, options = {}) {
  const headers = {
    Accept: "application/json",
    ...authHeaders(),
    ...(options.headers || {}),
  };
  if (options.body && !headers["Content-Type"]) {
    headers["Content-Type"] = "application/json";
  }
  const res = await fetch(path, { ...options, headers });
  const text = await res.text();
  const payload = text ? JSON.parse(text) : null;
  if (!res.ok) {
    throw new Error(payload?.detail || payload?.title || `HTTP ${res.status}`);
  }
  return payload;
}

let toastTimer;
function showToast(msg, isError = false) {
  const el = document.getElementById("toast");
  el.textContent = msg;
  el.style.background = isError ? "var(--danger)" : "var(--navy)";
  el.classList.add("visible");
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => el.classList.remove("visible"), 3400);
}

// ──────────────────────────────────────────────────── SESSION / SCREENS ──────
function saveSession(payload) {
  state.accessToken  = payload.accessToken  || "";
  state.refreshToken = payload.refreshToken || "";
  state.username     = payload.username     || "";
  state.role         = payload.role         || "";
  localStorage.setItem("alerto.accessToken",  state.accessToken);
  localStorage.setItem("alerto.refreshToken", state.refreshToken);
  localStorage.setItem("alerto.username",     state.username);
  localStorage.setItem("alerto.role",         state.role);
}

function clearSession() {
  state.accessToken = state.refreshToken = state.username = state.role = "";
  localStorage.removeItem("alerto.accessToken");
  localStorage.removeItem("alerto.refreshToken");
  localStorage.removeItem("alerto.username");
  localStorage.removeItem("alerto.role");
}

function showLoginScreen() {
  document.getElementById("loginScreen").style.display = "";
  document.getElementById("appShell").style.display = "none";
}

function showAppShell() {
  document.getElementById("loginScreen").style.display = "none";
  document.getElementById("appShell").style.display = "";
  buildSidebar();
  const firstNav = NAV[state.role]?.[0];
  if (firstNav) navigate(firstNav.id);
}

// ──────────────────────────────────────────────────────── NAVIGATION ─────────
function buildSidebar() {
  const items = NAV[state.role] || [];
  document.getElementById("sidebarNavList").innerHTML = items.map(item => `
    <li>
      <button class="nav-item" data-panel="${item.id}" onclick="navigate('${item.id}')">
        <span class="nav-emoji">${item.emoji}</span>
        <span>${item.label}</span>
      </button>
    </li>
  `).join("");

  const avatar = (state.username || "?").charAt(0).toUpperCase();
  document.getElementById("sidebarAvatar").textContent = avatar;
  document.getElementById("sidebarUsername").textContent = state.username || "Usuario";
  document.getElementById("sidebarRole").textContent = state.role || "—";
}

function navigate(panelId) {
  state.activePanel = panelId;

  // Hide all panels
  document.querySelectorAll(".panel-section").forEach(el => el.style.display = "none");

  // Show target
  const target = document.getElementById(`panel-${panelId}`);
  if (target) target.style.display = "";

  // Update nav active state
  document.querySelectorAll(".nav-item").forEach(btn => {
    btn.classList.toggle("active", btn.dataset.panel === panelId);
  });

  // Lazy map init
  if (panelId === "geofences" && !state.geofenceMapL) {
    state.geofenceMapL = initBaseMap("geofenceMap");
  }
  if (panelId === "myzone" && !state.zoneMapL) {
    state.zoneMapL = initBaseMap("myzoneMap");
  }

  // Trigger panel data
  const item = (NAV[state.role] || []).find(i => i.id === panelId);
  if (item?.onEnter) item.onEnter();
}

// ──────────────────────────────────────────────────────────── AUTH ────────────
async function login(username, password) {
  try {
    const data = await request("/api/v1/auth/login", {
      method: "POST",
      body: JSON.stringify({ username, password }),
    });
    saveSession(data);
    showAppShell();
  } catch (err) {
    showToast(err.message, true);
  }
}

async function logout() {
  if (state.refreshToken) {
    try {
      await request("/api/v1/auth/logout", {
        method: "POST",
        body: JSON.stringify({ refreshToken: state.refreshToken }),
      });
    } catch {}
  }
  clearSession();
  showLoginScreen();
}

// ─────────────────────────────────────────────────────── ALERTS PANEL ────────
async function loadAlerts() {
  const status = document.getElementById("alertStatusFilter")?.value || "";
  try {
    const params = new URLSearchParams({ pageSize: 50 });
    if (status) params.set("status", status);
    const data = await request(`/api/v1/alerts?${params}`);
    renderAlertsTable(data.items || []);
  } catch (err) {
    showToast(err.message, true);
  }
}

function renderAlertsTable(alerts) {
  const tbody = document.getElementById("alertsTableBody");
  if (!alerts.length) {
    tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;color:var(--muted);padding:32px">Sin resultados</td></tr>`;
    return;
  }
  const canApprove = state.role === "Admin" || state.role === "Operator";
  const isAdmin = state.role === "Admin";

  tbody.innerHTML = alerts.map(a => `
    <tr>
      <td><strong>${escapeHtml(a.title)}</strong></td>
      <td>${severityBadge(a.severity)}</td>
      <td>${statusBadge(a.status)}</td>
      <td><code style="font-size:0.78rem">${a.geofenceId ? a.geofenceId.slice(0,8) + "…" : "—"}</code></td>
      <td style="white-space:nowrap">${formatDate(a.createdAtUtc)}</td>
      <td>
        <div class="row-actions">
          <button class="ghost-button" onclick="showAlertDetail('${a.id}')">Ver</button>
          ${canApprove && a.status === "Pending" ? `
            <button class="secondary-button" onclick="approveAlert('${a.id}',${a.version})">Aprobar</button>
            <button class="ghost-button" onclick="rejectAlert('${a.id}',${a.version})">Rechazar</button>
          ` : ""}
          ${isAdmin ? `<button class="danger-button" onclick="deleteAlert('${a.id}',${a.version})">Eliminar</button>` : ""}
        </div>
      </td>
    </tr>
  `).join("");
}

async function showAlertDetail(id) {
  try {
    const alert = await request(`/api/v1/alerts/${id}`);
    document.getElementById("alertDetailTitle").textContent = alert.title;
    document.getElementById("alertDetailPre").textContent = JSON.stringify(alert, null, 2);
    document.getElementById("alertDetailBox").style.display = "";

    // Load confirmations for operator/admin
    if (state.role === "Admin" || state.role === "Operator") {
      try {
        const confirmations = await request(`/api/v1/alerts/${id}/citizen-confirmations`);
        const section = document.getElementById("confirmationsSection");
        const list = document.getElementById("confirmationsList");
        section.style.display = "";
        if (!confirmations.length) {
          list.innerHTML = `<p style="color:var(--muted);margin:0">Sin confirmaciones ciudadanas aún.</p>`;
        } else {
          list.innerHTML = confirmations.map(c => `
            <div class="confirmation-item">
              <span class="conf-user">${escapeHtml(c.confirmedByUserId)}</span>
              <span class="conf-time">${formatDate(c.confirmedAtUtc)}</span>
              ${c.notes ? `<span class="conf-notes">${escapeHtml(c.notes)}</span>` : ""}
            </div>
          `).join("");
        }
      } catch {}
    }

    document.getElementById("alertDetailBox").scrollIntoView({ behavior: "smooth", block: "nearest" });
  } catch (err) {
    showToast(err.message, true);
  }
}

async function approveAlert(id, version) {
  if (!confirm("¿Aprobar esta alerta?")) return;
  try {
    await request(`/api/v1/alerts/${id}/approve`, {
      method: "POST",
      body: JSON.stringify({ expectedVersion: version }),
    });
    showToast("Alerta aprobada.");
    loadAlerts();
  } catch (err) { showToast(err.message, true); }
}

async function rejectAlert(id, version) {
  const reason = prompt("Motivo del rechazo:");
  if (!reason) return;
  try {
    await request(`/api/v1/alerts/${id}/reject`, {
      method: "POST",
      body: JSON.stringify({ expectedVersion: version, reason }),
    });
    showToast("Alerta rechazada.");
    loadAlerts();
  } catch (err) { showToast(err.message, true); }
}

async function deleteAlert(id, version) {
  const reason = prompt("Motivo de eliminación (admin):");
  if (!reason) return;
  try {
    await request(`/api/v1/alerts/${id}`, {
      method: "DELETE",
      body: JSON.stringify({ expectedVersion: version, reason }),
    });
    showToast("Alerta eliminada.");
    loadAlerts();
  } catch (err) { showToast(err.message, true); }
}

// ─────────────────────────────────────────────── NEW ALERT (Admin/Operator) ──
async function loadGeofencesSelect() {
  if (state.geofences.length === 0) await fetchGeofences();
  populateGeofenceSelect("geofenceSelect");
}

function populateGeofenceSelect(selectId) {
  const sel = document.getElementById(selectId);
  if (!sel) return;
  sel.innerHTML = state.geofences.map(g =>
    `<option value="${g.id}">${escapeHtml(g.name)} (${escapeHtml(g.code)})</option>`
  ).join("");
}

async function submitCreateAlert(e) {
  e.preventDefault();
  const body = {
    title:       document.getElementById("alertTitle").value,
    description: document.getElementById("alertDescription").value,
    severity:    document.getElementById("alertSeverity").value,
    sourceSystem:"PANEL_OPERATIVO",
    address:     document.getElementById("alertAddress").value,
    latitude:    parseFloat(document.getElementById("alertLatitude").value),
    longitude:   parseFloat(document.getElementById("alertLongitude").value),
    geofenceId:  document.getElementById("geofenceSelect").value,
  };
  try {
    await request("/api/v1/alerts", { method: "POST", body: JSON.stringify(body) });
    showToast("Alerta creada exitosamente.");
    e.target.reset();
  } catch (err) { showToast(err.message, true); }
}

// ──────────────────────────────────────────────── PENDING ALERTS (Operator) ──
async function loadPendingAlerts() {
  try {
    const data = await request("/api/v1/alerts?status=Pending&pageSize=50");
    const list = document.getElementById("pendingAlertsList");
    const noMsg = document.getElementById("noPendingMsg");
    const items = data.items || [];
    if (!items.length) {
      list.innerHTML = "";
      noMsg.style.display = "";
      return;
    }
    noMsg.style.display = "none";
    list.innerHTML = items.map(a => `
      <div class="alert-card">
        <div class="alert-card-top">
          <span class="alert-card-title">${escapeHtml(a.title)}</span>
          <div style="display:flex;gap:8px">
            ${severityBadge(a.severity)}
            ${statusBadge("Pending")}
          </div>
        </div>
        <div class="alert-card-meta">${formatDate(a.createdAtUtc)} · ${escapeHtml(a.sourceSystem || "")}</div>
        <div class="alert-card-desc">${escapeHtml(a.description)}</div>
        <div class="alert-card-actions">
          <button class="secondary-button" onclick="approvePending('${a.id}',${a.version})">✔ Aprobar</button>
          <button class="ghost-button" onclick="rejectPending('${a.id}',${a.version})">✘ Rechazar</button>
          <button class="ghost-button" onclick="showAlertDetail('${a.id}')">Ver detalle</button>
        </div>
      </div>
    `).join("");
    // Make detail box accessible from pending panel
    document.getElementById("panel-alerts").style.display = "none";
  } catch (err) { showToast(err.message, true); }
}

async function approvePending(id, version) {
  if (!confirm("¿Aprobar esta alerta?")) return;
  try {
    await request(`/api/v1/alerts/${id}/approve`, {
      method: "POST",
      body: JSON.stringify({ expectedVersion: version }),
    });
    showToast("Alerta aprobada y lista para difusión.");
    loadPendingAlerts();
  } catch (err) { showToast(err.message, true); }
}

async function rejectPending(id, version) {
  const reason = prompt("Motivo del rechazo:");
  if (!reason) return;
  try {
    await request(`/api/v1/alerts/${id}/reject`, {
      method: "POST",
      body: JSON.stringify({ expectedVersion: version, reason }),
    });
    showToast("Alerta rechazada.");
    loadPendingAlerts();
  } catch (err) { showToast(err.message, true); }
}

// ──────────────────────────────────────────────────── USERS (Admin) ──────────
async function loadUsers() {
  const search = document.getElementById("userSearchInput")?.value || "";
  const role   = document.getElementById("userRoleFilter")?.value  || "";
  try {
    const params = new URLSearchParams({ pageSize: 50 });
    if (search) params.set("search", search);
    if (role)   params.set("role", role);
    const data = await request(`/api/v1/users?${params}`);
    renderUsersTable(data.items || []);
  } catch (err) { showToast(err.message, true); }
}

function renderUsersTable(users) {
  const tbody = document.getElementById("usersTableBody");
  if (!users.length) {
    tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;color:var(--muted);padding:32px">Sin resultados</td></tr>`;
    return;
  }
  tbody.innerHTML = users.map(u => `
    <tr>
      <td><code>${escapeHtml(u.username)}</code></td>
      <td>${escapeHtml(u.displayName)}</td>
      <td style="font-size:0.82rem">${escapeHtml(u.email)}</td>
      <td>${roleBadge(u.role)}</td>
      <td><span class="badge ${u.isActive ? "badge-success" : "badge-default"}">${u.isActive ? "Activo" : "Inactivo"}</span></td>
      <td>
        <div class="row-actions">
          <select onchange="changeUserRole('${u.id}',${u.version},this)" style="min-height:34px;font-size:0.8rem;padding:4px 8px"
            title="Cambiar el rol del usuario">
            ${["Admin","Operator","Citizen","Analyst","Auditor"].map(r =>
              `<option ${r === u.role ? "selected" : ""} value="${r}">${roleLabel(r)}</option>`
            ).join("")}
          </select>
          ${u.isActive
            ? `<button class="ghost-button" onclick="deactivateUser('${u.id}',${u.version})">Desactivar</button>`
            : `<button class="secondary-button" onclick="activateUser('${u.id}',${u.version})">Activar</button>`}
        </div>
      </td>
    </tr>
  `).join("");
}

async function changeUserRole(id, version, selectEl) {
  const newRole = selectEl.value;
  if (!confirm(`¿Cambiar rol a ${roleLabel(newRole)}?`)) { loadUsers(); return; }
  try {
    const current = await request(`/api/v1/users/${id}`);
    await request(`/api/v1/users/${id}`, {
      method: "PUT",
      body: JSON.stringify({
        displayName: current.displayName,
        email: current.email,
        role: newRole,
        expectedVersion: version,
      }),
    });
    showToast(`Rol actualizado a ${roleLabel(newRole)}.`);
    loadUsers();
  } catch (err) { showToast(err.message, true); loadUsers(); }
}

async function deactivateUser(id, version) {
  const reason = prompt("Motivo de desactivación:");
  if (!reason) return;
  try {
    await request(`/api/v1/users/${id}/deactivate`, {
      method: "POST",
      body: JSON.stringify({ expectedVersion: version, reason }),
    });
    showToast("Usuario desactivado.");
    loadUsers();
  } catch (err) { showToast(err.message, true); }
}

async function activateUser(id, version) {
  try {
    await request(`/api/v1/users/${id}/activate`, {
      method: "POST",
      body: JSON.stringify({ expectedVersion: version, reason: "Reactivación manual." }),
    });
    showToast("Usuario activado.");
    loadUsers();
  } catch (err) { showToast(err.message, true); }
}

async function submitCreateUser() {
  const body = {
    username:    document.getElementById("newUsername").value.trim(),
    displayName: document.getElementById("newDisplayName").value.trim(),
    email:       document.getElementById("newEmail").value.trim(),
    password:    document.getElementById("newPassword").value,
    role:        document.getElementById("newRole").value,
  };
  if (!body.username || !body.displayName || !body.email || !body.password) {
    showToast("Completa todos los campos.", true); return;
  }
  try {
    await request("/api/v1/users", { method: "POST", body: JSON.stringify(body) });
    showToast(`Usuario ${body.username} creado.`);
    document.getElementById("createUserForm").style.display = "none";
    loadUsers();
  } catch (err) { showToast(err.message, true); }
}

// ─────────────────────────────────────────────────── GEOFENCES (Admin) ───────
async function fetchGeofences() {
  try {
    const data = await request("/api/v1/geofences?pageSize=100");
    state.geofences = data.items || [];
  } catch {}
}

async function loadGeofences() {
  if (state.geofences.length === 0) await fetchGeofences();
  renderGeofencesTable(state.geofences);
}

function renderGeofencesTable(geofences) {
  const tbody = document.getElementById("geofencesTableBody");
  if (!geofences.length) {
    tbody.innerHTML = `<tr><td colspan="5" style="text-align:center;color:var(--muted);padding:32px">Sin geocercas</td></tr>`;
    return;
  }
  tbody.innerHTML = geofences.map(g => `
    <tr>
      <td><code>${escapeHtml(g.code)}</code></td>
      <td><strong>${escapeHtml(g.name)}</strong></td>
      <td>${escapeHtml(g.neighborhood)}</td>
      <td><span class="badge ${g.isActive ? "badge-success" : "badge-default"}">${g.isActive ? "Activa" : "Inactiva"}</span></td>
      <td>
        <button class="ghost-button" onclick="showGeofenceOnMap('${escapeHtml(g.name)}','${escapeHtml(g.polygonWkt || "")}')">🗺️ Ver</button>
      </td>
    </tr>
  `).join("");
}

function showGeofenceOnMap(name, wkt) {
  document.getElementById("geofenceMapTitle").textContent = name;
  const coords = parseWktPolygon(wkt);
  if (!coords || !state.geofenceMapL) return;
  // Remove old layers
  state.geofenceMapL.eachLayer(l => { if (l instanceof L.Polygon) state.geofenceMapL.removeLayer(l); });
  const poly = L.polygon(coords, { color: "#06264a", weight: 2, fillOpacity: 0.15 })
    .addTo(state.geofenceMapL);
  state.geofenceMapL.fitBounds(poly.getBounds(), { padding: [20, 20] });
}

// ─────────────────────────────────────────────────────── MY ZONE (Citizen) ───
async function loadMyZonePanel() {
  if (state.geofences.length === 0) await fetchGeofences();
  const sel = document.getElementById("citizenGeofenceSelect");
  if (sel) {
    sel.innerHTML = `<option value="">Selecciona tu zona...</option>` +
      state.geofences.map(g =>
        `<option value="${g.id}" ${g.id === state.citizenGeofenceId ? "selected" : ""}>${escapeHtml(g.name)}</option>`
      ).join("");
  }
  if (state.citizenGeofenceId) loadZoneAlerts();
  else {
    document.getElementById("zoneSelectHint").style.display = "";
    document.getElementById("zoneNoAlerts").style.display = "none";
  }
}

async function loadZoneAlerts() {
  const geoId = state.citizenGeofenceId;
  document.getElementById("zoneSelectHint").style.display = "none";
  if (!geoId) return;
  try {
    const data = await request(`/api/v1/alerts?geofenceId=${geoId}&status=Approved&pageSize=50`);
    const items = data.items || [];
    const noMsg = document.getElementById("zoneNoAlerts");
    const list  = document.getElementById("zoneAlertsList");

    // Clear old markers
    state.zoneMarkers.forEach(m => state.zoneMapL?.removeLayer(m));
    state.zoneMarkers = [];

    if (!items.length) {
      list.innerHTML = "";
      noMsg.style.display = "";
    } else {
      noMsg.style.display = "none";
      list.innerHTML = items.map(a => `
        <div class="alert-card">
          <div class="alert-card-top">
            <span class="alert-card-title">${escapeHtml(a.title)}</span>
            ${severityBadge(a.severity)}
          </div>
          <div class="alert-card-meta">${formatDate(a.createdAtUtc)} · ${escapeHtml(a.address || "")}</div>
          <div class="alert-card-desc">${escapeHtml(a.description)}</div>
        </div>
      `).join("");

      // Place markers on map
      items.forEach(a => {
        if (!a.latitude || !a.longitude || !state.zoneMapL) return;
        const color = RISK_COLORS[a.severity] || RISK_COLORS.Moderate;
        const marker = L.circleMarker([a.latitude, a.longitude], {
          radius: 10, color, fillColor: color, fillOpacity: 0.7, weight: 2,
        }).bindPopup(`<strong>${escapeHtml(a.title)}</strong><br>${a.severity}`);
        marker.addTo(state.zoneMapL);
        state.zoneMarkers.push(marker);
      });

      if (state.zoneMarkers.length) {
        const group = L.featureGroup(state.zoneMarkers);
        state.zoneMapL.fitBounds(group.getBounds(), { padding: [30, 30] });
      }
    }
  } catch (err) { showToast(err.message, true); }
}

// ─────────────────────────────────────────────────────── REPORT (Citizen) ────
async function loadReportPanel() {
  if (state.geofences.length === 0) await fetchGeofences();
  populateGeofenceSelect("citizenGeofence");
  loadActiveAlertsForConfirm();
}

async function loadActiveAlertsForConfirm() {
  try {
    const data = await request("/api/v1/alerts?status=Approved&pageSize=30");
    const container = document.getElementById("activeAlertsForConfirm");
    const items = data.items || [];
    if (!items.length) {
      container.innerHTML = `<p style="color:var(--muted);margin:0">No hay alertas aprobadas activas en este momento.</p>`;
      return;
    }
    container.innerHTML = items.map(a => `
      <div class="confirm-item">
        <div class="confirm-item-title">${escapeHtml(a.title)}</div>
        <div class="confirm-item-meta">
          ${severityBadge(a.severity)}
          &nbsp;${formatDate(a.createdAtUtc)}
        </div>
        <button class="secondary-button" onclick="citizenConfirm('${a.id}')" style="width:100%">
          ✅ Confirmo que veo esta alerta
        </button>
      </div>
    `).join("");
  } catch (err) { showToast(err.message, true); }
}

async function citizenConfirm(alertId) {
  const notes = prompt("Agrega una nota opcional (describe lo que ves):");
  if (notes === null) return; // cancelled
  try {
    await request(`/api/v1/alerts/${alertId}/citizen-confirm`, {
      method: "POST",
      body: JSON.stringify({ notes: notes || "" }),
    });
    showToast("¡Confirmación registrada! Gracias por reportar.");
  } catch (err) { showToast(err.message, true); }
}

async function submitCitizenAlert(e) {
  e.preventDefault();
  const body = {
    title:       document.getElementById("citizenTitle").value,
    description: document.getElementById("citizenDescription").value,
    severity:    document.getElementById("citizenSeverity").value,
    sourceSystem:"CITIZEN_REPORT",
    address:     document.getElementById("citizenAddress").value || "Sin referencia",
    latitude:    parseFloat(document.getElementById("citizenLat").value),
    longitude:   parseFloat(document.getElementById("citizenLon").value),
    geofenceId:  document.getElementById("citizenGeofence").value,
  };
  if (!body.geofenceId) { showToast("Selecciona tu zona.", true); return; }
  try {
    await request("/api/v1/alerts", { method: "POST", body: JSON.stringify(body) });
    showToast("¡Reporte enviado! Un operador lo revisará pronto.");
    e.target.reset();
  } catch (err) { showToast(err.message, true); }
}

// ──────────────────────────────────────────────────────── WEATHER ────────────
function initBaseMap(containerId) {
  const map = L.map(containerId).setView([6.2518, -75.5636], 12);
  L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
    attribution: "© OpenStreetMap",
    maxZoom: 18,
  }).addTo(map);
  return map;
}

function initLeafletMap(containerId, key) {
  const map = initBaseMap(containerId);
  if (key === "main") state.weatherMapMain = map;
  else if (key === "op") state.weatherMapOp = map;
  return map;
}

function updateWeatherMap(map, lat, lon, riskLevel, precipMm) {
  if (!map) return;
  const riskEs = RISK_ES[riskLevel] || riskLevel;
  const color  = RISK_COLORS[riskEs] || RISK_COLORS[riskLevel] || RISK_COLORS.Bajo;
  // Remove previous weather markers but preserve the user-location marker
  map.eachLayer(l => { if (l instanceof L.CircleMarker && l !== state.weatherUserMarker) map.removeLayer(l); });
  L.circleMarker([lat, lon], { radius: 14, color, fillColor: color, fillOpacity: 0.75, weight: 2 })
    .bindPopup(`<strong>${riskEs}</strong><br>${Number(precipMm).toFixed(2)} mm/h`)
    .addTo(map)
    .openPopup();
  map.setView([lat, lon], 13);
}

async function loadWeather(latId, lonId, summaryId, forecastId, mapKey, forceRefresh = false) {
  const lat = parseFloat(document.getElementById(latId)?.value);
  const lon = parseFloat(document.getElementById(lonId)?.value);
  if (isNaN(lat) || isNaN(lon)) { showToast("Coordenadas inválidas.", true); return; }
  try {
    const params = new URLSearchParams({ latitude: lat, longitude: lon });
    if (forceRefresh) params.set("forceRefresh", "true");
    const data = await request(`/api/v1/weather/dashboard?${params}`);
    renderWeatherSummary(summaryId, data);
    renderForecast(forecastId, data.hourlyForecast || []);
    if (mapKey === "main" && !state.weatherMapMain) initLeafletMap("weatherMapMain", "main");
    if (mapKey === "op"   && !state.weatherMapOp)   initLeafletMap("weatherMapOp",   "op");
    const targetMap = mapKey === "main" ? state.weatherMapMain : state.weatherMapOp;
    updateWeatherMap(targetMap, lat, lon, data.riskLevel, data.precipitationMmPerHour);
  } catch (err) { showToast(err.message, true); }
}

function renderWeatherSummary(summaryId, data) {
  const el = document.getElementById(summaryId);
  if (!el) return;
  const riskEs = RISK_ES[data.riskLevel] || data.riskLevel || "—";
  const color  = RISK_COLORS[riskEs] || RISK_COLORS[data.riskLevel] || RISK_COLORS.Bajo;
  const tip    = RISK_META[riskEs] || "";
  el.innerHTML = `
    <strong style="color:${color}" title="${tip}">${riskEs}</strong>
    <span>${Number(data.precipitationMmPerHour ?? 0).toFixed(2)} mm/h &middot; ${data.weatherDescription || ""}</span>
    <span style="font-size:0.78rem;color:var(--muted)">Prob. lluvia: ${data.precipitationProbabilityPercent ?? "?"}%</span>
  `;
}

function renderForecast(forecastId, items) {
  const el = document.getElementById(forecastId);
  if (!el) return;
  if (!items.length) { el.innerHTML = ""; return; }
  el.innerHTML = items.slice(0, 12).map(f => {
    const mm = Number(f.precipitationMm ?? 0);
    const color = mm >= 15 ? RISK_COLORS.Critical : mm >= 7.5 ? RISK_COLORS.High
      : mm >= 2.5 ? RISK_COLORS.Moderate : RISK_COLORS.Low;
    return `
      <div class="forecast-item">
        <span style="color:var(--muted)">${formatHour(f.timeUtc)}</span>
        <span>${f.weatherDescription || f.weatherCode}</span>
        <span style="color:${color};font-weight:700">${mm.toFixed(1)} mm</span>
      </div>
    `;
  }).join("");
}

function geolocate(latId, lonId) {
  if (!navigator.geolocation) { showToast("Geolocalización no disponible.", true); return; }
  navigator.geolocation.getCurrentPosition(
    pos => {
      document.getElementById(latId).value = pos.coords.latitude.toFixed(6);
      document.getElementById(lonId).value = pos.coords.longitude.toFixed(6);
      showToast("Ubicación detectada.");
    },
    () => showToast("No se pudo obtener la ubicación.", true)
  );
}

// ─────────────────────────── WEATHER PANEL — GEOFENCES & LOCATION ────────────
function pointInPolygon(lat, lon, polygonCoords) {
  // Ray-casting: polygonCoords [[lat, lon], ...]
  let inside = false;
  const n = polygonCoords.length;
  for (let i = 0, j = n - 1; i < n; j = i++) {
    const latI = polygonCoords[i][0], lonI = polygonCoords[i][1];
    const latJ = polygonCoords[j][0], lonJ = polygonCoords[j][1];
    if (((latI <= lat && lat < latJ) || (latJ <= lat && lat < latI)) &&
        (lon < (lonJ - lonI) * (lat - latI) / (latJ - latI) + lonI)) {
      inside = !inside;
    }
  }
  return inside;
}

function findUserGeofence(lat, lon) {
  for (const g of state.geofences) {
    if (!g.isActive || !g.polygonWkt) continue;
    const coords = parseWktPolygon(g.polygonWkt);
    if (!coords) continue;
    if (pointInPolygon(lat, lon, coords)) return g;
  }
  return null;
}

async function drawWeatherGeofenceOverlays() {
  const map = state.weatherMapMain;
  if (!map) return;
  state.weatherGeofenceLayers.forEach(({layer}) => map.removeLayer(layer));
  state.weatherGeofenceLayers = [];
  if (state.geofences.length === 0) await fetchGeofences();
  state.geofences.forEach(g => {
    if (!g.isActive || !g.polygonWkt) return;
    const coords = parseWktPolygon(g.polygonWkt);
    if (!coords) return;
    const layer = L.polygon(coords, {
      color: "#06264a", weight: 2, fillColor: "#06264a", fillOpacity: 0.08,
    }).bindPopup(`<strong>${escapeHtml(g.name)}</strong><br><small>${escapeHtml(g.neighborhood)}</small>`);
    layer.addTo(map);
    state.weatherGeofenceLayers.push({ id: g.id, layer });
  });
  if (state.weatherGeofenceLayers.length) {
    const group = L.featureGroup(state.weatherGeofenceLayers.map(l => l.layer));
    map.fitBounds(group.getBounds(), { padding: [24, 24] });
  }
}

function highlightWeatherGeofence(geofenceId) {
  state.weatherGeofenceLayers.forEach(({id, layer}) => {
    if (id === geofenceId) {
      layer.setStyle({ color: "#1d4ed8", weight: 3, fillColor: "#3b82f6", fillOpacity: 0.22 });
    } else {
      layer.setStyle({ color: "#06264a", weight: 2, fillColor: "#06264a", fillOpacity: 0.08 });
    }
  });
}

async function loadWeatherPanelAlerts(geofenceId) {
  const list  = document.getElementById("weatherAlertsList");
  const noMsg = document.getElementById("weatherNoAlerts");
  try {
    const params = new URLSearchParams({ status: "Approved", pageSize: 30 });
    if (geofenceId) params.set("geofenceId", geofenceId);
    const data  = await request(`/api/v1/alerts?${params}`);
    const items = data.items || [];
    if (!items.length) {
      list.innerHTML = "";
      noMsg.style.display = "";
    } else {
      noMsg.style.display = "none";
      list.innerHTML = items.map(a => `
        <div class="alert-card">
          <div class="alert-card-top">
            <span class="alert-card-title">${escapeHtml(a.title)}</span>
            ${severityBadge(a.severity)}
          </div>
          <div class="alert-card-meta">${formatDate(a.createdAtUtc)} · ${escapeHtml(a.address || "")}</div>
          <div class="alert-card-desc">${escapeHtml(a.description)}</div>
        </div>
      `).join("");
    }
  } catch (err) { showToast(err.message, true); }
}

async function loadThresholdMode() {
  try {
    const data = await request("/api/v1/weather/threshold-mode");
    applyThresholdUI(data.isDemoMode);
  } catch {}
}

async function setThresholdMode(demoMode) {
  try {
    await request("/api/v1/weather/threshold-mode", {
      method: "POST",
      body: JSON.stringify({ demoMode }),
    });
    applyThresholdUI(demoMode);
    // Re-consulta ignorando caché para ver el nuevo nivel de riesgo al instante
    const lat = document.getElementById("weatherLatInput")?.value;
    const lon = document.getElementById("weatherLonInput")?.value;
    if (lat && lon) {
      await loadWeather("weatherLatInput", "weatherLonInput", "weatherSummaryMain", "forecastListMain", "main", true);
    }
    showToast(demoMode ? "Modo demo activado." : "Reglas originales activadas.");
  } catch (err) { showToast(err.message, true); }
}

function applyThresholdUI(isDemoMode) {
  document.getElementById("thresholdOriginalBtn")?.classList.toggle("active-threshold", !isDemoMode);
  document.getElementById("thresholdDemoBtn")?.classList.toggle("active-threshold",  isDemoMode);
}

async function initWeatherPanel() {
  if (!state.weatherMapMain) {
    initLeafletMap("weatherMapMain", "main");
  }
  await drawWeatherGeofenceOverlays();

  // Mostrar controles de umbral solo al Admin y cargar modo actual
  const thresholdControls = document.getElementById("thresholdControls");
  if (thresholdControls) {
    thresholdControls.style.display = state.role === "Admin" ? "flex" : "none";
    if (state.role === "Admin") await loadThresholdMode();
  }

  const banner       = document.getElementById("weatherLocationBanner");
  const geoInfo      = document.getElementById("weatherGeofenceInfo");
  const alertSection = document.getElementById("weatherAlertsSection");
  const titleEl      = document.getElementById("weatherAlertsSectionTitle");

  if (!navigator.geolocation) {
    banner.textContent = "Geolocalización no disponible en este navegador.";
    banner.className = "location-banner banner-warning";
    banner.style.display = "";
    return;
  }

  banner.textContent = "⏳ Detectando tu ubicación...";
  banner.className = "location-banner banner-info";
  banner.style.display = "";

  navigator.geolocation.getCurrentPosition(
    async (pos) => {
      const lat = pos.coords.latitude;
      const lon = pos.coords.longitude;

      // Place user marker on map
      if (state.weatherUserMarker) state.weatherMapMain.removeLayer(state.weatherUserMarker);
      state.weatherUserMarker = L.circleMarker([lat, lon], {
        radius: 10, color: "#2563eb", fillColor: "#2563eb", fillOpacity: 0.85, weight: 3,
      }).bindPopup("<strong>📍 Tu ubicación</strong>").addTo(state.weatherMapMain).openPopup();

      // Auto-fill coordinates and load weather data
      document.getElementById("weatherLatInput").value = lat.toFixed(6);
      document.getElementById("weatherLonInput").value = lon.toFixed(6);
      loadWeather("weatherLatInput", "weatherLonInput", "weatherSummaryMain", "forecastListMain", "main");

      const userGeofence = findUserGeofence(lat, lon);
      if (userGeofence) {
        banner.textContent = `📍 Estás dentro de la geocerca: ${userGeofence.name}`;
        banner.className = "location-banner banner-success";
        geoInfo.innerHTML = `Zona activa: <strong>${escapeHtml(userGeofence.name)}</strong> — ${escapeHtml(userGeofence.neighborhood)}`;
        geoInfo.style.display = "";
        highlightWeatherGeofence(userGeofence.id);
        const entry = state.weatherGeofenceLayers.find(l => l.id === userGeofence.id);
        if (entry) state.weatherMapMain.fitBounds(entry.layer.getBounds(), { padding: [30, 30] });
        titleEl.textContent = `Alertas en tu zona — ${userGeofence.name}`;
        alertSection.style.display = "";
        await loadWeatherPanelAlerts(userGeofence.id);
      } else {
        banner.textContent = "No estás dentro de ninguna geocerca registrada. Mostrando todas las alertas activas.";
        banner.className = "location-banner banner-info";
        geoInfo.style.display = "none";
        titleEl.textContent = "Todas las alertas aprobadas";
        alertSection.style.display = "";
        await loadWeatherPanelAlerts(null);
      }
    },
    () => {
      banner.textContent = "📍 Concede permiso de ubicación para ver las alertas de tu zona.";
      banner.className = "location-banner banner-warning";
      alertSection.style.display = "none";
      geoInfo.style.display = "none";
    },
    { timeout: 10000, enableHighAccuracy: false }
  );
}

// ──────────────────────────────────────────────── GEOFENCE DRAW MAP ──────────
function initDrawGeofenceMap() {
  if (state.drawMap) { state.drawMap.invalidateSize(); return; }
  state.drawMap = L.map("drawGeofenceMap").setView([6.2518, -75.5636], 13);
  L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
    attribution: "© OpenStreetMap", maxZoom: 18,
  }).addTo(state.drawMap);
  state.drawMap.on("click", e => addGeofencePoint(e.latlng));

  // Info label on map
  const info = L.control({ position: "topright" });
  info.onAdd = () => {
    const d = L.DomUtil.create("div");
    d.style.cssText = "background:white;padding:6px 10px;border-radius:6px;font-size:0.78rem;box-shadow:0 2px 6px rgba(0,0,0,.15)";
    d.textContent = "Haz clic para agregar vértices";
    return d;
  };
  info.addTo(state.drawMap);
}

function addGeofencePoint(latlng) {
  state.drawPoints.push(latlng);
  const n = state.drawPoints.length;
  const m = L.circleMarker(latlng, {
    radius: 7, color: "#06264a", fillColor: "#06264a", fillOpacity: 1, weight: 2,
  }).bindTooltip(`${n}`, { permanent: true, direction: "top", offset: [0, -8] }).addTo(state.drawMap);
  state.drawMarkers.push(m);

  if (state.drawPolyline) state.drawMap.removeLayer(state.drawPolyline);
  if (state.drawPoints.length >= 2) {
    state.drawPolyline = L.polyline(state.drawPoints, {
      color: "#06264a", weight: 2, dashArray: "6,4",
    }).addTo(state.drawMap);
  }
  document.getElementById("geofencePointCount").textContent =
    `${n} punto${n !== 1 ? "s" : ""} — ${n >= 3 ? "listo para completar" : `falta${n === 1 ? "n" : ""} ${3 - n}`}`;
}

function clearGeofencePoints() {
  state.drawMarkers.forEach(m => state.drawMap?.removeLayer(m));
  if (state.drawPolyline) state.drawMap?.removeLayer(state.drawPolyline);
  if (state.drawPolygon)  state.drawMap?.removeLayer(state.drawPolygon);
  state.drawPoints = [];
  state.drawMarkers = [];
  state.drawPolyline = null;
  state.drawPolygon  = null;
  const cnt = document.getElementById("geofencePointCount");
  if (cnt) cnt.textContent = "0 puntos";
  const wkt = document.getElementById("geoWkt");
  if (wkt) wkt.value = "";
}

function completeGeofencePolygon() {
  if (state.drawPoints.length < 3) {
    showToast("Necesitas al menos 3 puntos.", true); return;
  }
  if (state.drawPolyline) { state.drawMap.removeLayer(state.drawPolyline); state.drawPolyline = null; }
  if (state.drawPolygon)  state.drawMap.removeLayer(state.drawPolygon);
  state.drawPolygon = L.polygon(state.drawPoints, {
    color: "#06264a", weight: 2, fillOpacity: 0.18,
  }).addTo(state.drawMap);
  state.drawMap.fitBounds(state.drawPolygon.getBounds(), { padding: [20, 20] });
  // Close ring: first point repeated at end
  const closed = [...state.drawPoints, state.drawPoints[0]];
  const coords = closed.map(p => `${p.lng.toFixed(6)} ${p.lat.toFixed(6)}`).join(",");
  document.getElementById("geoWkt").value = `POLYGON((${coords}))`;
  showToast("Polígono completado. Revisa el WKT y crea la geocerca.");
}

async function submitCreateGeofence() {
  const body = {
    code:         (document.getElementById("geoCode").value.trim()).toUpperCase(),
    name:         document.getElementById("geoName").value.trim(),
    neighborhood: document.getElementById("geoNeighborhood").value.trim(),
    polygonWkt:   document.getElementById("geoWkt").value.trim(),
  };
  if (!body.code || !body.name || !body.neighborhood || !body.polygonWkt) {
    showToast("Completa todos los campos y dibuja el polígono.", true); return;
  }
  try {
    await request("/api/v1/geofences", { method: "POST", body: JSON.stringify(body) });
    showToast(`Geocerca "${body.name}" creada.`);
    document.getElementById("createGeofenceForm").style.display = "none";
    // Reset form
    ["geoCode","geoName","geoNeighborhood","geoWkt"].forEach(id => {
      const el = document.getElementById(id); if (el) el.value = "";
    });
    clearGeofencePoints();
    state.geofences = [];
    await loadGeofences();
  } catch (err) { showToast(err.message, true); }
}

// ─────────────────────────────────────────────────────── UTILITIES ────────────
function parseWktPolygon(wkt) {
  if (!wkt) return null;
  const m = wkt.match(/POLYGON\s*\(\((.+)\)\)/i);
  if (!m) return null;
  return m[1].trim().split(/\s*,\s*/).map(pair => {
    const parts = pair.trim().split(/\s+/);
    return [parseFloat(parts[1]), parseFloat(parts[0])]; // [lat, lon]
  });
}

function escapeHtml(str) {
  if (str == null) return "";
  return String(str)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function formatDate(iso) {
  if (!iso) return "—";
  return new Date(iso).toLocaleString("es-CO", {
    day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit",
  });
}

function formatHour(iso) {
  if (!iso) return "—";
  return new Date(iso).toLocaleTimeString("es-CO", { hour: "2-digit", minute: "2-digit" });
}

// ──────────────────────────────────────────────────── INIT & EVENTS ──────────
document.addEventListener("DOMContentLoaded", () => {

  // ── Login form
  document.getElementById("loginForm").addEventListener("submit", e => {
    e.preventDefault();
    login(
      document.getElementById("username").value.trim(),
      document.getElementById("password").value,
    );
  });

  // ── Demo buttons
  document.querySelectorAll(".demo-btn").forEach(btn => {
    btn.addEventListener("click", () => {
      const creds = DEMO_CREDENTIALS[btn.dataset.demo];
      if (!creds) return;
      document.getElementById("username").value = creds.username;
      document.getElementById("password").value = creds.password;
      login(creds.username, creds.password);
    });
  });

  // ── Logout
  document.getElementById("logoutButton").addEventListener("click", logout);

  // ── Alert panel events
  document.getElementById("refreshAlertsBtn")?.addEventListener("click", loadAlerts);
  document.getElementById("alertStatusFilter")?.addEventListener("change", loadAlerts);
  document.getElementById("alertDetailClose")?.addEventListener("click", () => {
    document.getElementById("alertDetailBox").style.display = "none";
  });

  // ── New alert form
  document.getElementById("alertForm")?.addEventListener("submit", submitCreateAlert);

  // ── Operator weather
  document.getElementById("weatherOpBtn")?.addEventListener("click", () =>
    loadWeather("weatherOpLat", "weatherOpLon", "weatherSummaryOp", "forecastListOp", "op")
  );
  document.getElementById("geolocateOpBtn")?.addEventListener("click", () =>
    geolocate("weatherOpLat", "weatherOpLon")
  );

  // ── Users panel
  document.getElementById("showCreateUserBtn")?.addEventListener("click", () => {
    document.getElementById("createUserForm").style.display = "";
  });
  document.getElementById("cancelCreateUserBtn")?.addEventListener("click", () => {
    document.getElementById("createUserForm").style.display = "none";
  });
  document.getElementById("submitCreateUserBtn")?.addEventListener("click", submitCreateUser);
  document.getElementById("searchUsersBtn")?.addEventListener("click", loadUsers);
  document.getElementById("userSearchInput")?.addEventListener("keydown", e => {
    if (e.key === "Enter") loadUsers();
  });

  // ── Geofences panel
  document.getElementById("refreshGeofencesBtn")?.addEventListener("click", loadGeofences);
  document.getElementById("showCreateGeofenceBtn")?.addEventListener("click", () => {
    document.getElementById("createGeofenceForm").style.display = "";
    // Init draw map after paint so container has dimensions
    setTimeout(() => initDrawGeofenceMap(), 80);
  });
  document.getElementById("cancelCreateGeofenceBtn")?.addEventListener("click", () => {
    document.getElementById("createGeofenceForm").style.display = "none";
    clearGeofencePoints();
  });
  document.getElementById("completeGeofenceBtn")?.addEventListener("click", completeGeofencePolygon);
  document.getElementById("clearGeofenceBtn")?.addEventListener("click", clearGeofencePoints);
  document.getElementById("submitCreateGeofenceBtn")?.addEventListener("click", submitCreateGeofence);

  // ── Pending
  document.getElementById("refreshPendingBtn")?.addEventListener("click", loadPendingAlerts);

  // ── My zone
  document.getElementById("citizenGeofenceSelect")?.addEventListener("change", e => {
    state.citizenGeofenceId = e.target.value;
    localStorage.setItem("alerto.geofenceId", state.citizenGeofenceId);
    loadZoneAlerts();
  });
  document.getElementById("refreshZoneBtn")?.addEventListener("click", loadZoneAlerts);

  // ── Citizen report
  document.getElementById("citizenAlertForm")?.addEventListener("submit", submitCitizenAlert);
  document.getElementById("citizenGeolocate")?.addEventListener("click", () =>
    geolocate("citizenLat", "citizenLon")
  );
  document.getElementById("refreshActiveAlertsBtn")?.addEventListener("click", loadActiveAlertsForConfirm);

  // ── Main weather panel
  document.getElementById("weatherBtn")?.addEventListener("click", () =>
    loadWeather("weatherLatInput", "weatherLonInput", "weatherSummaryMain", "forecastListMain", "main")
  );
  document.getElementById("geolocateWeatherBtn")?.addEventListener("click", () =>
    geolocate("weatherLatInput", "weatherLonInput")
  );

  // ── Threshold mode buttons (admin only)
  document.getElementById("thresholdOriginalBtn")?.addEventListener("click", () => setThresholdMode(false));
  document.getElementById("thresholdDemoBtn")?.addEventListener("click",     () => setThresholdMode(true));

  // ── Restore session
  if (state.accessToken) {
    showAppShell();
  } else {
    showLoginScreen();
  }
});
