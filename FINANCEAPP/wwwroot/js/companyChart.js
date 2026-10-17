// Gráfico de cotización de una empresa con ApexCharts.js (SPEC-05 y SPEC-05-01):
// línea o velas, marcadores de compras, traspasos, ventas y dividendos, línea de precio actual
// (se mueve con el precio en vivo), línea opcional de precio medio y volumen opcional en un
// segundo gráfico sincronizado.
window.companyChart = (function () {
    var charts = {};

    var COLORS = {
        line: '#3F3B9B',
        buy: '#3F3B9B',
        transfer: '#6C63C7',
        sell: '#C23B2E',
        dividend: '#177A4D',
        current: '#177A4D',
        average: '#8A5A00',
        up: '#177A4D',
        down: '#C23B2E',
        volume: '#9AA0B4'
    };

    var KIND_LABELS = {
        Buy: 'Compra',
        Transfer: 'Traspaso',
        Sell: 'Venta',
        Dividend: 'Dividendo'
    };

    function formatMoney(value, symbol) {
        if (value === null || value === undefined || isNaN(value)) {
            return '';
        }
        return Number(value).toLocaleString('es-ES', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ' + symbol;
    }

    function escapeHtml(text) {
        return String(text)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    }

    // Cierre del día del marcador o, si no hay sesión ese día, el del día anterior más cercano
    function closeAt(series, dateMs) {
        var result = null;
        for (var i = 0; i < series.length; i++) {
            if (series[i].x <= dateMs) {
                result = series[i].c;
            } else {
                break;
            }
        }
        if (result === null && series.length > 0) {
            result = series[0].c;
        }
        return result;
    }

    function markerColor(kind) {
        switch (kind) {
            case 'Sell': return COLORS.sell;
            case 'Dividend': return COLORS.dividend;
            case 'Transfer': return COLORS.transfer;
            default: return COLORS.buy;
        }
    }

    function buildPointAnnotations(markers, series, symbol) {
        return (markers || []).map(function (m) {
            var y = m.kind === 'Dividend' ? closeAt(series, m.date) : m.price;
            var color = markerColor(m.kind);
            return {
                x: m.date,
                y: y,
                marker: {
                    size: m.kind === 'Dividend' ? 5 : 6,
                    fillColor: m.kind === 'Dividend' ? color : '#ffffff',
                    strokeColor: color,
                    strokeWidth: 2,
                    shape: m.kind === 'Sell' ? 'square' : 'circle'
                },
                label: { text: '' },
                mouseEnter: function (annotationEl) {
                    showOperationTooltip(annotationEl, m, symbol);
                },
                mouseLeave: function () {
                    hideOperationTooltip();
                }
            };
        }).filter(function (a) { return a.y !== null && a.y !== undefined; });
    }

    function showOperationTooltip(element, m, symbol) {
        hideOperationTooltip();

        var lines = '<strong>' + escapeHtml(KIND_LABELS[m.kind] || m.kind) + ' · ' + escapeHtml(m.dateLabel) + '</strong><br/>';
        if (m.kind === 'Dividend') {
            lines += 'Importe: ' + formatMoney(m.amount, symbol);
        } else {
            lines += 'Precio: ' + formatMoney(m.price, symbol) + '<br/>' +
                'Acciones: ' + m.shares + '<br/>' +
                'Importe: ' + formatMoney(m.amount, symbol);
        }

        var tooltip = document.createElement('div');
        tooltip.id = 'company-chart-op-tooltip';
        tooltip.className = 'company-chart-op-tooltip';
        tooltip.innerHTML = lines;
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

    function currentPriceAnnotation(price, symbol) {
        return {
            id: 'current-price',
            y: price,
            borderColor: COLORS.current,
            strokeDashArray: 0,
            label: {
                borderColor: COLORS.current,
                style: { color: '#ffffff', background: COLORS.current },
                text: 'Precio actual: ' + formatMoney(price, symbol)
            }
        };
    }

    function averagePriceAnnotation(price, symbol) {
        return {
            id: 'average-price',
            y: price,
            borderColor: COLORS.average,
            strokeDashArray: 6,
            label: {
                borderColor: COLORS.average,
                position: 'left',
                textAnchor: 'start',
                style: { color: '#ffffff', background: COLORS.average },
                text: 'Precio medio: ' + formatMoney(price, symbol)
            }
        };
    }

    function toSeriesData(series, candles) {
        return series.map(function (p) {
            return candles
                ? { x: p.x, y: [p.o, p.h, p.l, p.c] }
                : { x: p.x, y: p.c };
        });
    }

    function render(elementId, volumeElementId, dataJson) {
        if (typeof ApexCharts === 'undefined') {
            // El modal traduce este error a «No se pudo cargar la librería de gráficos»
            throw new Error('APEXCHARTS_NOT_LOADED');
        }

        var data = JSON.parse(dataJson);
        destroy(elementId);

        var container = document.getElementById(elementId);
        if (!container) {
            return;
        }

        var symbol = data.currencySymbol || '€';
        var series = (data.series || []).slice();
        var candles = !!data.showCandles;
        var group = 'company-chart-' + elementId;

        var yAxisAnnotations = [];
        if (data.currentPrice !== null && data.currentPrice !== undefined) {
            yAxisAnnotations.push(currentPriceAnnotation(data.currentPrice, symbol));
        }
        if (data.showAverage && data.averagePrice !== null && data.averagePrice !== undefined) {
            yAxisAnnotations.push(averagePriceAnnotation(data.averagePrice, symbol));
        }

        var yLabels = {
            minWidth: 70,
            formatter: function (value) {
                return formatMoney(value, symbol);
            }
        };

        var options = {
            chart: {
                id: elementId,
                group: data.showVolume ? group : undefined,
                type: candles ? 'candlestick' : 'line',
                height: 420,
                animations: { enabled: false },
                zoom: { enabled: true, type: 'x', autoScaleYaxis: true },
                toolbar: { show: true, tools: { zoom: true, zoomin: true, zoomout: true, pan: true, reset: true, download: false } }
            },
            series: [{ name: 'Cotización', data: toSeriesData(series, candles) }],
            xaxis: { type: 'datetime', labels: { datetimeUTC: false } },
            yaxis: { labels: yLabels, tooltip: { enabled: false } },
            stroke: candles ? { width: 1 } : { curve: 'straight', width: 2 },
            colors: [COLORS.line],
            plotOptions: {
                candlestick: { colors: { upward: COLORS.up, downward: COLORS.down } }
            },
            tooltip: {
                x: { format: 'dd MMM yyyy' },
                y: { formatter: function (value) { return formatMoney(value, symbol); } }
            },
            annotations: {
                points: buildPointAnnotations(data.markers, series, symbol),
                yaxis: yAxisAnnotations
            }
        };

        if (candles) {
            // Tooltip propio de velas con apertura, máximo, mínimo y cierre en la divisa de la empresa
            options.tooltip = {
                custom: function (ctx) {
                    var p = series[ctx.dataPointIndex];
                    if (!p) {
                        return '';
                    }
                    return '<div class="company-chart-candle-tooltip">' +
                        '<strong>' + new Date(p.x).toLocaleDateString('es-ES') + '</strong><br/>' +
                        'Apertura: ' + formatMoney(p.o, symbol) + '<br/>' +
                        'Máximo: ' + formatMoney(p.h, symbol) + '<br/>' +
                        'Mínimo: ' + formatMoney(p.l, symbol) + '<br/>' +
                        'Cierre: ' + formatMoney(p.c, symbol) +
                        '</div>';
                }
            };
        }

        var chart = new ApexCharts(container, options);
        chart.render();

        var volumeChart = null;
        var volumeContainer = volumeElementId ? document.getElementById(volumeElementId) : null;
        if (data.showVolume && volumeContainer) {
            volumeChart = new ApexCharts(volumeContainer, {
                chart: {
                    id: elementId + '-volume',
                    group: group,
                    type: 'bar',
                    height: 130,
                    animations: { enabled: false },
                    toolbar: { show: false },
                    zoom: { enabled: false }
                },
                series: [{ name: 'Volumen', data: series.map(function (p) { return { x: p.x, y: p.v }; }) }],
                xaxis: { type: 'datetime', labels: { show: false, datetimeUTC: false } },
                yaxis: {
                    labels: {
                        minWidth: 70,
                        formatter: function (value) {
                            return value !== null && value !== undefined ? Number(value).toLocaleString('es-ES', { notation: 'compact' }) : '';
                        }
                    }
                },
                colors: [COLORS.volume],
                dataLabels: { enabled: false },
                plotOptions: { bar: { columnWidth: '90%' } },
                tooltip: { x: { format: 'dd MMM yyyy' } }
            });
            volumeChart.render();
        }

        charts[elementId] = {
            chart: chart,
            volumeChart: volumeChart,
            series: series,
            candles: candles,
            isDaily: !!data.isDaily,
            symbol: symbol
        };
    }

    // Mueve la línea «Precio actual» y, en vista diaria, actualiza el punto de hoy con el precio en vivo
    // (cierre provisional, no se guarda). No recrea el gráfico.
    function updateLive(elementId, price, todayMs) {
        var state = charts[elementId];
        if (!state || price === null || price === undefined) {
            return;
        }

        state.chart.removeAnnotation('current-price');
        state.chart.addYaxisAnnotation(currentPriceAnnotation(price, state.symbol));

        if (!state.isDaily) {
            return;
        }

        var series = state.series;
        var last = series.length > 0 ? series[series.length - 1] : null;
        if (last && last.x === todayMs) {
            if (last.c === price) {
                return;
            }
            last.c = price;
            last.h = Math.max(last.h, price);
            last.l = Math.min(last.l, price);
        } else if (!last || last.x < todayMs) {
            series.push({ x: todayMs, o: price, h: price, l: price, c: price, v: 0 });
        } else {
            return;
        }

        state.chart.updateSeries([{ name: 'Cotización', data: toSeriesData(series, state.candles) }], false);
    }

    function destroy(elementId) {
        var state = charts[elementId];
        if (state) {
            if (state.volumeChart) {
                state.volumeChart.destroy();
            }
            state.chart.destroy();
            delete charts[elementId];
        }
        hideOperationTooltip();
    }

    return {
        render: render,
        updateLive: updateLive,
        destroy: destroy
    };
})();
