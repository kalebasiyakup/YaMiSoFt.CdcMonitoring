let network = null;

export function render(containerId, nodes, edges) {
    const container = document.getElementById(containerId);
    if (!container) return;

    const data = {
        nodes: new vis.DataSet(nodes),
        edges: new vis.DataSet(edges)
    };

    const options = {
        nodes: {
            shape: "box",
            margin: 10,
            font: { multi: "html", size: 14 }
        },
        edges: {
            arrows: "to",
            font: { align: "top", size: 11 },
            smooth: { type: "cubicBezier", roundness: 0.4 }
        },
        physics: {
            solver: "forceAtlas2Based",
            forceAtlas2Based: { springLength: 180 },
            stabilization: { iterations: 150 }
        },
        layout: { improvedLayout: true }
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
