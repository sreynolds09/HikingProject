/* ==========================================================================
   site.js — HikingApp (aligned + polished)
   - Modular: HikingApp.utils, HikingApp.api, HikingApp.modals, HikingApp.map,
              HikingApp.entities (parks/routes/points/images/feedback)
   - Mapbox rendering (maps, markers, routes, points)
   - GPX parsing preview
   - Multi-image uploads with previews
   - All CRUD exposed & event-driven reloads
   ========================================================================== */

window.HikingApp = window.HikingApp || {};

(function (HikingApp, document) {
    'use strict';

    /* ===========================
       utils
    =========================== */
    HikingApp.utils = (function () {
        const $ = id => document.getElementById(id);
        const qs = (sel, ctx = document) => (ctx || document).querySelector(sel);
        const qsa = (sel, ctx = document) => Array.from((ctx || document).querySelectorAll(sel));

        const cloneTemplate = id => {
            const t = document.getElementById(id);
            return t ? t.content.cloneNode(true) : null;
        };

        // Toast
        function showToast(msg = '', type = 'info', ms = 3500) {
            let container = document.getElementById('hikingToasts');
            if (!container) {
                container = document.createElement('div');
                container.id = 'hikingToasts';
                container.className = 'position-fixed top-0 end-0 p-3';
                container.style.zIndex = 12000;
                document.body.appendChild(container);
            }
            const el = document.createElement('div');
            el.className = 'rounded shadow-sm px-3 py-2 mb-2';
            el.style.background = type === 'success' ? '#2e6b4f' : (type === 'warning' ? '#b58c00' : (type === 'danger' || type === 'error' ? '#b33a3a' : '#2e6b4f'));
            el.style.color = '#fff';
            el.textContent = msg;
            container.appendChild(el);
            setTimeout(() => {
                el.style.opacity = '0';
                el.style.transition = 'opacity 300ms';
                setTimeout(() => el.remove(), 350);
            }, ms);
        }

        // Loader overlay
        function showLoader() {
            let l = document.getElementById('globalLoader');
            if (!l) {
                l = document.createElement('div');
                l.id = 'globalLoader';
                l.className = 'position-fixed top-0 start-0 w-100 h-100 d-flex justify-content-center align-items-center';
                l.style.zIndex = 20000;
                l.style.background = 'rgba(0,0,0,0.35)';
                l.innerHTML = '<div class="spinner-border text-light" role="status" aria-hidden="true"></div>';
                document.body.appendChild(l);
            }
            l.style.display = 'flex';
        }
        function hideLoader() {
            const l = document.getElementById('globalLoader');
            if (l) l.style.display = 'none';
        }

        // Unified API request helper
        async function apiRequest(url, method = 'GET', body = null, isFormData = false, opts = {}) {
            const fetchOpts = Object.assign({ method, headers: {} }, opts);
            if (isFormData && body instanceof FormData) {
                fetchOpts.body = body;
            } else if (body != null) {
                fetchOpts.headers['Content-Type'] = 'application/json';
                fetchOpts.body = JSON.stringify(body);
            }
            const res = await fetch(url, fetchOpts);
            const ct = res.headers.get('content-type') || '';
            const isJson = ct.includes('application/json') || res.status === 204;
            if (!res.ok) {
                let errTxt = `HTTP ${res.status}`;
                try {
                    if (isJson) {
                        const errObj = await res.json();
                        errTxt = errObj?.message || JSON.stringify(errObj);
                    } else {
                        errTxt = await res.text();
                    }
                } catch { /* ignore */ }
                const err = new Error(errTxt);
                err.status = res.status;
                throw err;
            }
            if (isJson) {
                try { return res.status === 204 ? null : await res.json(); } catch { return null; }
            }
            return res.text();
        }

        // Simple debounce
        function debounce(fn, wait = 300) {
            let t = null;
            return (...a) => { clearTimeout(t); t = setTimeout(() => fn(...a), wait); };
        }

        // Populate select from GET /api URL
        async function populateDropdown(selector, apiUrl, valueKey = 'id', textKey = 'name', includeEmpty = true) {
            const el = document.querySelector(selector);
            if (!el) return;
            try {
                const items = await apiRequest(apiUrl, 'GET');
                el.innerHTML = includeEmpty ? '<option value="">— Select —</option>' : '';
                (items || []).forEach(i => {
                    const opt = document.createElement('option');
                    opt.value = i[valueKey] ?? i[valueKey.toLowerCase()];
                    opt.textContent = i[textKey] ?? i[textKey.toLowerCase()];
                    el.appendChild(opt);
                });
            } catch (err) {
                console.error('populateDropdown error', err);
            }
        }

        // Confirm delete modal helper
        function confirmDelete(entityName, data, onConfirm) {
            const modalEl = document.getElementById('deleteConfirmModal');
            if (!modalEl) {
                if (confirm(`Delete ${entityName}?`)) onConfirm && onConfirm();
                return;
            }
            const msgEl = modalEl.querySelector('#deleteConfirmText');
            if (msgEl) msgEl.textContent = `Delete ${entityName}?`;
            const btn = modalEl.querySelector('#confirmDeleteBtn');
            const bs = new bootstrap.Modal(modalEl);
            bs.show();
            const clickHandler = async () => {
                btn.removeEventListener('click', clickHandler);
                bs.hide();
                try { await onConfirm && onConfirm(); } catch (err) { console.error(err); }
            };
            btn.addEventListener('click', clickHandler);
        }

        // Generic form -> FormData helper
        function formToPayload(form) {
            const hasFiles = !!form.querySelector('input[type="file"]');
            if (hasFiles) return new FormData(form);
            const obj = {};
            new FormData(form).forEach((v, k) => {
                if (obj[k] !== undefined) {
                    if (!Array.isArray(obj[k])) obj[k] = [obj[k]];
                    obj[k].push(v);
                } else obj[k] = v;
            });
            return obj;
        }

        // Lightbox
        function openLightbox(url) {
            const w = window.open('', '_blank');
            if (!w) return;
            w.document.write(`<html><head><title>Image</title></head><body style="margin:0;background:#000;display:flex;align-items:center;justify-content:center;"><img src="${url}" style="max-width:100vw;max-height:100vh"/></body></html>`);
        }

        return { $, qs, qsa, cloneTemplate, showToast, showLoader, hideLoader, apiRequest, debounce, populateDropdown, confirmDelete, formToPayload, openLightbox };
    })();

    /* ===========================
       map (Mapbox helpers)
    =========================== */
    HikingApp.map = (function (utils) {
        const maps = {};     // keyed by element id
        const markers = {};  // keyed by map id -> array

        function initMap(elId, opts = {}) {
            const el = document.getElementById(elId);
            if (!el || !window.mapboxgl) return null;
            if (maps[elId]) return maps[elId];

            function resolveMapboxToken() {
                if (window.MAPBOX_TOKEN) return window.MAPBOX_TOKEN;
                const meta = document.querySelector('meta[name="mapbox-token"]');
                if (meta) return meta.content;
                console.warn('⚠️ Mapbox token not found. Please inject via window.MAPBOX_TOKEN or <meta name="mapbox-token">');
                return '';
            }

            mapboxgl.accessToken = resolveMapboxToken();
            const map = new mapboxgl.Map(Object.assign({
                container: elId,
                style: 'mapbox://styles/mapbox/outdoors-v12',
                center: [-85.3, 35.0],
                zoom: 7
            }, opts));
            maps[elId] = map;
            markers[elId] = [];
            return map;
        }

        function initAll() {
            ['mainMap', 'parksMap', 'routesMap', 'routeMiniMap', 'dashboardMap'].forEach(id => {
                if (document.getElementById(id)) initMap(id);
            });
        }

        function clearMarkers(mapId) {
            (markers[mapId] || []).forEach(m => m.remove());
            markers[mapId] = [];
        }

        function addMarker(mapId, lng, lat, popupHtml, opts = {}) {
            const map = maps[mapId];
            if (!map) return;
            const marker = new mapboxgl.Marker(opts)
                .setLngLat([parseFloat(lng), parseFloat(lat)])
                .setPopup(new mapboxgl.Popup({ offset: 10 }).setHTML(popupHtml || ''))
                .addTo(map);
            markers[mapId].push(marker);
            return marker;
        }

        function fitMapToMarkers(mapId, padding = 40) {
            const map = maps[mapId];
            if (!map) return;
            const arr = markers[mapId] || [];
            if (!arr.length) return;
            const bounds = new mapboxgl.LngLatBounds();
            arr.forEach(m => bounds.extend(m.getLngLat()));
            map.fitBounds(bounds, { padding, maxZoom: 13 });
        }

        function plotPoints(points = [], mapId = 'routeMiniMap') {
            const map = maps[mapId] || initMap(mapId);
            if (!map) return;
            clearMarkers(mapId);
            points.forEach(p => {
                if (p.latitude == null || p.longitude == null) return;
                addMarker(mapId, p.longitude, p.latitude, `<strong>${p.description || ''}</strong>`);
            });
            if (markers[mapId].length) fitMapToMarkers(mapId);
        }

        function renderRoute(mapId, coords = []) {
            const map = maps[mapId] || initMap(mapId);
            if (!map || !coords?.length) return;
            const srcId = `route-src-${mapId}`;
            const layerId = `route-line-${mapId}`;
            const lineCoords = coords
                .map(c => [Number(c.longitude ?? c.Longitude ?? c.lng ?? 0), Number(c.latitude ?? c.Latitude ?? c.lat ?? 0)])
                .filter(c => !isNaN(c[0]) && !isNaN(c[1]));

            map.once('load', () => {
                if (!map.getSource(srcId)) {
                    map.addSource(srcId, { type: 'geojson', data: { type: 'Feature', geometry: { type: 'LineString', coordinates: lineCoords } } });
                    map.addLayer({
                        id: layerId,
                        type: 'line',
                        source: srcId,
                        layout: { 'line-join': 'round', 'line-cap': 'round' },
                        paint: { 'line-color': '#ff5a5f', 'line-width': 4 }
                    });
                } else {
                    map.getSource(srcId).setData({ type: 'Feature', geometry: { type: 'LineString', coordinates: lineCoords } });
                }

                clearMarkers(mapId);
                coords.forEach((c, i) => addMarker(mapId, c.longitude ?? c.Longitude ?? 0, c.latitude ?? c.Latitude ?? 0, `#${i + 1} ${c.description || ''}`));

                const bounds = lineCoords.reduce((b, c) => b.extend(c), new mapboxgl.LngLatBounds(lineCoords[0], lineCoords[0]));
                map.fitBounds(bounds, { padding: 30, maxZoom: 14 });
            });
        }

        return { initMap, initAll, addMarker, clearMarkers, fitMapToMarkers, plotPoints, renderRoute, maps };
    })(HikingApp.utils);

    /* ===========================
       api (CRUD wrappers)
    =========================== */
    HikingApp.api = (function (utils) {
        const endpoints = {
            parks: '/api/parks',
            routes: '/api/routes',
            points: '/api/routepoints',
            images: '/api/routeimages',
            feedback: '/api/routefeedback',
            gpxpoints: '/api/gpxpoints' // fallback
        };

        // PARKS
        async function listParks() { return await utils.apiRequest(endpoints.parks, 'GET'); }
        async function getPark(id) { return await utils.apiRequest(`${endpoints.parks}/${id}`, 'GET'); }
        async function createPark(payload) { return await utils.apiRequest(endpoints.parks, 'POST', payload); }
        async function updatePark(id, payload) { return await utils.apiRequest(`${endpoints.parks}/${id}`, 'PUT', payload); }
        async function deletePark(id) { return await utils.apiRequest(`${endpoints.parks}/${id}`, 'DELETE'); }

        // ROUTES
        async function listRoutes() { return await utils.apiRequest(endpoints.routes, 'GET'); }
        async function getRoute(id) { return await utils.apiRequest(`${endpoints.routes}/${id}`, 'GET'); }
        async function createRoute(payload) { return await utils.apiRequest(endpoints.routes, 'POST', payload); }
        async function updateRoute(id, payload) { return await utils.apiRequest(`${endpoints.routes}/${id}`, 'PUT', payload); }
        async function deleteRoute(id) { return await utils.apiRequest(`${endpoints.routes}/${id}`, 'DELETE'); }

        // ROUTE POINTS
        async function listPoints(routeId) {
            const url = routeId ? `${endpoints.points}?routeId=${encodeURIComponent(routeId)}` : endpoints.points;
            return await utils.apiRequest(url, 'GET');
        }
        async function getPoint(id) { return await utils.apiRequest(`${endpoints.points}/${id}`, 'GET'); }
        async function createPoint(payloadOrForm) {
            if (payloadOrForm instanceof FormData) return await utils.apiRequest(endpoints.points, 'POST', payloadOrForm, true);
            return await utils.apiRequest(endpoints.points, 'POST', payloadOrForm);
        }
        async function updatePoint(id, payload) { return await utils.apiRequest(`${endpoints.points}/${id}`, 'PUT', payload); }
        async function deletePoint(id) { return await utils.apiRequest(`${endpoints.points}/${id}`, 'DELETE'); }

        // IMAGES
        async function listImages() { return await utils.apiRequest(endpoints.images, 'GET'); }
        async function uploadImages(routeId, formData) {
            let url = endpoints.images;
            if (routeId) url += `?routeId=${encodeURIComponent(routeId)}`;
            return await utils.apiRequest(url, 'POST', formData, true);
        }
        async function deleteImage(id) { return await utils.apiRequest(`${endpoints.images}/${id}`, 'DELETE'); }

        // FEEDBACK
        async function listFeedback(routeId) {
            const url = routeId ? `${endpoints.feedback}?routeId=${encodeURIComponent(routeId)}` : endpoints.feedback;
            return await utils.apiRequest(url, 'GET');
        }
        async function createFeedback(payload) { return await utils.apiRequest(endpoints.feedback, 'POST', payload); }
        async function updateFeedback(id, payload) { return await utils.apiRequest(`${endpoints.feedback}/${id}`, 'PUT', payload); }
        async function deleteFeedback(id) { return await utils.apiRequest(`${endpoints.feedback}/${id}`, 'DELETE'); }

        // GPX upload
        async function uploadGpx(routeId, formData) {
            const url = `${endpoints.routes}/${encodeURIComponent(routeId)}/upload-gpx`;
            try {
                return await utils.apiRequest(url, 'POST', formData, true);
            } catch {
                return await utils.apiRequest(endpoints.gpxpoints, 'POST', formData, true);
            }
        }

        return {
            listParks, getPark, createPark, updatePark, deletePark,
            listRoutes, getRoute, createRoute, updateRoute, deleteRoute,
            listPoints, getPoint, createPoint, updatePoint, deletePoint,
            listImages, uploadImages, deleteImage,
            listFeedback, createFeedback, updateFeedback, deleteFeedback,
            uploadGpx
        };
    })(HikingApp.utils);

    /* ===========================
       modals (view/delete/upload/feedback)
    =========================== */
    HikingApp.modals = (function (utils, api, map) {
        async function showViewModal(entityKey, data) {
            const modal = document.getElementById('viewModal');
            if (!modal) return;
            const content = modal.querySelector('#viewModalContent');
            content.innerHTML = `<div class="text-center py-4"><div class="spinner-border text-primary"></div><div class="mt-2">Loading...</div></div>`;
            const bs = new bootstrap.Modal(modal);
            bs.show();

            try {
                const id = data && (data.id || data.RouteID || data.ParkID || data.Id || data.ID);
                let payload;
                if (entityKey === 'parks') payload = id ? await api.getPark(id) : data;
                else if (entityKey === 'routes') payload = id ? await api.getRoute(id) : data;
                else if (entityKey === 'routeimages') payload = id ? (await api.listImages()).find(i => String(i.id) === String(id)) : data;
                else if (entityKey === 'routefeedback') payload = id ? (await api.listFeedback()).find(f => String(f.Id ?? f.id) === String(id)) : data;
                else if (entityKey === 'routepoints' || entityKey === 'gpxpoints') payload = id ? await api.getPoint(id) : data;
                else payload = data;

                let html = '';
                switch (entityKey) {
                    case 'parks': html = buildParkView(payload); break;
                    case 'routes': html = buildRouteView(payload); break;
                    case 'routeimages': html = buildImageView(payload); break;
                    case 'routefeedback': html = buildFeedbackView(payload); break;
                    case 'routepoints':
                    case 'gpxpoints': html = buildPointsView(payload); break;
                    default: html = `<pre>${escapeHtml(JSON.stringify(payload, null, 2))}</pre>`;
                }
                content.innerHTML = html;

                if (entityKey === 'routes' && payload?.Coordinates?.length && document.getElementById('routeViewMap')) {
                    map.initMap('routeViewMap');
                    map.renderRoute('routeViewMap', payload.Coordinates);
                } else if ((entityKey === 'routepoints' || entityKey === 'gpxpoints') && Array.isArray(payload) && document.getElementById('viewPointsMap')) {
                    map.initMap('viewPointsMap');
                    map.plotPoints(payload, 'viewPointsMap');
                }
            } catch (err) {
                console.error('showViewModal error', err);
                content.innerHTML = `<div class="alert alert-danger">Failed to load details.</div>`;
            }
        }

        function buildParkView(p) {
            if (!p) return '<div class="text-muted">No data</div>';
            const routesHtml = (p.Routes || []).map(r => `<li class="list-group-item small">${escapeHtml(r.RouteName || r.RouteName)}</li>`).join('') || '<li class="list-group-item small text-muted">No routes</li>';
            return `
      <div class="row">
        <div class="col-md-8">
          <h4>${escapeHtml(p.ParkName)}</h4>
          <p class="mb-1"><strong>Location:</strong> ${escapeHtml(p.Location || '')}</p>
          <p>${escapeHtml(p.Description || '')}</p>
        </div>
        <div class="col-md-4">${p.ImageURL ? `<img src="${p.ImageURL}" class="img-fluid rounded" alt="park image"/>` : ''}</div>
        <div class="col-12 mt-3">
          <h6>Routes</h6>
          <ul class="list-group">${routesHtml}</ul>
        </div>
      </div>`;
        }

        function buildRouteView(r) {
            if (!r) return '<div class="text-muted">No data</div>';
            const imagesHtml = (r.RecentImages || []).map(i => `<div class="col-md-4 mb-2"><img src="${i.imageURL || i.ImageURL}" class="img-fluid rounded" role="button" data-img="${i.imageURL || i.ImageURL}" /></div>`).join('') || '<div class="text-muted small">No images</div>';
            const fbHtml = (r.RecentFeedback || []).map(f => `<li class="list-group-item small"><strong>${escapeHtml(f.UserName || f.user || '')}</strong> — ${escapeHtml(f.Comments || f.comment || '')} <span class="text-warning">★${f.Rating || f.rating || 0}</span></li>`).join('') || '<li class="list-group-item small text-muted">No feedback</li>';
            return `
      <div class="row">
        <div class="col-md-8">
          <h4>${escapeHtml(r.RouteName)}</h4>
          <p><strong>Park:</strong> ${escapeHtml(r.ParkName || r.Park?.ParkName || '')}</p>
          <p><strong>Difficulty:</strong> ${escapeHtml(r.Difficulty || '')} — <strong>Distance:</strong> ${r.Distance || ''} mi</p>
          <p>${escapeHtml(r.Description || '')}</p>
          <div id="routeViewMap" style="height:260px;" class="rounded border"></div>
        </div>
        <div class="col-md-4">
          <h6>Images</h6>
          <div class="row">${imagesHtml}</div>
          <hr/>
          <h6>Recent Feedback</h6>
          <ul class="list-group small">${fbHtml}</ul>
        </div>
      </div>`;
        }

        function buildImageView(i) {
            if (!i) return '<div class="text-muted">No data</div>';
            return `<div class="text-center">
        <img src="${i.imageURL || i.ImageURL}" class="img-fluid rounded" alt="${escapeHtml(i.caption || '')}" />
        <p class="mt-2"><strong>Route:</strong> ${escapeHtml(i.routeName || '')}</p>
        <p class="small text-muted">File: ${escapeHtml(i.fileName || '')}</p>
      </div>`;
        }

        function buildFeedbackView(f) {
            if (!f) return '<div class="text-muted">No data</div>';
            return `<div>
        <h5>${escapeHtml(f.UserName || f.User || '')}</h5>
        <p><strong>Route:</strong> ${escapeHtml(f.RouteName || '')}</p>
        <p><strong>Rating:</strong> ${escapeHtml(String(f.Rating || f.rating || 0))}</p>
        <p>${escapeHtml(f.Comments || f.comment || '')}</p>
      </div>`;
        }

        function buildPointsView(points) {
            if (!points) return '<div class="text-muted">No data</div>';
            const rows = (points || []).map(p => `<li class="list-group-item small">${Number(p.latitude ?? p.Latitude).toFixed(6)}, ${Number(p.longitude ?? p.Longitude).toFixed(6)} ${escapeHtml(p.description || p.Description || '')}</li>`).join('');
            return `<div><h5>Points (${(points || []).length})</h5><div id="viewPointsMap" style="height:260px;" class="rounded border mb-2"></div><ul class="list-group">${rows}</ul></div>`;
        }

        function showUploadModal(entityKey, data) {
            if (entityKey === 'routeimages') {
                const modal = document.getElementById('uploadRouteImageModal');
                modal.querySelector('[name="RouteId"]').value = data.id || data.RouteID || '';
                new bootstrap.Modal(modal).show();
            } else if (entityKey === 'gpxpoints' || entityKey === 'routepoints') {
                const modal = document.getElementById('uploadGpxModal');
                modal.querySelector('[name="RouteId"]').value = data.id || data.RouteID || '';
                new bootstrap.Modal(modal).show();
            } else {
                utils.showToast('Upload not supported for ' + entityKey, 'warning');
            }
        }

        function showFeedbackModal(_entityKey, data) {
            const modal = document.getElementById('feedbackModal');
            if (!modal) return;
            modal.querySelector('[name="RouteId"]').value = data.id || data.RouteID || '';
            new bootstrap.Modal(modal).show();
        }

        function escapeHtml(s) { if (s == null) return ''; return String(s).replace(/[&<>"'`]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;', '`': '&#x60;' })[c]); }

        return { showViewModal, showUploadModal, showFeedbackModal };
    })(HikingApp.utils, HikingApp.api, HikingApp.map);

    /* ===========================
       entities (client-side controllers)
    =========================== */
    HikingApp.entities = (function (utils, api, modals, map) {

        function attachActionsToTd(td, entityKey, data) {
            const tpl = utils.cloneTemplate('actionButtonsTemplate');
            if (!tpl) {
                const view = document.createElement('button');
                view.className = 'btn btn-sm btn-outline-primary me-1';
                view.textContent = 'View';
                view.addEventListener('click', () => modals.showViewModal(entityKey, data));
                td.appendChild(view);
                return;
            }
            td.appendChild(tpl);
            td.querySelectorAll('[data-action]').forEach(b => {
                const action = b.getAttribute('data-action');
                b.dataset.entity = entityKey;
                b.addEventListener('click', e => {
                    e.stopPropagation();
                    switch (action) {
                        case 'view': return modals.showViewModal(entityKey, data);
                        case 'edit': return openEditModal(entityKey, data);
                        case 'delete': return utils.confirmDelete(entityKey, data, async () => {
                            try {
                                if (entityKey === 'parks') { await api.deletePark(data.id || data.ParkID); document.dispatchEvent(new CustomEvent('park:deleted')); utils.showToast('Park deleted', 'success'); }
                                else if (entityKey === 'routes') { await api.deleteRoute(data.id || data.RouteID); document.dispatchEvent(new CustomEvent('route:deleted')); utils.showToast('Route deleted', 'success'); }
                                else if (entityKey === 'routeimages') { await api.deleteImage(data.id || data.Id); document.dispatchEvent(new CustomEvent('routeimage:deleted')); utils.showToast('Image deleted', 'success'); }
                                else if (entityKey === 'routefeedback') { await api.deleteFeedback(data.id || data.Id); document.dispatchEvent(new CustomEvent('feedback:deleted')); utils.showToast('Feedback deleted', 'success'); }
                                else if (entityKey === 'routepoints' || entityKey === 'gpxpoints') { await api.deletePoint(data.id || data.Id); document.dispatchEvent(new CustomEvent('gpx:deleted')); utils.showToast('Point deleted', 'success'); }
                            } catch (err) { console.error(err); utils.showToast('Delete failed', 'danger'); }
                        });
                        case 'upload': return modals.showUploadModal(entityKey, data);
                        case 'feedback': return modals.showFeedbackModal(entityKey, data);
                        default: return;
                    }
                });
            });
        }

        function openEditModal(entityKey, data) {
            if (entityKey === 'parks') {
                const modal = document.getElementById('createParkModal');
                const form = modal.querySelector('form[data-entity="park"]');
                form.querySelector('[name="Name"]').value = data.ParkName || data.name || '';
                form.querySelector('[name="Location"]').value = data.Location || data.location || '';
                form.querySelector('[name="Description"]').value = data.Description || data.description || '';
                form.dataset.action = 'edit';
                form.dataset.id = data.ParkID || data.id;
                new bootstrap.Modal(modal).show();
                return;
            }
            if (entityKey === 'routes') {
                const modal = document.getElementById('createRouteModal');
                const form = modal.querySelector('form[data-entity="route"]');
                form.querySelector('[name="Name"]').value = data.RouteName || data.name || '';
                form.querySelector('[name="ParkId"]').value = data.ParkID || data.parkId || '';
                form.querySelector('[name="Difficulty"]').value = data.Difficulty || data.difficulty || '';
                form.querySelector('[name="Distance"]').value = data.Distance || data.distance || '';
                form.dataset.action = 'edit';
                form.dataset.id = data.RouteID || data.id;
                new bootstrap.Modal(modal).show();
                return;
            }
            if (entityKey === 'routefeedback') {
                const modal = document.getElementById('feedbackModal');
                const form = modal.querySelector('form[data-entity="feedback"]');
                form.dataset.action = 'edit';
                form.dataset.id = data.Id || data.id;
                form.querySelector('[name="RouteId"]').value = data.RouteID || data.routeId || '';
                form.querySelector('[name="User"]').value = data.UserName || data.User || '';
                form.querySelector('[name="Comment"]').value = data.Comments || data.Comment || '';
                form.querySelector('[name="Rating"]').value = data.Rating || data.rating || 5;
                new bootstrap.Modal(modal).show();
                return;
            }
            if (entityKey === 'routepoints') {
                const modal = document.getElementById('routePointModal');
                const form = modal.querySelector('form[data-entity="routepoints"]') || modal.querySelector('form');
                form.dataset.action = 'edit';
                form.dataset.id = data.Id || data.id;
                form.querySelector('[name="RouteId"]').value = data.RouteID || data.routeId || '';
                form.querySelector('[name="Latitude"]').value = Number(data.latitude ?? data.Latitude ?? 0).toFixed(6);
                form.querySelector('[name="Longitude"]').value = Number(data.longitude ?? data.Longitude ?? 0).toFixed(6);
                form.querySelector('[name="Elevation"]').value = data.elevation || data.Elevation || '';
                new bootstrap.Modal(modal).show();
                return;
            }
        }

        function escapeHtml(s) { if (s == null) return ''; return String(s).replace(/[&<>"'`]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;', '`': '&#x60;' })[c]); }

        async function loadParksIntoTable() {
            try {
                utils.showLoader();
                const parks = await api.listParks();
                const tbody = document.querySelector('#parksTableBody, #parksTable tbody');
                if (!tbody) return;
                tbody.innerHTML = '';
                (parks || []).forEach(p => {
                    const tr = document.createElement('tr');
                    tr.dataset.id = p.ParkID || p.id || '';
                    tr.innerHTML = `<td class="park-name">${escapeHtml(p.ParkName || p.name || '')}</td>
                          <td class="park-location">${escapeHtml(p.Location || p.location || '')}</td>
                          <td>${(p.Routes || p.routeCount || 0)}</td>
                          <td></td>`;
                    attachActionsToTd(tr.querySelector('td:last-child'), 'parks', {
                        id: p.ParkID || p.id,
                        ParkID: p.ParkID || p.id,
                        ParkName: p.ParkName || p.name,
                        Location: p.Location || p.location,
                        Description: p.Description || p.description,
                        ImageURL: p.ImageURL || p.imageURL || p.Image || ''
                    });
                    tbody.appendChild(tr);
                });
            } catch (err) { console.error(err); utils.showToast('Failed loading parks', 'danger'); }
            finally { utils.hideLoader(); }
        }

        async function loadRoutesIntoTable() {
            try {
                utils.showLoader();
                const routes = await api.listRoutes();
                const tbody = document.querySelector('#routesTableBody, #routesTable tbody');
                if (!tbody) return;
                tbody.innerHTML = '';
                (routes || []).forEach(r => {
                    const tr = document.createElement('tr');
                    tr.dataset.id = r.RouteID || r.id || '';
                    tr.innerHTML = `<td class="route-name">${escapeHtml(r.RouteName || r.name || '')}</td>
                          <td class="route-park">${escapeHtml(r.ParkName || r.parkName || '')}</td>
                          <td>${escapeHtml(r.Difficulty || r.difficulty || '')}</td>
                          <td>${r.Distance != null ? Number(r.Distance).toFixed(1) : ''}</td>
                          <td></td>`;
                    attachActionsToTd(tr.querySelector('td:last-child'), 'routes', {
                        id: r.RouteID || r.id,
                        RouteID: r.RouteID || r.id,
                        RouteName: r.RouteName || r.name,
                        ParkID: r.ParkID || r.parkId || r.parkID,
                        ParkName: r.ParkName || r.parkName,
                        Difficulty: r.Difficulty || r.difficulty,
                        Distance: r.Distance
                    });
                    tbody.appendChild(tr);
                });

                // Dashboard small map plotting
                if (document.getElementById('dashboardMap')) {
                    HikingApp.map.initMap('dashboardMap');
                    HikingApp.map.clearMarkers('dashboardMap');
                    (routes || []).forEach(r => {
                        const lat = r.Latitude || r.latitude || (r.Coordinates?.[0]?.latitude);
                        const lon = r.Longitude || r.longitude || (r.Coordinates?.[0]?.longitude);
                        if (lat && lon) HikingApp.map.addMarker('dashboardMap', lon, lat, `<strong>${escapeHtml(r.RouteName || r.name)}</strong>`);
                    });
                    HikingApp.map.fitMapToMarkers('dashboardMap');
                }

                // Mini map plotting if present
                if (document.getElementById('routeMiniMap')) {
                    HikingApp.map.initMap('routeMiniMap');
                    HikingApp.map.clearMarkers('routeMiniMap');
                    (routes || []).forEach(r => {
                        const lat = r.Latitude || r.latitude || (r.Coordinates?.[0]?.latitude);
                        const lon = r.Longitude || r.longitude || (r.Coordinates?.[0]?.longitude);
                        if (lat && lon) HikingApp.map.addMarker('routeMiniMap', lon, lat, `<strong>${escapeHtml(r.RouteName || r.name)}</strong>`);
                    });
                    HikingApp.map.fitMapToMarkers('routeMiniMap');
                }

            } catch (err) { console.error(err); utils.showToast('Failed loading routes', 'danger'); }
            finally { utils.hideLoader(); }
        }

        async function loadImagesIntoGrid() {
            try {
                const images = await api.listImages();
                const grid = document.getElementById('routeImagesGrid');
                if (!grid) return;
                grid.innerHTML = '';
                (images || []).forEach(img => {
                    const col = document.createElement('div');
                    col.className = 'col-md-3 mb-3';
                    col.innerHTML = `<div class="card h-100 shadow-sm">
            <img src="${escapeHtml(img.imageURL || img.ImageURL || img.url || img.imageUrl)}" class="card-img-top" alt="${escapeHtml(img.caption || '')}" role="button" data-image-url="${escapeHtml(img.imageURL || img.ImageURL || img.url)}"/>
            <div class="card-body d-flex justify-content-between align-items-center">
              <div class="small">${escapeHtml(img.routeName || img.RouteName || '')}</div>
              <div class="btn-group btn-group-sm" role="group">
                <button class="btn btn-outline-primary btn-view-image" data-id="${img.id || img.Id}"><i class="bi bi-eye"></i></button>
                <button class="btn btn-outline-danger btn-delete-image" data-id="${img.id || img.Id}"><i class="bi bi-trash"></i></button>
              </div>
            </div>
          </div>`;
                    grid.appendChild(col);
                });

                grid.querySelectorAll('.btn-view-image').forEach(b => b.addEventListener('click', e => {
                    const id = e.currentTarget.dataset.id;
                    const item = (images || []).find(x => String(x.id || x.Id) === String(id));
                    if (item) HikingApp.modals.showViewModal('routeimages', item);
                }));
                grid.querySelectorAll('.btn-delete-image').forEach(b => b.addEventListener('click', e => {
                    const id = e.currentTarget.dataset.id;
                    const item = (images || []).find(x => String(x.id || x.Id) === String(id));
                    if (item) utils.confirmDelete('routeimages', item, async () => {
                        await api.deleteImage(item.id || item.Id);
                        document.dispatchEvent(new CustomEvent('routeimage:deleted'));
                        utils.showToast('Image deleted', 'success');
                    });
                }));

            } catch (err) { console.error(err); utils.showToast('Failed loading images', 'danger'); }
        }

        async function loadGpxIntoTable() {
            try {
                const rows = await api.listPoints();
                const tbody = document.querySelector('#gpxTable tbody, #pointsTableBody');
                if (!tbody) return;
                tbody.innerHTML = '';
                (rows || []).forEach(r => {
                    const tr = document.createElement('tr');
                    const lat = r.Latitude ?? r.latitude;
                    const lon = r.Longitude ?? r.longitude;
                    tr.innerHTML = `<td>${escapeHtml(r.fileName || r.FileName || '')}</td>
                          <td>${escapeHtml(r.routeName || r.RouteName || '')}</td>
                          <td>${r.pointCount || r.point_count || ''}</td>
                          <td>${typeof lat !== 'undefined' && typeof lon !== 'undefined' ? `${Number(lat).toFixed(5)}, ${Number(lon).toFixed(5)}` : ''}</td>
                          <td></td>`;
                    attachActionsToTd(tr.querySelector('td:last-child'), 'gpxpoints', r);
                    tbody.appendChild(tr);
                });
            } catch (err) { console.error(err); utils.showToast('Failed loading GPX', 'danger'); }
        }

        async function loadFeedbackIntoTable() {
            try {
                const fb = await api.listFeedback();
                const tbody = document.querySelector('#feedbackTableBody, #feedbackTable tbody');
                if (!tbody) return;
                tbody.innerHTML = '';
                (fb || []).forEach(f => {
                    const tr = document.createElement('tr');
                    tr.innerHTML = `<td>${escapeHtml(f.RouteName || f.routeName || '')}</td>
                          <td>${escapeHtml(f.UserName || f.User || '')}</td>
                          <td>${escapeHtml(f.Comments || f.comment || '')}</td>
                          <td>${f.Rating || f.rating || ''}</td>
                          <td></td>`;
                    attachActionsToTd(tr.querySelector('td:last-child'), 'routefeedback', f);
                    tbody.appendChild(tr);
                });
            } catch (err) { console.error(err); utils.showToast('Failed loading feedback', 'danger'); }
        }

        // Export public refresh functions
        return {
            loadParksIntoTable, loadRoutesIntoTable, loadImagesIntoGrid, loadGpxIntoTable, loadFeedbackIntoTable
        };
    })(HikingApp.utils, HikingApp.api, HikingApp.modals, HikingApp.map);

    /* ===========================
       form wiring: submissions
    =========================== */
    (function (utils, api) {
        async function handleFormSubmit(eventOrForm) {
            let form;
            if (eventOrForm instanceof Event) {
                eventOrForm.preventDefault();
                form = eventOrForm.currentTarget || eventOrForm.target;
            } else if (eventOrForm instanceof HTMLFormElement) {
                form = eventOrForm;
            } else {
                form = document.getElementById(eventOrForm);
                if (!form) return;
            }

            const entity = form.dataset.entity || form.getAttribute('data-entity');
            const action = form.dataset.action || form.getAttribute('data-action') || 'create';
            const id = form.dataset.id || form.querySelector('[name="id"]')?.value || form.querySelector('[name="ParkID"]')?.value || form.querySelector('[name="RouteID"]')?.value || null;

            try {
                utils.showLoader();
                const payload = utils.formToPayload(form);

                if (entity === 'park' || entity === 'parks') {
                    if (action === 'edit' && id) { await api.updatePark(id, payload); document.dispatchEvent(new CustomEvent('park:updated')); utils.showToast('Park updated', 'success'); }
                    else { await api.createPark(payload); document.dispatchEvent(new CustomEvent('park:created')); utils.showToast('Park created', 'success'); }
                } else if (entity === 'route' || entity === 'routes') {
                    if (action === 'edit' && id) { await api.updateRoute(id, payload); document.dispatchEvent(new CustomEvent('route:updated')); utils.showToast('Route updated', 'success'); }
                    else { await api.createRoute(payload); document.dispatchEvent(new CustomEvent('route:created')); utils.showToast('Route created', 'success'); }
                } else if (entity === 'routepoints') {
                    if (action === 'edit' && id) { await api.updatePoint(id, payload); document.dispatchEvent(new CustomEvent('gpx:updated')); utils.showToast('Point updated', 'success'); }
                    else { await api.createPoint(payload); document.dispatchEvent(new CustomEvent('gpx:uploaded')); utils.showToast('Point added', 'success'); }
                } else if (entity === 'routeimage' || entity === 'routeimages') {
                    let fd;
                    if (payload instanceof FormData) fd = payload;
                    else { fd = new FormData(); Object.keys(payload).forEach(k => fd.append(k, payload[k])); }
                    const rid = fd.get('RouteId') || fd.get('RouteID') || fd.get('routeId') || form.querySelector('[name="RouteId"]')?.value;
                    await api.uploadImages(rid, fd);
                    document.dispatchEvent(new CustomEvent('routeimage:uploaded'));
                    utils.showToast('Images uploaded', 'success');
                } else if (entity === 'gpx' || entity === 'gpxUpload') {
                    let fd;
                    if (payload instanceof FormData) fd = payload;
                    else { fd = new FormData(); Object.keys(payload).forEach(k => fd.append(k, payload[k])); }
                    const rid = fd.get('RouteId') || form.querySelector('[name="RouteId"]')?.value;
                    await api.uploadGpx(rid, fd);
                    document.dispatchEvent(new CustomEvent('gpx:uploaded'));
                    utils.showToast('GPX uploaded & parsed', 'success');
                } else if (entity === 'feedback' || entity === 'routefeedback') {
                    if (action === 'edit' && id) { await api.updateFeedback(id, payload); document.dispatchEvent(new CustomEvent('feedback:updated')); utils.showToast('Feedback updated', 'success'); }
                    else { await api.createFeedback(payload); document.dispatchEvent(new CustomEvent('feedback:created')); utils.showToast('Feedback submitted', 'success'); }
                } else {
                    const e = entity.endsWith('s') ? entity : entity + 's';
                    await utils.apiRequest(`/api/${e}`, action === 'edit' && id ? 'PUT' : 'POST', payload, payload instanceof FormData);
                    document.dispatchEvent(new CustomEvent(`${entity}:updated`));
                    utils.showToast(`${entity} saved`, 'success');
                }

                const bsModal = form.closest('.modal');
                if (bsModal) bootstrap.Modal.getInstance(bsModal)?.hide();

            } catch (err) {
                console.error('form submit error', err);
                utils.showToast(err?.message || 'Submission failed', 'danger');
                throw err;
            } finally {
                utils.hideLoader();
            }
            return false;
        }

        // expose so DOMContentLoaded can call it
        HikingApp.utils.handleFormSubmit = handleFormSubmit;

        function wireForms() {
            document.querySelectorAll('form[data-entity]').forEach(f => {
                if (f.__hiking_wired) return;
                f.addEventListener('submit', handleFormSubmit);
                f.__hiking_wired = true;
            });
        }

        // file preview: images + gpx
        function wireFilePreviews() {
            document.addEventListener('change', e => {
                const target = e.target;
                if (!target) return;

                // image preview
                if (target.matches('#imageUploadForm input[type="file"], #uploadRouteImageForm input[type="file"], input[name="ImageFile"]')) {
                    const files = target.files || [];
                    const preview = document.getElementById('imagePreview') || document.getElementById('routeImagePreview');
                    if (!preview) return;
                    preview.innerHTML = '';
                    Array.from(files).forEach(f => {
                        if (!f.type.startsWith('image/')) return;
                        const r = new FileReader();
                        r.onload = ev => {
                            const img = document.createElement('img');
                            img.src = ev.target.result;
                            img.style.maxWidth = '120px';
                            img.style.maxHeight = '90px';
                            img.className = 'rounded me-2 mb-2 shadow-sm';
                            img.addEventListener('click', () => utils.openLightbox(img.src));
                            preview.appendChild(img);
                        };
                        r.readAsDataURL(f);
                    });
                }

                // GPX preview: simple stats
                if (target.matches('#gpxUploadForm input[type="file"], input[name="File"], input[name="file"]')) {
                    const file = target.files?.[0];
                    const previewEl = document.getElementById('gpxPreview');
                    if (!previewEl) return;
                    previewEl.innerHTML = '';
                    if (!file) return;
                    const fr = new FileReader();
                    fr.onload = ev => {
                        try {
                            const parser = new DOMParser();
                            const xml = parser.parseFromString(ev.target.result, 'application/xml');
                            const trkpts = xml.querySelectorAll('trkpt');
                            const pts = Array.from(trkpts).map(pt => ({ lat: parseFloat(pt.getAttribute('lat')), lon: parseFloat(pt.getAttribute('lon')) })).filter(p => !isNaN(p.lat) && !isNaN(p.lon));
                            let dist = 0;
                            for (let i = 1; i < pts.length; i++) dist += haversine(pts[i - 1], pts[i]);
                            previewEl.innerHTML = `<div>File: <strong>${file.name}</strong></div><div>Points: <strong>${pts.length}</strong></div><div>Distance (approx): <strong>${dist.toFixed(2)} mi</strong></div>`;
                        } catch {
                            previewEl.innerHTML = `<div class="text-muted">Unable to parse preview</div>`;
                        }
                    };
                    fr.readAsText(file);
                }
            });
        }

        function haversine(a, b) {
            const toRad = x => x * Math.PI / 180;
            const R = 3958.8; // miles
            const dLat = toRad(b.lat - a.lat);
            const dLon = toRad(b.lon - a.lon);
            const lat1 = toRad(a.lat);
            const lat2 = toRad(b.lat);
            const aa = Math.sin(dLat / 2) ** 2 + Math.sin(dLon / 2) ** 2 * Math.cos(lat1) * Math.cos(lat2);
            const c = 2 * Math.atan2(Math.sqrt(aa), Math.sqrt(1 - aa));
            return R * c;
        }

        function wireCrudEvents() {
            document.addEventListener('park:created', () => HikingApp.entities.loadParksIntoTable());
            document.addEventListener('park:updated', () => HikingApp.entities.loadParksIntoTable());
            document.addEventListener('park:deleted', () => HikingApp.entities.loadParksIntoTable());

            document.addEventListener('route:created', () => HikingApp.entities.loadRoutesIntoTable());
            document.addEventListener('route:updated', () => HikingApp.entities.loadRoutesIntoTable());
            document.addEventListener('route:deleted', () => HikingApp.entities.loadRoutesIntoTable());

            document.addEventListener('routeimage:uploaded', () => HikingApp.entities.loadImagesIntoGrid());
            document.addEventListener('routeimage:deleted', () => HikingApp.entities.loadImagesIntoGrid());

            document.addEventListener('gpx:uploaded', () => { HikingApp.entities.loadGpxIntoTable(); HikingApp.entities.loadRoutesIntoTable(); });
            document.addEventListener('gpx:deleted', () => HikingApp.entities.loadGpxIntoTable());

            document.addEventListener('feedback:created', () => HikingApp.entities.loadFeedbackIntoTable());
            document.addEventListener('feedback:deleted', () => HikingApp.entities.loadFeedbackIntoTable());
        }

        function init() {
            wireForms();
            wireFilePreviews();
            wireCrudEvents();
        }

        // initialize immediately
        init();
    })(HikingApp.utils, HikingApp.api);

    /* ===========================
       Global UI: dark mode + table filters
    =========================== */
    (function createDarkModeToggle() {
        if (document.getElementById('darkModeToggle')) return;
        const btn = document.createElement('button');
        btn.id = 'darkModeToggle';
        btn.className = 'btn btn-sm btn-outline-secondary position-fixed';
        btn.style.top = '12px';
        btn.style.right = '12px';
        btn.style.zIndex = 13000;
        btn.textContent = '🌙';
        document.body.appendChild(btn);
        const apply = (on) => {
            document.documentElement.classList.toggle('dark-mode', on);
            localStorage.setItem('hiking_dark', on ? '1' : '0');
        };
        apply(localStorage.getItem('hiking_dark') === '1');
        btn.addEventListener('click', () => {
            const now = document.documentElement.classList.toggle('dark-mode');
            localStorage.setItem('hiking_dark', now ? '1' : '0');
        });
    })();

    (function setupTableFilters() {
        const { debounce } = HikingApp.utils;
        function filterTable(tableSelector, inputSelector, rowSelector = 'tbody tr', columns = []) {
            const input = document.querySelector(inputSelector);
            const table = document.querySelector(tableSelector);
            if (!input || !table) return;
            const rows = Array.from(table.querySelectorAll(rowSelector));
            input.addEventListener('input', debounce(() => {
                const q = input.value.trim().toLowerCase();
                rows.forEach(r => {
                    const text = columns.length
                        ? columns.map(c => (r.querySelector(c)?.textContent || '').toLowerCase()).join(' ')
                        : r.textContent.toLowerCase();
                    r.style.display = text.includes(q) ? '' : 'none';
                });
            }, 200));
        }
        filterTable('#parksTable', '#parksSearch', 'tbody tr', ['.park-name', '.park-location']);
        filterTable('#routesTable', '#routesSearch', 'tbody tr', ['.route-name', '.route-park']);
    })();

    /* ===========================
       DOMContentLoaded initialization
    =========================== */
    document.addEventListener('DOMContentLoaded', async function () {
        try {
            HikingApp.map.initAll();

            // Wire forms (safety)
            document.querySelectorAll('form[data-entity]').forEach(f => {
                if (!f.__hiking_wired) {
                    f.addEventListener('submit', function (ev) {
                        ev.preventDefault();
                        HikingApp.utils.handleFormSubmit(f);
                    });
                    f.__hiking_wired = true;
                }
            });

            // Populate dropdowns
            HikingApp.utils.populateDropdown('#routeParkDropdown', '/api/parks', 'ParkID', 'ParkName');
            HikingApp.utils.populateDropdown('#routeImageRouteDropdown', '/api/routes', 'RouteID', 'RouteName');
            HikingApp.utils.populateDropdown('#gpxRouteDropdown', '/api/routes', 'RouteID', 'RouteName');
            HikingApp.utils.populateDropdown('#routeFeedbackDropdown', '/api/routes', 'RouteID', 'RouteName');

            // Load tables & grids
            await Promise.all([
                HikingApp.entities.loadParksIntoTable(),
                HikingApp.entities.loadRoutesIntoTable(),
                HikingApp.entities.loadImagesIntoGrid(),
                HikingApp.entities.loadGpxIntoTable(),
                HikingApp.entities.loadFeedbackIntoTable()
            ]);

            console.log('HikingApp ready');
        } catch (err) {
            console.error('HikingApp init error', err);
        }
    });

})(window.HikingApp || {}, document);















