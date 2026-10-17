// Gráfico de evolución del patrimonio (SPEC-17-01) con ApexCharts.js:
// áreas apiladas por clase de activo, línea del patrimonio total y línea discontinua de lo invertido.
// Si ApexCharts no está cargado (sin conexión) no hace nada y el listado sigue funcionando.
window.wealthChart = (function () {
    var charts = {};

    // Colores de cada clase leídos de los tokens de app.css (con valor por defecto)
    function token(name, fallback) {
        var value = getComputedStyle(document.documentElement).getPropertyValue(name);
        return value && value.trim() ? value.trim() : fallback;
    }

    function money(value, currency) {
        if (value === null || value === undefined) {
            return '';
        }
        var symbol = currency === 'EUR' ? '€' : currency === 'USD' ? '$' : currency === 'GBP' ? '£' : currency;
        return Number(value).toLocaleString('es-ES', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ' + symbol;
    }

    function escapeHtml(text) {
        var div = document.createElement('div');
        div.textContent = text || '';
        return div.innerHTML;
    }

    function render(elementId, dataJson) {
        destroy(elementId);

        if (typeof ApexCharts === 'undefined') {
            return;
        }

        var container = document.getElementById(elementId);
        if (!container) {
            return;
        }

        var data = JSON.parse(dataJson);
        var points = data.points || [];
        var currency = data.currencyCode || 'EUR';

        // Las bandas por clase se dibujan como áreas acumuladas (sin el apilado de ApexCharts, que también
        // apilaría las líneas): cada área llega hasta la suma de su clase y las anteriores, y se dibuja
        // primero la más alta para que cada banda quede visible. El tooltip enseña el valor real de la clase.
        var classes = [
            { name: 'Acciones', field: 'stocks', color: token('--indigo', '#3F3B9B') },
            { name: 'Fondos', field: 'funds', color: token('--teal', '#1F8A93') },
            { name: 'Efectivo', field: 'bank', color: token('--sage', '#7FB08F') },
            { name: 'Refugio', field: 'safeHaven', color: token('--amber', '#E0A030') },
            { name: 'Inmuebles', field: 'realEstate', color: token('--plum', '#9A4F8C') }
        ];

        var cumulative = points.map(function () { return 0; });
        var areaSeries = classes.map(function (c) {
            var values = points.map(function (p, i) {
                cumulative[i] += Number(p[c.field] || 0);
                return Math.round(cumulative[i] * 100) / 100;
            });
            return { name: c.name, type: 'area', data: values, field: c.field, color: c.color };
        }).reverse();

        var lineSeries = [
            { name: 'Patrimonio total', type: 'line', data: points.map(function (p) { return p.total; }), field: 'total', color: token('--ink', '#231F3A') },
            { name: 'Invertido (sin efectivo)', type: 'line', data: points.map(function (p) { return p.invested; }), field: 'invested', color: token('--ink-soft', '#5A5670') }
        ];

        var all = areaSeries.concat(lineSeries);

        var options = {
            chart: {
                type: 'line',
                height: 380,
                animations: { enabled: false },
                toolbar: { show: false },
                fontFamily: 'inherit'
            },
            series: all.map(function (s) { return { name: s.name, type: s.type, data: s.data }; }),
            colors: all.map(function (s) { return s.color; }),
            stroke: {
                curve: 'straight',
                width: [0, 0, 0, 0, 0, 3, 2],
                dashArray: [0, 0, 0, 0, 0, 0, 6]
            },
            fill: {
                type: 'solid',
                opacity: [0.9, 0.9, 0.9, 0.9, 0.9, 1, 1]
            },
            markers: { size: points.length === 1 ? 6 : 0, hover: { size: 4 } },
            xaxis: {
                categories: points.map(function (p) { return p.label; }),
                labels: { rotate: -45, hideOverlappingLabels: true }
            },
            yaxis: {
                labels: {
                    formatter: function (value) {
                        return value === null || value === undefined
                            ? ''
                            : Number(value).toLocaleString('es-ES', { maximumFractionDigits: 0 });
                    }
                }
            },
            legend: { position: 'bottom' },
            dataLabels: { enabled: false },
            tooltip: {
                shared: true,
                intersect: false,
                x: {
                    formatter: function (value, opts) {
                        var point = opts && opts.dataPointIndex !== undefined ? points[opts.dataPointIndex] : null;
                        if (!point) {
                            return value;
                        }
                        return point.note
                            ? escapeHtml(point.label) + ' · ' + escapeHtml(point.note)
                            : escapeHtml(point.label);
                    }
                },
                y: {
                    // Valor real de la clase (no el acumulado que dibuja el área)
                    formatter: function (value, opts) {
                        if (opts && opts.seriesIndex !== undefined && opts.dataPointIndex !== undefined) {
                            var s = all[opts.seriesIndex];
                            var point = points[opts.dataPointIndex];
                            if (s && point) {
                                return money(point[s.field], currency);
                            }
                        }
                        return money(value, currency);
                    }
                }
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
    }

    return {
        render: render,
        destroy: destroy
    };
})();
