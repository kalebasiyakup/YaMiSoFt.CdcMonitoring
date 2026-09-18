let network = null;

// producer (yazma yetkisi) -> topic -> reader (okuma yetkisi) / consumer-group (aktif abonelik)
// şeklinde katmanlı, yönlü bir grafik; ayrıca group-acl-principal -> consumer-group kenarları
// (o grubu kullanmaya ACL ile yetkili principal'lar, aktif olsun olmasın) eklenir. topology.js'teki
// odaklama/panel mantığı burada gerekmiyor: her düğüm/kenarın ayrıntısı zaten native vis.js
// tooltip'i (title) ile yeterli.
export function render(containerId, nodes, edges) {
    const container = document.getElementById(containerId);
    if (!container) return;

    const data = {
        nodes: new vis.DataSet(nodes),
        edges: new vis.DataSet(edges)
    };

    const options = {
        nodes: {
            margin: 10,
            font: { size: 13 }
        },
        groups: {
            producer: { shape: "box", color: { background: "#eef3fb", border: "#4a76c4" } },
            topic: { shape: "ellipse", color: { background: "#f3ecfc", border: "#8a63d2" } },
            reader: { shape: "box", color: { background: "#fdf0e6", border: "#c2703d" } },
            "consumer-group": { shape: "box", color: { background: "#e6f6ef", border: "#2f9e64" } },
            "group-acl-principal": { shape: "box", color: { background: "#fbf1de", border: "#d9a441" } }
        },
        edges: {
            arrows: "to",
            smooth: { type: "cubicBezier", roundness: 0.4 }
        },
        physics: { enabled: false },
        layout: {
            hierarchical: {
                enabled: true,
                direction: "UD",
                sortMethod: "directed",
                levelSeparation: 130,
                nodeSpacing: 160
            }
        },
        interaction: { hover: true }
    };

    if (network) {
        network.destroy();
    }
    network = new vis.Network(container, data, options);
}

export function dispose() {
    if (network) {
        network.destroy();
        network = null;
    }
}
