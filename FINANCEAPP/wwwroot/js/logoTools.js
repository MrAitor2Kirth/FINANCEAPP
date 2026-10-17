// Utilidades del logo de la app: recorta los márgenes de una imagen en el navegador (canvas)
// y la reduce antes de guardarla. Se invoca desde Settings.razor por interop.
window.logoTools = (function () {
    var WORK_SIDE = 1600;   // lado mayor del lienzo de trabajo (limita la memoria con imágenes enormes)
    var TOLERANCE = 36;     // distancia RGB al fondo a partir de la cual un píxel cuenta como contenido
    var PADDING = 0.04;     // respiro alrededor del contenido (fracción del lado mayor)
    var WHITEN_LOW = 14;    // por debajo de esta distancia al fondo el píxel pasa a blanco puro
    var WHITEN_HIGH = 64;   // por encima de esta distancia el píxel no se toca (entre ambas, mezcla progresiva)

    function loadImage(src) {
        return new Promise(function (resolve, reject) {
            var img = new Image();
            img.onload = function () { resolve(img); };
            img.onerror = function () { reject(new Error('decode')); };
            img.src = src;
        });
    }

    function makeCanvas(w, h) {
        var canvas = document.createElement('canvas');
        canvas.width = w;
        canvas.height = h;
        return canvas;
    }

    // Color medio de un bloque pequeño de la esquina indicada
    function cornerColor(data, w, h, cx, cy) {
        var size = Math.max(1, Math.min(5, w, h));
        var x0 = cx === 0 ? 0 : w - size;
        var y0 = cy === 0 ? 0 : h - size;
        var r = 0, g = 0, b = 0, a = 0, n = 0;
        for (var y = y0; y < y0 + size; y++) {
            for (var x = x0; x < x0 + size; x++) {
                var i = (y * w + x) * 4;
                r += data[i]; g += data[i + 1]; b += data[i + 2]; a += data[i + 3];
                n++;
            }
        }
        return { r: r / n, g: g / n, b: b / n, a: a / n };
    }

    function lerp(a, b, t) { return a + (b - a) * t; }

    // Fondo esperado en (x, y): interpolación bilineal de las cuatro esquinas (admite degradados suaves)
    function backgroundAt(c, u, v, channel) {
        var top = lerp(c.tl[channel], c.tr[channel], u);
        var bottom = lerp(c.bl[channel], c.br[channel], u);
        return lerp(top, bottom, v);
    }

    async function processLogo(base64, contentType, maxSide, maxInputSide) {
        try {
            var img = await loadImage('data:' + contentType + ';base64,' + base64);
            var w = img.naturalWidth, h = img.naturalHeight;
            if (!w || !h) { return { error: 'decode' }; }
            if (w > maxInputSide || h > maxInputSide) { return { error: 'too-large' }; }

            // 1. Lienzo de trabajo reducido
            var k = Math.min(1, WORK_SIDE / Math.max(w, h));
            var ww = Math.max(1, Math.round(w * k));
            var wh = Math.max(1, Math.round(h * k));
            var work = makeCanvas(ww, wh);
            var ctx = work.getContext('2d', { willReadFrequently: true });
            ctx.drawImage(img, 0, 0, ww, wh);
            var imageData = ctx.getImageData(0, 0, ww, wh);
            var data = imageData.data;

            // 2. Fondo a partir de las esquinas
            var corners = {
                tl: cornerColor(data, ww, wh, 0, 0),
                tr: cornerColor(data, ww, wh, 1, 0),
                bl: cornerColor(data, ww, wh, 0, 1),
                br: cornerColor(data, ww, wh, 1, 1)
            };
            var avgAlpha = (corners.tl.a + corners.tr.a + corners.bl.a + corners.br.a) / 4;
            var transparentBg = avgAlpha < 16;

            // 3. Caja mínima del contenido
            var minX = ww, minY = wh, maxX = -1, maxY = -1;
            var du = ww > 1 ? 1 / (ww - 1) : 0;
            var dv = wh > 1 ? 1 / (wh - 1) : 0;
            for (var y = 0; y < wh; y++) {
                var v = y * dv;
                for (var x = 0; x < ww; x++) {
                    var i = (y * ww + x) * 4;
                    var alpha = data[i + 3];
                    if (alpha <= 16) { continue; }
                    var isContent = true;
                    if (!transparentBg) {
                        var u = x * du;
                        var dr = data[i] - backgroundAt(corners, u, v, 'r');
                        var dg = data[i + 1] - backgroundAt(corners, u, v, 'g');
                        var db = data[i + 2] - backgroundAt(corners, u, v, 'b');
                        isContent = Math.sqrt(dr * dr + dg * dg + db * db) > TOLERANCE;
                    }
                    if (isContent) {
                        if (x < minX) { minX = x; }
                        if (x > maxX) { maxX = x; }
                        if (y < minY) { minY = y; }
                        if (y > maxY) { maxY = y; }
                    }
                }
            }

            // Imagen toda de un color: se devuelve la original sin recortar
            if (maxX < 0 || maxY < 0) {
                return { base64: base64, contentType: contentType, width: w, height: h, trimmed: false };
            }

            // 4. El fondo opaco se lleva a blanco puro (con mezcla progresiva en los bordes) para que
            //    el logo se funda con la placa blanca de la interfaz sin dejar un recuadro grisáceo
            if (!transparentBg) {
                for (var y2 = 0; y2 < wh; y2++) {
                    var v2 = y2 * dv;
                    for (var x2 = 0; x2 < ww; x2++) {
                        var j = (y2 * ww + x2) * 4;
                        var u2 = x2 * du;
                        var er = data[j] - backgroundAt(corners, u2, v2, 'r');
                        var eg = data[j + 1] - backgroundAt(corners, u2, v2, 'g');
                        var eb = data[j + 2] - backgroundAt(corners, u2, v2, 'b');
                        var dist = Math.sqrt(er * er + eg * eg + eb * eb);
                        if (dist < WHITEN_HIGH) {
                            var weight = dist <= WHITEN_LOW ? 1 : 1 - (dist - WHITEN_LOW) / (WHITEN_HIGH - WHITEN_LOW);
                            data[j] += (255 - data[j]) * weight;
                            data[j + 1] += (255 - data[j + 1]) * weight;
                            data[j + 2] += (255 - data[j + 2]) * weight;
                        }
                    }
                }
                ctx.putImageData(imageData, 0, 0);
            }

            // 5. Respiro, recorte y reducción
            var cw = maxX - minX + 1;
            var ch = maxY - minY + 1;
            var pad = Math.round(PADDING * Math.max(cw, ch));
            var sx = Math.max(0, minX - pad);
            var sy = Math.max(0, minY - pad);
            var sw = Math.min(ww, maxX + pad + 1) - sx;
            var sh = Math.min(wh, maxY + pad + 1) - sy;

            var s = Math.min(1, maxSide / Math.max(sw, sh));
            var ow = Math.max(1, Math.round(sw * s));
            var oh = Math.max(1, Math.round(sh * s));
            var out = makeCanvas(ow, oh);
            var octx = out.getContext('2d');
            octx.imageSmoothingQuality = 'high';
            octx.drawImage(work, sx, sy, sw, sh, 0, 0, ow, oh);

            var dataUrl = out.toDataURL('image/png');
            return {
                base64: dataUrl.substring(dataUrl.indexOf(',') + 1),
                contentType: 'image/png',
                width: ow,
                height: oh,
                trimmed: true
            };
        } catch (e) {
            return { error: e && e.message === 'decode' ? 'decode' : 'failed' };
        }
    }

    return { processLogo: processLogo };
})();
