/* ==========================================================================
   LogiTrack Solutions - Motor de mapas, rutas y animación de entregas
   Leaflet 1.9.4 (local) + OSRM demo (https://router.project-osrm.org)
   Si OSRM no responde se genera una ruta estimada para que la demo siga funcionando.
   ========================================================================== */
(function () {
    "use strict";

    var SANTA_CRUZ = { lat: -17.783700, lng: -63.180426 };
    var ZOOM_INICIAL = 13;
    var mapas = {};

    if (window.L) {
        L.Icon.Default.mergeOptions({
            iconUrl: "/lib/leaflet/images/marker-icon.png",
            iconRetinaUrl: "/lib/leaflet/images/marker-icon-2x.png",
            shadowUrl: "/lib/leaflet/images/marker-shadow.png",
            iconSize: [25, 41],
            iconAnchor: [12, 41]
        });
    }

    function estado(id) {
        var e = mapas[id];
        if (!e || !e.map) {
            throw new Error("El mapa '" + id + "' no está inicializado.");
        }
        return e;
    }

    function puntoValido(p) {
        return p && typeof p.lat === "number" && typeof p.lng === "number" &&
            isFinite(p.lat) && isFinite(p.lng) &&
            p.lat >= -90 && p.lat <= 90 && p.lng >= -180 && p.lng <= 180;
    }

    function haversine(a, b) {
        var r = 6371.0088;
        var dLat = (b.lat - a.lat) * Math.PI / 180;
        var dLng = (b.lng - a.lng) * Math.PI / 180;
        var lat1 = a.lat * Math.PI / 180;
        var lat2 = b.lat * Math.PI / 180;
        var h = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLng / 2) * Math.sin(dLng / 2);
        return r * 2 * Math.atan2(Math.sqrt(h), Math.sqrt(1 - h));
    }

    function longitudRuta(puntos) {
        var total = 0;
        for (var i = 1; i < puntos.length; i++) {
            total += haversine(puntos[i - 1], puntos[i]);
        }
        return total;
    }

    function icono(tipo) {
        var color = tipo === "origen" ? "#0B6E4F" : (tipo === "destino" ? "#F4A300" : "#1565C0");
        var letra = tipo === "origen" ? "A" : (tipo === "destino" ? "B" : "▶");
        return L.divIcon({
            className: "lt-marker",
            html: '<div class="lt-marker-pin" style="background:' + color + '">' + letra + "</div>",
            iconSize: [30, 42],
            iconAnchor: [15, 40]
        });
    }

    function limpiarMarcadores(e) {
        if (e.capas) {
            e.map.removeLayer(e.capas);
        }
        e.capas = L.layerGroup().addTo(e.map);
        e.marcadorVehiculo = null;
        e.lineaRuta = null;
    }

    function dibujarLinea(e, puntos, punteada) {
        if (!puntos || puntos.length < 2) return;
        if (e.lineaRuta) {
            e.map.removeLayer(e.lineaRuta);
        }
        e.lineaRuta = L.polyline(puntos, {
            color: punteada ? "#78909C" : "#0B6E4F",
            weight: punteada ? 3 : 5,
            opacity: punteada ? 0.7 : 0.95,
            dashArray: punteada ? "6 8" : null,
            lineCap: "round"
        }).addTo(e.map);
    }

    function ajustarVista(e, puntos) {
        if (!puntos || puntos.length === 0) return;
        var validos = puntos.filter(puntoValido);
        if (validos.length === 0) return;
        if (validos.length === 1) {
            e.map.setView([validos[0].lat, validos[0].lng], ZOOM_INICIAL);
        } else {
            e.map.fitBounds(L.latLngBounds(validos.map(function (p) { return [p.lat, p.lng]; })).pad(0.18));
        }
        setTimeout(function () { e.map.invalidateSize(); }, 60);
    }

    function rutaEstimada(origen, waypoints, destino) {
        var puntosBase = [origen].concat(waypoints || []).concat([destino]);
        var puntos = [];
        for (var i = 0; i < puntosBase.length - 1; i++) {
            var a = puntosBase[i];
            var b = puntosBase[i + 1];
            var pasos = 12;
            for (var j = 0; j < pasos; j++) {
                var t = j / pasos;
                puntos.push({
                    lat: a.lat + (b.lat - a.lat) * t,
                    lng: a.lng + (b.lng - a.lng) * t
                });
            }
        }
        puntos.push(destino);
        var distancia = longitudRuta(puntos) * 1.22; // factor vial urbano
        return {
            puntos: puntos,
            distanciaKm: Math.round(distancia * 100) / 100,
            duracionMin: Math.max(2, Math.ceil(distancia / 32 * 60)),
            origen: "estimada"
        };
    }

    function detenerAnimacion(e) {
        if (e.animacion) {
            e.animacion.activa = false;
            e.animacion = null;
        }
    }

    var api = {
        inicializar: function (id, dotNetRef, opciones) {
            opciones = opciones || {};
            if (mapas[id] && mapas[id].map) {
                mapas[id].map.remove();
            }
            var centro = puntoValido(opciones.centro)
                ? [opciones.centro.lat, opciones.centro.lng]
                : [SANTA_CRUZ.lat, SANTA_CRUZ.lng];

            var map = L.map(id, {
                center: centro,
                zoom: opciones.zoom || ZOOM_INICIAL,
                scrollWheelZoom: opciones.scrollWheelZoom !== false,
                attributionControl: true
            });

            L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
                maxZoom: 19,
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
            }).addTo(map);

            var e = {
                map: map,
                capas: L.layerGroup().addTo(map),
                dotNet: dotNetRef,
                opciones: opciones,
                seleccionable: !!opciones.seleccionable,
                animacion: null
            };
            mapas[id] = e;

            map.on("click", function (ev) {
                if (e.seleccionable && e.dotNet) {
                    e.dotNet.invokeMethodAsync("OnMapaClick", ev.latlng.lat, ev.latlng.lng);
                }
            });

            setTimeout(function () { map.invalidateSize(); }, 80);
            return true;
        },

        establecerModoSeleccion: function (id, seleccionable) {
            estado(id).seleccionable = !!seleccionable;
        },

        establecerPuntos: function (id, origen, destino) {
            var e = estado(id);
            limpiarMarcadores(e);
            var puntos = [];

            if (puntoValido(origen)) {
                L.marker([origen.lat, origen.lng], { icon: icono("origen"), title: "Origen" })
                    .bindPopup("Origen de la ruta")
                    .addTo(e.capas);
                puntos.push(origen);
            }
            if (puntoValido(destino)) {
                L.marker([destino.lat, destino.lng], { icon: icono("destino"), draggable: e.seleccionable })
                    .bindPopup("Destino de la entrega")
                    .addTo(e.capas)
                    .on("dragend", function (ev) {
                        var p = ev.target.getLatLng();
                        if (e.dotNet) {
                            e.dotNet.invokeMethodAsync("OnMapaClick", p.lat, p.lng);
                        }
                    });
                puntos.push(destino);
            }

            if (puntoValido(origen) && puntoValido(destino)) {
                var distancia = haversine(origen, destino) * 1.22;
                dibujarLinea(e, [
                    [origen.lat, origen.lng],
                    [destino.lat, destino.lng]
                ], true);
            }

            ajustarVista(e, puntos);
        },

        trazarRuta: function (id, origen, destino, waypoints) {
            var e = estado(id);
            if (!puntoValido(origen) || !puntoValido(destino)) {
                return Promise.resolve(null);
            }

            var coords = [origen].concat(waypoints || []).concat([destino])
                .map(function (p) { return p.lng.toFixed(6) + "," + p.lat.toFixed(6); })
                .join(";");
            var url = "https://router.project-osrm.org/route/v1/driving/" + coords +
                "?overview=full&geometries=geojson&steps=false";

            return fetch(url, { mode: "cors", cache: "no-store" })
                .then(function (r) {
                    if (!r.ok) throw new Error("OSRM " + r.status);
                    return r.json();
                })
                .then(function (json) {
                    if (!json.routes || json.routes.length === 0) {
                        throw new Error("Sin ruta OSRM");
                    }
                    var ruta = json.routes[0];
                    var puntos = ruta.geometry.coordinates.map(function (c) {
                        return { lat: c[1], lng: c[0] };
                    });
                    var resultado = {
                        puntos: puntos,
                        distanciaKm: Math.round(ruta.distance / 100 * 100) / 100,
                        duracionMin: Math.max(2, Math.ceil(ruta.duration / 60)),
                        origen: "osrm"
                    };
                    e.ultimaRuta = resultado;
                    dibujarLinea(e, puntos.map(function (p) { return [p.lat, p.lng]; }), false);
                    ajustarVista(e, puntos);
                    return resultado;
                })
                .catch(function () {
                    var resultado = rutaEstimada(origen, waypoints || [], destino);
                    e.ultimaRuta = resultado;
                    dibujarLinea(e, resultado.puntos.map(function (p) { return [p.lat, p.lng]; }), true);
                    ajustarVista(e, resultado.puntos);
                    return resultado;
                });
        },

        animar: function (id, opciones) {
            var e = estado(id);
            detenerAnimacion(e);
            var puntos = (e.ultimaRuta && e.ultimaRuta.puntos) || [];
            if (puntos.length < 2) return false;

            if (e.marcadorVehiculo) {
                e.map.removeLayer(e.marcadorVehiculo);
            }
            e.marcadorVehiculo = L.marker([puntos[0].lat, puntos[0].lng], {
                icon: icono("vehiculo"),
                title: "Vehículo en ruta"
            }).addTo(e.capas);

            e.animacion = {
                puntos: puntos,
                distanciaKm: e.ultimaRuta.distanciaKm,
                duracionRealMs: (e.ultimaRuta.duracionMin || 10) * 60000,
                duracionDemoMs: Math.max(6000, Math.min(30000, (e.ultimaRuta.duracionMin || 10) * 1400)),
                velocidad: opciones.velocidad || 1,
                transcurrido: opciones.desde || 0,
                activa: true,
                ultimoTick: performance.now(),
                ultimoEnvio: 0,
                notificar: function (progreso, punto, restanteKm, restanteMin) {
                    if (e.dotNet) {
                        e.dotNet.invokeMethodAsync("OnProgreso", progreso, punto.lat, punto.lng, restanteKm, restanteMin);
                    }
                }
            };

            var anim = e.animacion;
            function tick(now) {
                if (mapas[id] !== e || e.animacion !== anim) return;
                var delta = now - anim.ultimoTick;
                anim.ultimoTick = now;
                if (anim.activa) {
                    anim.transcurrido += delta * anim.velocidad;
                }
                var progreso = Math.min(1, anim.transcurrido / anim.duracionDemoMs);
                var indice = progreso * (anim.puntos.length - 1);
                var i = Math.min(anim.puntos.length - 2, Math.floor(indice));
                var t = indice - i;
                var a = anim.puntos[i];
                var b = anim.puntos[i + 1];
                var punto = { lat: a.lat + (b.lat - a.lat) * t, lng: a.lng + (b.lng - a.lng) * t };

                e.marcadorVehiculo.setLatLng([punto.lat, punto.lng]);

                if (now - anim.ultimoEnvio > 110 || progreso >= 1) {
                    anim.ultimoEnvio = now;
                    anim.notificar(
                        Math.round(progreso * 1000) / 10,
                        punto,
                        Math.round(anim.distanciaKm * (1 - progreso) * 100) / 100,
                        Math.max(0, Math.ceil(anim.duracionRealMs / 60000 * (1 - progreso)))
                    );
                }

                if (progreso >= 1) {
                    anim.activa = false;
                    e.animacion = null;
                    if (e.dotNet) {
                        e.dotNet.invokeMethodAsync("OnAnimacionFinalizada");
                    }
                    return;
                }
                requestAnimationFrame(tick);
            }
            requestAnimationFrame(tick);
            return true;
        },

        pausar: function (id) {
            var e = estado(id);
            if (e.animacion) e.animacion.activa = false;
        },

        reanudar: function (id) {
            var e = estado(id);
            if (e.animacion) {
                e.animacion.ultimoTick = performance.now();
                e.animacion.activa = true;
            }
        },

        reiniciar: function (id) {
            var e = estado(id);
            if (e.animacion) {
                e.animacion.transcurrido = 0;
                e.animacion.activa = true;
                e.animacion.ultimoTick = performance.now();
            }
        },

        centrar: function (id, lat, lng, zoom) {
            var e = estado(id);
            if (puntoValido({ lat: lat, lng: lng })) {
                e.map.setView([lat, lng], zoom || ZOOM_INICIAL);
            }
        },

        limpiar: function (id) {
            var e = mapas[id];
            if (!e) return;
            detenerAnimacion(e);
            if (e.map) {
                e.map.remove();
            }
            delete mapas[id];
        }
    };

    window.logitrackMapas = api;
})();
