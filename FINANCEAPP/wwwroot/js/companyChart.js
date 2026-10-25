// Gráfico de cotización de una empresa con ApexCharts.js (SPEC-05, SPEC-05-01, SPEC-05-02 y SPEC-05-03):
// línea o velas, marcadores de compras, traspasos, ventas y dividendos, marcas del usuario, línea de precio
// actual (se mueve con el precio en vivo), línea opcional de precio medio y volumen opcional en dinero en un
// segundo gráfico sincronizado.
// Eje por sesiones (SPEC-05-03): la X de cada punto es su número de orden en la serie, no su fecha; así no hay
// huecos de fines de semana, festivos ni noches. Las fechas se ven en etiquetas y tooltips, y el modal sigue
// hablando en fechas (milisegundos): este fichero las traduce a índices.
// Calendario de dividendos (SPEC-21-01): el eje se prolonga con sesiones futuras vacías, los pagos de dividendo se
// dibujan como rombos azules (también en el futuro) y un minigráfico navegador («brush») elige el tramo visible.
// Comparación (SPEC-05-04): con data.comparison se dibujan dos líneas en base 100 (la empresa y otra empresa o un
// índice) con las marcas sobre las dos; el gráfico avisa al modal del tramo visible y el modal (un servicio C#)
// devuelve las series recalculadas con setComparison. Las cuentas no se hacen aquí.
window.companyChart = (function () {
    var charts = {};

    var COLORS = {
        line: '#3F3B9B',
        buy: '#177A4D',
        transfer: '#6C63C7',
        sell: '#C23B2E',
        dividend: '#1F6FD1',
        mark: '#8B5A2B',
        current: '#4A4F5C',
        average: '#C2185B',
        future: '#9AA0B4',
        compared: '#E07B00',
        up: '#177A4D',
        down: '#C23B2E'
    };

    var KIND_LABELS = {
        Buy: 'Compra',
        Transfer: 'Traspaso',
        Sell: 'Venta',
        Dividend: 'Dividendo'
    };

    function isSet(value) {
        return value !== null && value !== undefined;
    }

    function formatMoney(value, symbol) {
        if (!isSet(value) || isNaN(value)) {
            return '';
        }
        return Number(value).toLocaleString('es-ES', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ' + symbol;
    }

    function formatCompactMoney(value, symbol) {
        if (!isSet(value) || isNaN(value)) {
            return '';
        }
        return Number(value).toLocaleString('es-ES', { notation: 'compact', maximumFractionDigits: 1 }) + ' ' + symbol;
    }

    // Fecha completa de un punto según la temporalidad (SPEC-05-02, RF-10)
    function formatDate(ms, granularity) {
        var d = new Date(ms);
        switch (granularity) {
            case 'Hourly':
                return d.toLocaleString('es-ES', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
            case 'Weekly':
                return 'Semana del ' + d.toLocaleDateString('es-ES', { day: '2-digit', month: 'short', year: 'numeric' });
            case 'Monthly':
                return d.toLocaleDateString('es-ES', { month: 'short', year: 'numeric' });
            case 'Yearly':
                return String(d.getFullYear());
            default:
                return d.toLocaleDateString('es-ES', { day: '2-digit', month: 'short', year: 'numeric' });
        }
    }

    // Etiqueta corta del eje X
    function formatAxisDate(ms, granularity) {
        var d = new Date(ms);
        switch (granularity) {
            case 'Hourly':
                return d.toLocaleString('es-ES', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' });
            case 'Monthly':
                return d.toLocaleDateString('es-ES', { month: 'short', year: '2-digit' });
            case 'Yearly':
                return String(d.getFullYear());
            default:
                return d.toLocaleDateString('es-ES', { day: '2-digit', month: 'short', year: '2-digit' });
        }
    }

    function escapeHtml(text) {
        return String(text)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    // ---------------------------------------------------------------- Fechas ⇄ índices

    // Índice de la sesión de esa fecha o de la anterior más cercana; -1 si es anterior a todas
    function indexAtOrBefore(series, ms) {
        var lo = 0;
        var hi = series.length - 1;
        var result = -1;
        while (lo <= hi) {
            var mid = (lo + hi) >> 1;
            if (series[mid].x <= ms) {
                result = mid;
                lo = mid + 1;
            } else {
                hi = mid - 1;
            }
        }
        return result;
    }

    // Índice de la primera sesión de esa fecha o posterior; series.length si es posterior a todas
    function indexAtOrAfter(series, ms) {
        var lo = 0;
        var hi = series.length - 1;
        var result = series.length;
        while (lo <= hi) {
            var mid = (lo + hi) >> 1;
            if (series[mid].x >= ms) {
                result = mid;
                hi = mid - 1;
            } else {
                lo = mid + 1;
            }
        }
        return result;
    }

    // Fecha (ms) del índice más cercano, dentro de la serie
    function msAt(series, value) {
        if (series.length === 0 || !isSet(value) || isNaN(value)) {
            return null;
        }
        var i = Math.min(series.length - 1, Math.max(0, Math.round(value)));
        return series[i].x;
    }

    // Tramo de índices para un tramo de fechas; con margen al final si llega a la última sesión
    function rangeFromMs(state, startMs, endMs) {
        var series = state.series;
        var last = series.length - 1;
        if (last < 0) {
            return null;
        }
        var start = isSet(startMs) ? indexAtOrAfter(series, startMs) : 0;
        var end = isSet(endMs) ? indexAtOrBefore(series, endMs) : last;
        if (start > last || end < 0) {
            return null;
        }
        if (end < start) {
            end = start;
        }
        var lastPrice = isSet(state.lastPriceIndex) ? state.lastPriceIndex : last;
        var pad = end >= lastPrice ? Math.max(1, (end - start) * 0.02) : 0.5;
        return { min: start - 0.5, max: end + pad };
    }

    // Tramo inicial de la temporalidad (SPEC-05-02, RF-8): acaba siempre en el último precio, aunque haya
    // sesiones futuras (SPEC-21-01, RF-10); sin inicio = desde el primer punto
    function initialRange(state) {
        if (!isSet(state.viewEnd)) {
            return null;
        }
        return rangeFromMs(state, state.viewStart, state.viewEnd);
    }

    // Todo lo que tiene precio, sin el tramo futuro
    function pricedRange(state) {
        return rangeFromMs(state, null, msAt(state.series, state.lastPriceIndex));
    }

    // Tramo visible en fechas, para conservarlo al cambiar datos o capas (SPEC-05-03, RF-4)
    function currentViewMs(state) {
        var globals = state.chart.w.globals;
        return {
            start: msAt(state.series, Math.ceil(globals.minX)),
            end: msAt(state.series, Math.floor(globals.maxX))
        };
    }

    // Aplica un tramo del eje X al gráfico y al volumen
    function zoomTo(state, range) {
        if (!range || !(range.min < range.max)) {
            return;
        }
        state.chart.zoomX(range.min, range.max);
        if (state.volumeChart) {
            state.volumeChart.zoomX(range.min, range.max);
        }
        syncNavigator(state, range.min, range.max);
    }

    // La ventana del navegador sigue al zoom y al desplazamiento del gráfico (SPEC-21-01, RF-10)
    function syncNavigator(state, min, max) {
        if (!state.navChart || state.syncingNav || !isSet(min) || !isSet(max)) {
            return;
        }
        var current = state.navChart.w.config.chart.selection.xaxis || {};
        if (current.min === min && current.max === max) {
            return;
        }
        state.syncingNav = true;
        Promise.resolve(state.navChart.updateOptions({ chart: { selection: { xaxis: { min: min, max: max } } } }, false, false, false))
            .catch(function (error) { console.error('companyChart', error); })
            .then(function () { state.syncingNav = false; });
    }

    // Valor en base 100 (SPEC-05-04): «112,4»
    function formatIndexValue(value) {
        if (!isSet(value) || isNaN(value)) {
            return '';
        }
        return Number(value).toLocaleString('es-ES', { minimumFractionDigits: 1, maximumFractionDigits: 1 });
    }

    // ---------------------------------------------------------------- Comparación (SPEC-05-04)

    // Avisa al modal del tramo visible en fechas (ms) con una espera, para no hacerlo en cada píxel del zoom.
    // Solo si el tramo cambió: al redibujar las series el tramo es el mismo y no se vuelve a pedir
    function reportComparisonRange(state, immediate) {
        if (!state.comparison || !state.dotNet) {
            return;
        }
        if (state.reportTimer) {
            clearTimeout(state.reportTimer);
        }
        var send = function () {
            state.reportTimer = null;
            if (!state.comparison || !state.chart) {
                return;
            }
            var view = currentViewMs(state);
            var key = view.start + '-' + view.end;
            // Un tramo al revés es un gráfico aún sin dibujar: se espera al siguiente aviso
            if (!isSet(view.start) || !isSet(view.end) || view.start > view.end || key === state.reportedRange) {
                return;
            }
            state.reportedRange = key;
            state.dotNet.invokeMethodAsync('OnComparisonRangeChanged', view.start, view.end)
                .catch(function (error) { console.error('companyChart', error); });
        };
        if (immediate) {
            send();
        } else {
            state.reportTimer = setTimeout(send, 250);
        }
    }

    // Valor en base 100 de una serie de la comparación en un índice (null en el tramo futuro o sin dato)
    function comparisonValue(state, values, index) {
        var p = state.series[index];
        if (!p || p.future || !values || index >= values.length) {
            return null;
        }
        var value = values[index];
        return isSet(value) ? value : null;
    }

    // Marcas sobre las dos líneas de la comparación (RF-4b): un punto en cada una, a su valor en base 100
    function buildComparisonAnnotations(state) {
        var annotations = [];
        state.eventsByIndex = {};
        state.lineColorByIndex = {};
        state.hoverLineIndex = null;

        if (!state.showMarks) {
            return annotations;
        }

        (state.marks || []).forEach(function (k) {
            var i = indexAtOrBefore(state.series, k.date);
            if (i < 0) {
                return;
            }
            addEvent(state, i, markEventHtml(k));
            state.lineColorByIndex[i] = COLORS.mark;
            [state.comparison.main, state.comparison.compared].forEach(function (values) {
                var y = comparisonValue(state, values, i);
                if (!isSet(y)) {
                    return;
                }
                annotations.push({
                    x: i,
                    y: y,
                    marker: {
                        size: 6,
                        fillColor: k.isGlobal ? '#ffffff' : COLORS.mark,
                        strokeColor: COLORS.mark,
                        strokeWidth: 2,
                        shape: 'circle'
                    },
                    label: { text: '' }
                });
            });
        });

        return annotations;
    }

    // Tooltip del periodo en la comparación: las dos series en base 100 y las marcas del periodo
    function comparisonTooltip(state, index) {
        var p = state.series[index];
        if (!p) {
            return '';
        }
        setHoverLine(state, index);
        var c = state.comparison;
        var body;
        if (p.future) {
            body = 'Sin cotización (fecha futura)';
        } else {
            var main = comparisonValue(state, c.main, index);
            var compared = comparisonValue(state, c.compared, index);
            body = dot(COLORS.line, false) + escapeHtml(c.mainName) + ': ' + (isSet(main) ? formatIndexValue(main) : '—') + '<br/>' +
                dot(COLORS.compared, false) + escapeHtml(c.comparedName) + ': ' + (isSet(compared) ? formatIndexValue(compared) : '—');
        }
        return '<div class="company-chart-candle-tooltip">' +
            '<strong>' + escapeHtml(formatDate(p.x, state.granularity)) + '</strong><br/>' +
            body +
            eventsHtml(state, index) +
            '</div>';
    }

    // ---------------------------------------------------------------- Anotaciones

    function markerColor(kind) {
        switch (kind) {
            case 'Sell': return COLORS.sell;
            case 'Dividend': return COLORS.dividend;
            case 'Transfer': return COLORS.transfer;
            default: return COLORS.buy;
        }
    }

    // Quita el tooltip suelto de versiones anteriores si quedara alguno (el detalle va ahora en el del día)
    function hideTooltip() {
        var existing = document.getElementById('company-chart-op-tooltip');
        if (existing) {
            existing.remove();
        }
    }

    // Icono del tooltip con el mismo color y forma que el punto del gráfico (las ventas son cuadradas)
    function dot(color, hollow, square) {
        return '<span class="company-chart-tooltip-dot" style="border-color:' + color + ';background:' + (hollow ? 'transparent' : color) +
            (square ? ';border-radius:2px' : '') + '"></span>';
    }

    // Línea de una operación en el tooltip del día (SPEC-05-03): tipo, fecha real y detalle
    function operationEventHtml(m, symbol) {
        var head = dot(markerColor(m.kind), m.kind !== 'Dividend', m.kind === 'Sell') +
            '<strong>' + escapeHtml(KIND_LABELS[m.kind] || m.kind) + '</strong> · ' + escapeHtml(m.dateLabel);
        if (m.kind === 'Dividend') {
            return '<div>' + head + '<br/>Importe: ' + formatMoney(m.amount, symbol) + '</div>';
        }
        return '<div>' + head + '<br/>' +
            Number(m.shares).toLocaleString('es-ES', { maximumFractionDigits: 2 }) + ' acciones a ' + formatMoney(m.price, symbol) + ' · Importe: ' + formatMoney(m.amount, symbol) +
            '</div>';
    }

    // Línea de una marca en el tooltip del día: tipo y descripción completa (escapada)
    function markEventHtml(k) {
        return '<div>' + dot(COLORS.mark, k.isGlobal) +
            '<strong>Marca ' + (k.isGlobal ? 'global' : 'de empresa') + '</strong> · ' + escapeHtml(k.dateLabel) + '<br/>' +
            escapeHtml(k.description) +
            '</div>';
    }

    // Operaciones y marcas del periodo bajo el ratón, para añadirlas al tooltip de cotización
    function eventsHtml(state, index) {
        var events = state.eventsByIndex && state.eventsByIndex[index];
        if (!events || events.length === 0) {
            return '';
        }
        return '<div class="company-chart-tooltip-events">' + events.join('') + '</div>';
    }

    function addEvent(state, index, html) {
        (state.eventsByIndex[index] = state.eventsByIndex[index] || []).push(html);
    }

    // Línea de un pago de dividendo en el tooltip del día (SPEC-21-01, RF-9); el importe llega ya formateado
    function dividendEventHtml(d) {
        return '<div><span class="company-chart-tooltip-dot is-diamond" style="border-color:' + COLORS.dividend +
            ';background:' + (d.status === 'Cobrado' ? COLORS.dividend : 'transparent') + '"></span>' +
            '<strong>Pago de dividendo</strong> · ' + escapeHtml(d.paymentLabel) +
            (d.exLabel ? '<br/>Ex-dividendo: ' + escapeHtml(d.exLabel) : '') +
            '<br/>' + escapeHtml(d.status) + (d.totalText ? ' · Importe: ' + escapeHtml(d.totalText) : '') +
            '</div>';
    }

    // Rombo azul del pago de dividendo: hueco si está Pendiente y relleno si está Cobrado
    function diamondSvg(filled) {
        return '<svg width="14" height="14" viewBox="0 0 14 14"><path d="M7 1 L13 7 L7 13 L1 7 Z" fill="' +
            (filled ? COLORS.dividend : '#ffffff') + '" stroke="' + COLORS.dividend + '" stroke-width="2"/></svg>';
    }

    // Línea vertical discontinua del periodo bajo el ratón si tiene una marca o un pago de dividendo
    // (SPEC-05-03, RF-9; SPEC-21-01, RF-9). ApexCharts no avisa al pasar por un punto anotado (el gráfico se
    // queda el ratón), así que la gobierna el tooltip del día.
    function setHoverLine(state, index) {
        var color = isSet(index) && state.lineColorByIndex ? state.lineColorByIndex[index] : null;
        var key = color ? index : null;
        if (state.hoverLineIndex === key) {
            return;
        }
        state.hoverLineIndex = key;
        state.chart.removeAnnotation('mark-line');
        if (key !== null) {
            state.chart.addXaxisAnnotation({
                id: 'mark-line',
                x: key,
                borderColor: color,
                borderWidth: 2,
                strokeDashArray: 5,
                label: { text: '' }
            }, false);
        }
    }

    function buildPointAnnotations(state) {
        if (state.comparison) {
            return buildComparisonAnnotations(state);
        }

        var series = state.series;
        var hidden = state.hiddenKinds || [];
        var annotations = [];

        // Lo que se dibuja es lo que sale en el tooltip del día (tipos ocultos y marcas apagadas, fuera)
        state.eventsByIndex = {};
        state.lineColorByIndex = {};
        state.hoverLineIndex = null;

        (state.markers || []).forEach(function (m) {
            if (hidden.indexOf(m.kind) >= 0) {
                return;
            }
            var i = indexAtOrBefore(series, m.date);
            if (i < 0) {
                return;
            }
            var y = m.kind === 'Dividend' ? series[i].c : m.price;
            if (!isSet(y)) {
                return;
            }
            var color = markerColor(m.kind);
            addEvent(state, i, operationEventHtml(m, state.symbol));
            annotations.push({
                x: i,
                y: y,
                marker: {
                    size: m.kind === 'Dividend' ? 5 : 6,
                    fillColor: m.kind === 'Dividend' ? color : '#ffffff',
                    strokeColor: color,
                    strokeWidth: 2,
                    shape: m.kind === 'Sell' ? 'square' : 'circle'
                },
                label: { text: '' }
            });
        });

        if (state.showMarks) {
            (state.marks || []).forEach(function (k) {
                var i = indexAtOrBefore(series, k.date);
                if (i < 0) {
                    return;
                }
                addEvent(state, i, markEventHtml(k));
                state.lineColorByIndex[i] = state.lineColorByIndex[i] || COLORS.mark;
                annotations.push({
                    x: i,
                    y: series[i].c,
                    marker: {
                        size: 6,
                        // Empresa = relleno; Global = hueco (SPEC-05-03, D12)
                        fillColor: k.isGlobal ? '#ffffff' : COLORS.mark,
                        strokeColor: COLORS.mark,
                        strokeWidth: 2,
                        shape: 'circle'
                    },
                    label: { text: '' }
                });
            });

            // Pagos de dividendo (SPEC-21-01, RF-9): pasados sobre el cierre, futuros a la altura del último precio
            (state.dividendMarks || []).forEach(function (d) {
                var i = indexAtOrBefore(series, d.date);
                if (i < 0) {
                    return;
                }
                var y = !d.isFuture && i <= state.lastPriceIndex ? series[i].c : state.lastClose;
                if (!isSet(y)) {
                    return;
                }
                addEvent(state, i, dividendEventHtml(d));
                state.lineColorByIndex[i] = state.lineColorByIndex[i] || COLORS.dividend;
                annotations.push({
                    x: i,
                    y: y,
                    marker: { size: 0 },
                    customSVG: {
                        SVG: diamondSvg(d.status === 'Cobrado'),
                        cssClass: 'company-chart-dividend-mark',
                        offsetX: -7,
                        offsetY: -7
                    },
                    label: { text: '' }
                });
            });
        }

        return annotations;
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

    function buildYAxisAnnotations(state) {
        var annotations = [];
        // En la comparación no hay líneas de precio (RF-4b)
        if (state.comparison) {
            return annotations;
        }
        if (isSet(state.currentPrice)) {
            annotations.push(currentPriceAnnotation(state.currentPrice, state.symbol));
        }
        if (state.showAverage && isSet(state.averagePrice)) {
            annotations.push(averagePriceAnnotation(state.averagePrice, state.symbol));
        }
        return annotations;
    }

    // ---------------------------------------------------------------- Series

    // Las sesiones futuras van sin valor: ni línea, ni vela, ni volumen (SPEC-21-01, RF-10)
    function toSeriesData(series, candles) {
        return series.map(function (p, i) {
            if (p.future) {
                return { x: i, y: null };
            }
            return candles
                ? { x: i, y: [p.o, p.h, p.l, p.c] }
                : { x: i, y: p.c };
        });
    }

    // Series del gráfico principal: la cotización y, si hay tramo futuro, una serie auxiliar «Referencia» del mismo
    // tipo, oculta por CSS, que solo tiene valor (el último precio) en las sesiones futuras. Sin ella, un tramo solo
    // futuro deja la escala vacía (0–6) y los rombos de los pagos futuros quedan fuera. Es del mismo tipo porque
    // mezclar velas con una línea hace que ApexCharts empiece la escala en 0 (SPEC-21-01, RF-10)
    function chartSeries(state) {
        if (state.comparison) {
            return comparisonSeries(state);
        }

        var type = state.candles ? 'candlestick' : 'line';
        var main = { name: 'Cotización', type: type, data: toSeriesData(state.series, state.candles) };
        if (state.series.length - 1 <= state.lastPriceIndex || !isSet(state.lastClose)) {
            return [main];
        }
        var close = state.lastClose;
        return [main, {
            name: 'Referencia',
            type: type,
            data: state.series.map(function (p, i) {
                if (!p.future) {
                    return { x: i, y: null };
                }
                return state.candles ? { x: i, y: [close, close, close, close] } : { x: i, y: close };
            })
        }];
    }

    // Comparación (SPEC-05-04): la empresa y la comparada en base 100 y, con tramo futuro, la «Referencia» oculta con
    // el último valor de la empresa para que la escala no se quede vacía
    function comparisonSeries(state) {
        var c = state.comparison;
        var result = [
            { name: c.mainName, type: 'line', data: state.series.map(function (p, i) { return { x: i, y: comparisonValue(state, c.main, i) }; }) },
            { name: c.comparedName, type: 'line', data: state.series.map(function (p, i) { return { x: i, y: comparisonValue(state, c.compared, i) }; }) }
        ];
        if (state.series.length - 1 > state.lastPriceIndex) {
            var close = comparisonValue(state, c.main, state.lastPriceIndex);
            result.push({
                name: 'Referencia',
                type: 'line',
                data: state.series.map(function (p, i) { return { x: i, y: p.future && isSet(close) ? close : null }; })
            });
        }
        return result;
    }

    // Volumen en dinero aproximado, verde si el periodo cerró al alza y rojo si a la baja (SPEC-05-03, RF-1 y RF-3)
    function toVolumeData(series) {
        return series.map(function (p, i) {
            return p.future
                ? { x: i, y: null }
                : { x: i, y: p.tv, fillColor: p.c >= p.o ? COLORS.up : COLORS.down };
        });
    }

    // Serie del navegador: el cierre de cada periodo (vacía en el futuro)
    function toNavigatorData(series) {
        return series.map(function (p, i) {
            return { x: i, y: p.future ? null : p.c };
        });
    }

    // Sombreado claro del tramo futuro en el navegador
    function futureAnnotations(state) {
        var last = state.series.length - 1;
        if (last <= state.lastPriceIndex) {
            return [];
        }
        return [{
            x: state.lastPriceIndex + 0.5,
            x2: last + 0.5,
            fillColor: COLORS.future,
            opacity: 0.25,
            label: { text: 'Futuro', orientation: 'horizontal', position: 'top', textAnchor: 'start', borderWidth: 0, style: { background: 'transparent', color: '#5F6475' } }
        }];
    }

    // Copia en el estado los datos del modal que no dependen del dibujo
    function applyData(state, data) {
        var series = (data.series || []).slice();
        state.lastPriceIndex = series.length - 1;
        state.lastClose = series.length > 0 ? series[series.length - 1].c : null;
        (data.futureSessions || []).forEach(function (ms) {
            series.push({ x: ms, o: null, h: null, l: null, c: null, v: 0, tv: null, future: true });
        });
        state.series = series;
        state.dividendMarks = data.dividendMarks || [];
        state.markers = data.markers || [];
        state.marks = data.marks || [];
        state.showMarks = !!data.showMarks;
        state.hiddenKinds = data.hiddenKinds || [];
        state.currentPrice = isSet(data.currentPrice) ? data.currentPrice : null;
        state.averagePrice = isSet(data.averagePrice) ? data.averagePrice : null;
        state.showAverage = !!data.showAverage;
        state.viewStart = isSet(data.viewStart) ? data.viewStart : null;
        state.viewEnd = isSet(data.viewEnd) ? data.viewEnd : null;
        state.comparison = data.comparison || null;
    }

    // ---------------------------------------------------------------- API

    // dotNetRef (opcional): el modal, que recibe el tramo visible mientras se compara (SPEC-05-04)
    function render(elementId, volumeElementId, dataJson, navigatorElementId, dotNetRef) {
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
        // En la comparación no hay velas ni volumen (RF-4b)
        var comparing = !!data.comparison;
        var candles = !!data.showCandles && !comparing;
        var showVolume = !!data.showVolume && !comparing;
        var granularity = data.granularity || 'Daily';
        var group = 'company-chart-' + elementId;

        // El estado se crea antes del gráfico: etiquetas y tooltips leen state.series, que cambia con updateData
        var state = {
            chart: null,
            volumeChart: null,
            candles: candles,
            isDaily: !!data.isDaily,
            symbol: symbol,
            granularity: granularity,
            dotNet: dotNetRef || null
        };
        applyData(state, data);

        var xaxis = {
            type: 'numeric',
            tickAmount: 8,
            labels: {
                hideOverlappingLabels: true,
                formatter: function (value) {
                    var ms = msAt(state.series, value);
                    return isSet(ms) ? formatAxisDate(ms, granularity) : '';
                }
            },
            tooltip: { enabled: false }
        };

        var options = {
            chart: {
                id: elementId,
                group: showVolume ? group : undefined,
                type: candles ? 'candlestick' : 'line',
                height: 420,
                animations: { enabled: false },
                zoom: { enabled: true, type: 'x', autoScaleYaxis: true },
                toolbar: { show: true, tools: { zoom: true, zoomin: true, zoomout: true, pan: true, reset: true, download: false } },
                events: {
                    // El navegador sigue al zoom y al desplazamiento; al salir, se quita la línea de la marca
                    zoomed: function (ctx, e) { syncNavigator(state, e.xaxis.min, e.xaxis.max); reportComparisonRange(state); },
                    scrolled: function (ctx, e) { syncNavigator(state, e.xaxis.min, e.xaxis.max); reportComparisonRange(state); },
                    mouseLeave: function () { setHoverLine(state, null); },
                    // El navegador mueve el gráfico con updateOptions (sin «zoomed»): también se avisa aquí; el aviso
                    // solo sale si el tramo cambió, así que redibujar la comparación no lo repite (SPEC-05-04)
                    updated: function () { reportComparisonRange(state); }
                }
            },
            series: chartSeries(state),
            // La serie auxiliar no se ve ni sale en la leyenda
            legend: { show: false },
            markers: { size: 0 },
            xaxis: xaxis,
            yaxis: {
                labels: {
                    minWidth: 70,
                    formatter: function (value) { return comparing ? formatIndexValue(value) : formatMoney(value, symbol); }
                },
                tooltip: { enabled: false }
            },
            stroke: comparing
                ? { curve: 'straight', width: [2, 2, 0] }
                : candles ? { width: [1, 0] } : { curve: 'straight', width: [2, 0] },
            colors: comparing ? [COLORS.line, COLORS.compared, 'transparent'] : [COLORS.line, 'transparent'],
            plotOptions: {
                candlestick: { colors: { upward: COLORS.up, downward: COLORS.down } }
            },
            // Tooltip del periodo: cotización y, debajo, sus operaciones y marcas (SPEC-05-03, RF-9)
            tooltip: {
                custom: function (ctx) {
                    if (state.comparison) {
                        return comparisonTooltip(state, ctx.dataPointIndex);
                    }
                    var p = state.series[ctx.dataPointIndex];
                    if (!p) {
                        return '';
                    }
                    setHoverLine(state, ctx.dataPointIndex);
                    var prices = p.future
                        ? 'Sin cotización (fecha futura)'
                        : candles
                        ? 'Apertura: ' + formatMoney(p.o, symbol) + '<br/>' +
                          'Máximo: ' + formatMoney(p.h, symbol) + '<br/>' +
                          'Mínimo: ' + formatMoney(p.l, symbol) + '<br/>' +
                          'Cierre: ' + formatMoney(p.c, symbol)
                        : 'Cierre: ' + formatMoney(p.c, symbol);
                    return '<div class="company-chart-candle-tooltip">' +
                        '<strong>' + escapeHtml(formatDate(p.x, granularity)) + '</strong><br/>' +
                        prices +
                        eventsHtml(state, ctx.dataPointIndex) +
                        '</div>';
                }
            },
            annotations: {
                points: buildPointAnnotations(state),
                yaxis: buildYAxisAnnotations(state)
            }
        };

        state.chart = new ApexCharts(container, options);
        var renders = [state.chart.render()];

        var volumeContainer = volumeElementId ? document.getElementById(volumeElementId) : null;
        if (showVolume && volumeContainer) {
            state.volumeChart = new ApexCharts(volumeContainer, {
                chart: {
                    id: elementId + '-volume',
                    group: group,
                    type: 'bar',
                    height: 130,
                    animations: { enabled: false },
                    toolbar: { show: false },
                    zoom: { enabled: false }
                },
                series: [{ name: 'Volumen', data: toVolumeData(state.series) }],
                xaxis: { type: 'numeric', labels: { show: false }, tooltip: { enabled: false } },
                yaxis: {
                    labels: {
                        minWidth: 70,
                        formatter: function (value) { return formatCompactMoney(value, symbol); }
                    }
                },
                dataLabels: { enabled: false },
                stroke: { width: 0 },
                plotOptions: { bar: { columnWidth: '80%' } },
                tooltip: {
                    custom: function (ctx) {
                        var p = state.series[ctx.dataPointIndex];
                        if (!p || p.future) {
                            return '';
                        }
                        return '<div class="company-chart-candle-tooltip">' +
                            '<strong>' + escapeHtml(formatDate(p.x, granularity)) + '</strong><br/>' +
                            'Negociado: ≈ ' + formatCompactMoney(p.tv, symbol) + '<br/>' +
                            Number(p.v).toLocaleString('es-ES') + ' acciones' +
                            '</div>';
                    }
                }
            });
            renders.push(state.volumeChart.render());
        }

        charts[elementId] = state;

        // Tramo inicial de la temporalidad o, al cambiar una capa, el que se estaba mirando (RF-4).
        // El «reset» de ApexCharts sigue enseñando todo lo cargado. Las actualizaciones posteriores se
        // encadenan a state.ready para no leer el zoom antes de aplicarlo.
        // El navegador se crea cuando ya existen sus destinos (gráfico y volumen)
        var navContainer = navigatorElementId ? document.getElementById(navigatorElementId) : null;
        var keepView = data.keepView || null;
        state.ready = Promise.all(renders)
            .then(function () {
                if (charts[elementId] !== state || !navContainer) {
                    return;
                }
                state.navChart = new ApexCharts(navContainer, navigatorOptions(state, elementId, granularity));
                return state.navChart.render();
            })
            .then(function () {
                if (charts[elementId] !== state) {
                    return;
                }
                zoomTo(state, keepView ? rangeFromMs(state, keepView.start, keepView.end) : initialRange(state));
                reportComparisonRange(state);
            });
    }

    // Minigráfico navegador (SPEC-21-01, RF-10): todo el histórico y el tramo futuro, con una ventana que se arrastra
    // o se estira; el «brush» de ApexCharts mueve el gráfico y el volumen
    function navigatorOptions(state, elementId, granularity) {
        var targets = state.volumeChart ? [elementId, elementId + '-volume'] : [elementId];
        return {
            chart: {
                id: elementId + '-nav',
                type: 'area',
                height: 90,
                animations: { enabled: false },
                toolbar: { show: false },
                zoom: { enabled: false },
                brush: { enabled: true, targets: targets, autoScaleYaxis: true },
                // El brush no dispara «zoomed» en el gráfico: la comparación se recalcula desde aquí (SPEC-05-04)
                events: { brushScrolled: function () { reportComparisonRange(state); } },
                selection: {
                    enabled: true,
                    xaxis: { min: -0.5, max: Math.max(0.5, state.lastPriceIndex + 0.5) },
                    fill: { color: COLORS.line, opacity: 0.1 },
                    stroke: { color: COLORS.line }
                }
            },
            series: [{ name: 'Cierre', data: toNavigatorData(state.series) }],
            xaxis: {
                type: 'numeric',
                tickAmount: 6,
                labels: {
                    hideOverlappingLabels: true,
                    formatter: function (value) {
                        var ms = msAt(state.series, value);
                        return isSet(ms) ? formatAxisDate(ms, granularity) : '';
                    }
                },
                tooltip: { enabled: false }
            },
            // Mismo ancho de etiquetas que el gráfico para que los ejes X queden alineados
            yaxis: { tickAmount: 2, labels: { minWidth: 70, formatter: function () { return ''; } } },
            colors: [COLORS.line],
            stroke: { width: 1 },
            fill: { type: 'solid', opacity: 0.15 },
            dataLabels: { enabled: false },
            tooltip: { enabled: false },
            annotations: { xaxis: futureAnnotations(state) }
        };
    }

    // Ejecuta una actualización cuando el gráfico ya está dibujado y la anterior ha terminado
    function whenReady(elementId, state, action) {
        state.ready = state.ready
            .then(function () {
                if (charts[elementId] === state) {
                    return action();
                }
            })
            .catch(function (error) {
                console.error('companyChart', error);
            });
    }

    // Tramo visible en fechas (ms) para que el modal lo conserve al redibujar; null sin gráfico
    function getView(elementId) {
        var state = charts[elementId];
        return state ? currentViewMs(state) : null;
    }

    // Sustituye los datos (bloques de histórico, marcas nuevas…) sin recrear el gráfico y vuelve a
    // aplicar el tramo que el usuario estaba mirando (SPEC-05-02, RF-4). Devuelve false si no hay
    // gráfico dibujado (el modal lo dibuja entonces desde cero).
    function updateData(elementId, dataJson) {
        var state = charts[elementId];
        if (!state) {
            return false;
        }

        var data = JSON.parse(dataJson);

        whenReady(elementId, state, function () {
            // Los bloques antiguos añaden puntos al principio: el tramo se guarda en fechas, no en índices
            var view = currentViewMs(state);
            applyData(state, data);

            // El último false evita que ApexCharts copie serie y anotaciones al volumen (mismo grupo)
            var updates = [state.chart.updateOptions({
                series: chartSeries(state),
                annotations: {
                    points: buildPointAnnotations(state),
                    yaxis: buildYAxisAnnotations(state)
                }
            }, false, false, false)];

            if (state.volumeChart) {
                updates.push(state.volumeChart.updateSeries([{ name: 'Volumen', data: toVolumeData(state.series) }], false));
            }

            // El navegador se amplía con los bloques antiguos; la ventana la recoloca zoomTo
            if (state.navChart) {
                updates.push(state.navChart.updateOptions({
                    series: [{ name: 'Cierre', data: toNavigatorData(state.series) }],
                    annotations: { xaxis: futureAnnotations(state) }
                }, false, false, false));
            }

            return Promise.all(updates).then(function () {
                zoomTo(state, rangeFromMs(state, view.start, view.end));
                // Un bloque nuevo puede cambiar la base del tramo: se vuelve a pedir aunque el tramo sea el mismo
                state.reportedRange = null;
                reportComparisonRange(state, true);
            });
        });

        return true;
    }

    // Sustituye las dos series en base 100 de la comparación sin mover la vista (SPEC-05-04, RF-4)
    function setComparison(elementId, json) {
        var state = charts[elementId];
        if (!state || !state.comparison) {
            return;
        }

        var values = JSON.parse(json) || {};

        whenReady(elementId, state, function () {
            if (!state.comparison) {
                return;
            }
            // Mismo tramo exacto (no en fechas): rehacerlo desde fechas añade margen y lo alargaría en cada aviso
            var range = { min: state.chart.w.globals.minX, max: state.chart.w.globals.maxX };
            state.comparison.main = values.main || [];
            state.comparison.compared = values.compared || [];
            return Promise.resolve(state.chart.updateOptions({
                series: chartSeries(state),
                annotations: {
                    points: buildPointAnnotations(state),
                    yaxis: buildYAxisAnnotations(state)
                }
            }, false, false, false)).then(function () {
                zoomTo(state, range);
            });
        });
    }

    // Oculta los tipos de operación indicados (leyenda que filtra, SPEC-05-03, RF-7) sin mover la vista
    function setHiddenKinds(elementId, kindsJson) {
        var state = charts[elementId];
        if (!state) {
            return;
        }

        var kinds = JSON.parse(kindsJson) || [];

        whenReady(elementId, state, function () {
            var view = currentViewMs(state);
            state.hiddenKinds = kinds;
            hideTooltip();
            return Promise.resolve(state.chart.updateOptions({
                annotations: {
                    points: buildPointAnnotations(state),
                    yaxis: buildYAxisAnnotations(state)
                }
            }, false, false, false)).then(function () {
                zoomTo(state, rangeFromMs(state, view.start, view.end));
            });
        });
    }

    // Vuelve al tramo inicial de la temporalidad activa (botón «Vista inicial»)
    function resetView(elementId) {
        var state = charts[elementId];
        if (!state) {
            return;
        }

        whenReady(elementId, state, function () {
            zoomTo(state, initialRange(state) || pricedRange(state));
        });
    }

    // Mueve la línea «Precio actual» y, en vista diaria, actualiza el punto de hoy con el precio en vivo
    // (cierre provisional, no se guarda). No recrea el gráfico.
    function updateLive(elementId, price, todayMs) {
        var state = charts[elementId];
        // En la comparación no hay línea de precio y la base 100 no se mueve hasta el siguiente recálculo
        if (!state || !isSet(price) || state.comparison) {
            return;
        }

        state.currentPrice = price;
        state.chart.removeAnnotation('current-price');
        state.chart.addYaxisAnnotation(currentPriceAnnotation(price, state.symbol));

        if (!state.isDaily) {
            return;
        }

        whenReady(elementId, state, function () {
            // El último precio no es el último punto si hay sesiones futuras (SPEC-21-01)
            var series = state.series;
            var previousLastIndex = state.lastPriceIndex;
            var last = previousLastIndex >= 0 ? series[previousLastIndex] : null;
            var next = series[previousLastIndex + 1];
            if (last && last.x === todayMs) {
                if (last.c === price) {
                    return;
                }
                last.c = price;
                last.h = Math.max(last.h, price);
                last.l = Math.min(last.l, price);
            } else if (next && next.future && next.x === todayMs) {
                // Hoy ya era una sesión futura vacía: pasa a tener precio
                next.future = false;
                next.o = next.h = next.l = next.c = price;
                next.tv = 0;
                state.lastPriceIndex = previousLastIndex + 1;
            } else if (!next && (!last || last.x < todayMs)) {
                series.push({ x: todayMs, o: price, h: price, l: price, c: price, v: 0, tv: 0 });
                state.lastPriceIndex = series.length - 1;
            } else {
                return;
            }
            state.lastClose = series[state.lastPriceIndex].c;

            // updateSeries puede reiniciar el zoom: se conserva el tramo que se estaba mirando (SPEC-05-02);
            // si ese tramo llegaba al final, se alarga para que el punto de hoy se vea
            var range = { min: state.chart.w.globals.minX, max: state.chart.w.globals.maxX };
            if (state.lastPriceIndex > previousLastIndex && range.max >= previousLastIndex && range.max < state.lastPriceIndex + 0.5) {
                range.max = state.lastPriceIndex + 1;
            }
            return Promise.resolve(state.chart.updateSeries(chartSeries(state), false))
                .then(function () {
                    zoomTo(state, range);
                });
        });
    }

    function destroy(elementId) {
        var state = charts[elementId];
        if (state) {
            if (state.reportTimer) {
                clearTimeout(state.reportTimer);
            }
            if (state.navChart) {
                state.navChart.destroy();
            }
            if (state.volumeChart) {
                state.volumeChart.destroy();
            }
            state.chart.destroy();
            delete charts[elementId];
        }
        hideTooltip();
    }

    return {
        render: render,
        getView: getView,
        updateData: updateData,
        setComparison: setComparison,
        setHiddenKinds: setHiddenKinds,
        resetView: resetView,
        updateLive: updateLive,
        destroy: destroy
    };
})();
