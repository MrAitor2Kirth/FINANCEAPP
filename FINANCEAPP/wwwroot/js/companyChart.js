// Renderizado del gráfico de cotización de una empresa con ApexCharts.js,
// incluyendo las operaciones de compra superpuestas y la línea de precio actual.
window.companyChart = (function () {
    var charts = {};

    function buildPointAnnotations(operations) {
        return (operations || []).map(function (op) {
            return {
                x: op.date,
                y: op.price,
                marker: {
                    size: 6,
                    fillColor: '#ffffff',
                    strokeColor: '#3F3B9B',
                    strokeWidth: 2,
                    shape: 'circle'
                },
                label: { text: '' },
                mouseEnter: function (annotationEl) {
                    showOperationTooltip(annotationEl, op);
                },
                mouseLeave: function () {
                    hideOperationTooltip();
                }
            };
        });
    }

    function showOperationTooltip(element, op) {
        hideOperationTooltip();

        var tooltip = document.createElement('div');
        tooltip.id = 'company-chart-op-tooltip';
        tooltip.className = 'company-chart-op-tooltip';
        tooltip.innerHTML =
            '<strong>' + op.dateLabel + '</strong><br/>' +
            'Precio: ' + Number(op.price).toFixed(2) + ' €<br/>' +
            'Acciones: ' + op.shares;

        document.body.appendChild(tooltip);

        var rect = element.getBoundingClientRect();
        tooltip.style.left = (rect.left + window.scrollX + 12) + 'px';
        tooltip.style.top = (rect.top + window.scrollY - 12) + 'px';
    }

    function hideOperationTooltip() {
        var existing = document.getElementById('company-chart-op-tooltip');
        if (existing) {
            existing.remove();
        }
    }

    function render(elementId, dataJson) {
        var data = JSON.parse(dataJson);
        destroy(elementId);

        var container = document.getElementById(elementId);
        if (!container) {
            return;
        }

        var yAxisAnnotations = [];
        if (data.currentPrice !== null && data.currentPrice !== undefined) {
            yAxisAnnotations.push({
                y: data.currentPrice,
                borderColor: '#177A4D',
                strokeDashArray: 0,
                label: {
                    borderColor: '#177A4D',
                    style: { color: '#ffffff', background: '#177A4D' },
                    text: 'Precio actual: ' + Number(data.currentPrice).toFixed(2) + ' €'
                }
            });
        }

        var options = {
            chart: {
                type: 'line',
                height: 420,
                animations: { enabled: false },
                zoom: { enabled: true, type: 'x', autoScaleYaxis: true },
                toolbar: { show: true, tools: { zoom: true, zoomin: true, zoomout: true, pan: true, reset: true } }
            },
            series: [{ name: 'Cotización', data: data.series || [] }],
            xaxis: { type: 'datetime' },
            yaxis: {
                labels: {
                    formatter: function (value) {
                        return value !== null && value !== undefined ? Number(value).toFixed(2) + ' €' : '';
                    }
                }
            },
            stroke: { curve: 'straight', width: 2 },
            colors: ['#3F3B9B'],
            tooltip: { x: { format: 'dd MMM yyyy' } },
            annotations: {
                points: buildPointAnnotations(data.operations),
                yaxis: yAxisAnnotations
            }
        };

        var chart = new ApexCharts(container, options);
        chart.render();
        charts[elementId] = chart;
    }

    function destroy(elementId) {
        if (charts[elementId]) {
            charts[elementId].destroy();
            delete charts[elementId];
        }
        hideOperationTooltip();
    }

    return {
        render: render,
        destroy: destroy
    };
})();
