let network = null;
let nodesDataSet = null;
let edgesDataSet = null;
let panelEl = null;
let originalEdgeColor = new Map();
let focusedNodeId = null;

export function render(containerId, nodes, edges, dotNetRef) {
    const container = document.getElementById(containerId);
    if (!container) return;

    nodesDataSet = new vis.DataSet(nodes);
    edgesDataSet = new vis.DataSet(edges);
    originalEdgeColor = new Map(edges.map(e => [e.id, (e.color && e.color.color) || "#848484"]));
    focusedNodeId = null;
    const data = {
        nodes: nodesDataSet,
        edges: edgesDataSet
    };

    const options = {
        nodes: {
            margin: 10,
            font: { multi: "html", size: 14 }
        },
        groups: {
            // Kaynak (pub): yayını yapan bağlantı (DB) — mavi. Hub: publication/stream — mor.
            // Target (sub): abone bağlantı — turuncu/terracotta. Üçü de birbirinden ve
            // kenarların sağlık renklerinden (yeşil/sarı/kırmızı/gri) net şekilde ayrışır.
            source: { shape: "box", color: { background: "#eef3fb", border: "#4a76c4" } },
            // widthConstraint: uzun publication adları tek satırda genişlemek yerine
            // satır kaydırılır (ellipse yatayda daralır, dikeyde büyür) — komşu hub'larla çakışmayı önler.
            hub: { shape: "ellipse", color: { background: "#f3ecfc", border: "#8a63d2" }, widthConstraint: { minimum: 90, maximum: 130 } },
            target: { shape: "box", color: { background: "#fdf0e6", border: "#c2703d" } }
        },
        edges: {
            arrows: "to",
            font: { align: "top", size: 12 },
            smooth: { type: "cubicBezier", roundness: 0.4 }
        },
        physics: { enabled: false },
        layout: {
            hierarchical: {
                enabled: true,
                direction: "UD",
                sortMethod: "directed",
                levelSeparation: 130,
                nodeSpacing: 200
            }
        },
        interaction: { hover: true }
    };

    if (network) {
        network.destroy();
    }
    network = new vis.Network(container, data, options);
    ensurePanel(container);
    hidePanel();

    network.on("click", (params) => {
        if (params.nodes.length > 0) {
            hidePanel();
            const clickedId = params.nodes[0];
            const node = nodesDataSet.get(clickedId);

            // Aynı düğüme tekrar tıklamak odağı kaldırır; başka bir düğüme tıklamak odağı değiştirir.
            if (focusedNodeId === clickedId) {
                clearFocus();
            } else {
                applyFocus(clickedId);
            }

            // Hub (publication) düğümleri gerçek bir bağlantıya karşılık gelmez;
            // alttaki domain listesi yalnızca kaynak/hedef bağlantı tıklamalarında geçiş yapar.
            if (node && node.group !== "hub") {
                dotNetRef?.invokeMethodAsync("OnNodeSelected", clickedId);
            }
            return;
        }

        if (params.edges.length > 0) {
            const edge = edgesDataSet.get(params.edges[0]);
            showPanel(edge);
        } else {
            hidePanel();
            clearFocus();
        }
    });
}

// Tıklanan düğümü ve doğrudan komşularını/aralarındaki kenarları normal opaklıkta bırakır,
// grafiğin geri kalanını soluklaştırır ("bu düğüm neyle konuşuyor?" sorusuna odaklanmak için).
function applyFocus(nodeId) {
    focusedNodeId = nodeId;

    const connectedNodes = new Set(network.getConnectedNodes(nodeId));
    connectedNodes.add(nodeId);
    const connectedEdges = new Set(network.getConnectedEdges(nodeId));

    nodesDataSet.update(nodesDataSet.get().map(n => ({
        id: n.id,
        opacity: connectedNodes.has(n.id) ? 1 : 0.15
    })));

    edgesDataSet.update(edgesDataSet.get().map(e => ({
        id: e.id,
        color: { color: originalEdgeColor.get(e.id) || "#848484", opacity: connectedEdges.has(e.id) ? 1 : 0.12 }
    })));
}

function clearFocus() {
    if (focusedNodeId === null) return;
    focusedNodeId = null;

    nodesDataSet.update(nodesDataSet.get().map(n => ({ id: n.id, opacity: 1 })));
    edgesDataSet.update(edgesDataSet.get().map(e => ({
        id: e.id,
        color: { color: originalEdgeColor.get(e.id) || "#848484", opacity: 1 }
    })));
}

function ensurePanel(container) {
    if (panelEl) return;

    container.style.position = "relative";
    panelEl = document.createElement("div");
    panelEl.className = "topology-detail-panel";
    panelEl.style.cssText =
        "position:absolute; top:0; right:0; bottom:0; width:340px; max-width:80%; overflow-y:auto; " +
        "background:#fff; border-left:1px solid #dee2e6; box-shadow:-2px 0 6px rgba(0,0,0,.08); " +
        "padding:12px; display:none; z-index:10; font-size:13px;";
    container.appendChild(panelEl);
}

function hidePanel() {
    if (panelEl) panelEl.style.display = "none";
}

function showPanel(edge) {
    if (!panelEl || !edge || !edge.relationships) return;

    const items = edge.relationships.map(r => `
        <div style="border-left:4px solid ${r.color}; padding:6px 8px; margin-bottom:8px; background:#f8f9fa;">
            <div><strong>Slot:</strong> ${escapeHtml(r.slot)}</div>
            <div><strong>Publication:</strong> ${escapeHtml(r.publication)}</div>
            <div><strong>Subscription:</strong> ${escapeHtml(r.subscription)}</div>
            <div><strong>Kayıt durumu:</strong> ${escapeHtml(r.status)}</div>
            <div><strong>Sağlık:</strong> ${escapeHtml(r.health)}</div>
            ${r.lag !== null && r.lag !== undefined ? `<div><strong>Lag:</strong> ${Number(r.lag).toLocaleString("tr-TR")} B</div>` : ""}
        </div>`).join("");

    panelEl.innerHTML = `
        <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom:8px;">
            <strong>${edge.relationships.length} ilişki</strong>
            <button type="button" id="topology-panel-close" style="border:none; background:none; font-size:18px; line-height:1; cursor:pointer;">&times;</button>
        </div>
        ${items}`;
    panelEl.style.display = "block";
    document.getElementById("topology-panel-close").addEventListener("click", hidePanel);
}

function escapeHtml(value) {
    const div = document.createElement("div");
    div.textContent = value ?? "";
    return div.innerHTML;
}

export function dispose() {
    if (network) {
        network.destroy();
        network = null;
    }
    if (panelEl && panelEl.parentElement) {
        panelEl.parentElement.removeChild(panelEl);
    }
    panelEl = null;
    edgesDataSet = null;
    nodesDataSet = null;
    originalEdgeColor = new Map();
    focusedNodeId = null;
}
