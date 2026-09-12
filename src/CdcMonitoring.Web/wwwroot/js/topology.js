let network = null;
let edgesDataSet = null;
let panelEl = null;

export function render(containerId, nodes, edges, dotNetRef) {
    const container = document.getElementById(containerId);
    if (!container) return;

    edgesDataSet = new vis.DataSet(edges);
    const data = {
        nodes: new vis.DataSet(nodes),
        edges: edgesDataSet
    };

    const options = {
        nodes: {
            shape: "box",
            margin: 10,
            font: { multi: "html", size: 14 }
        },
        edges: {
            arrows: "to",
            font: { align: "top", size: 12 },
            smooth: { type: "cubicBezier", roundness: 0.4 }
        },
        physics: {
            solver: "forceAtlas2Based",
            forceAtlas2Based: { springLength: 180 },
            stabilization: { iterations: 150 }
        },
        layout: { improvedLayout: true },
        interaction: { hover: true }
    };

    if (network) {
        network.destroy();
    }
    network = new vis.Network(container, data, options);
    ensurePanel(container);
    hidePanel();

    network.on("click", (params) => {
        // Düğüme tıklanınca alttaki domain listesi o servise geçer.
        if (params.nodes.length > 0) {
            hidePanel();
            dotNetRef?.invokeMethodAsync("OnNodeSelected", params.nodes[0]);
            return;
        }

        if (params.edges.length > 0) {
            const edge = edgesDataSet.get(params.edges[0]);
            showPanel(edge);
        } else {
            hidePanel();
        }
    });
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
}
