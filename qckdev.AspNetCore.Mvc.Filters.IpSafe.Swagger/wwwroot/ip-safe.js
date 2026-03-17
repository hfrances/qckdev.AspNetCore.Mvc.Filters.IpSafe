(function () {
    "use strict";

    var DECORATED_ATTR = "data-ip-safe-decorated";

    function getSwaggerJsonUrl() {
        if (!window.ui || !window.ui.getConfigs) {
            return null;
        }

        var configs = window.ui.getConfigs();
        if (!configs) {
            return null;
        }

        if (configs.urls && configs.urls.length > 0 && configs.urls[0].url) {
            return configs.urls[0].url;
        }

        return configs.url || null;
    }

    function buildIpSafeMap(swaggerDoc) {
        var map = {};
        if (!swaggerDoc || !swaggerDoc.paths) {
            return map;
        }

        Object.keys(swaggerDoc.paths).forEach(function (path) {
            var pathItem = swaggerDoc.paths[path] || {};
            Object.keys(pathItem).forEach(function (method) {
                var operation = pathItem[method];
                if (!operation || operation["x-ip-safe"] !== true) {
                    return;
                }

                var key = method.toUpperCase() + " " + path;
                map[key] = true;
            });
        });

        return map;
    }

    function decorateOperations(ipSafeMap) {
        var opblocks = document.querySelectorAll(".swagger-ui .opblock");
        for (var i = 0; i < opblocks.length; i++) {
            var opblock = opblocks[i];
            if (opblock.getAttribute(DECORATED_ATTR) === "true") {
                continue;
            }

            var methodEl = opblock.querySelector(".opblock-summary-method");
            var pathEl = opblock.querySelector(".opblock-summary-path");
            var summaryControl = opblock.querySelector(".opblock-summary-control");
            var summaryContainer = opblock.querySelector(".opblock-summary");

            if (!methodEl || !pathEl || !summaryControl || !summaryContainer) {
                continue;
            }

            var method = (methodEl.textContent || "").trim().toUpperCase();
            var path = (pathEl.textContent || "").trim();
            var key = method + " " + path;

            if (!ipSafeMap[key]) {
                opblock.setAttribute(DECORATED_ATTR, "true");
                continue;
            }

            var badge = document.createElement("div");
            badge.className = "ip-safe-indicator";
            badge.innerHTML = "<svg class=\"ip-safe-icon\" viewBox=\"0 0 24 24\" aria-hidden=\"true\" focusable=\"false\"><circle cx=\"6\" cy=\"12\" r=\"2\"/><circle cx=\"18\" cy=\"7\" r=\"2\"/><circle cx=\"18\" cy=\"17\" r=\"2\"/><path d=\"M8 12h4\"/><path d=\"M14 12l2.2-3.3\"/><path d=\"M14 12l2.2 3.3\"/></svg>";
            badge.setAttribute("role", "img");
            badge.setAttribute("aria-label", "IP protected endpoint");
            badge.title = "Endpoint protected by IP filter";

            // Place the IP badge after copy-to-clipboard when available.
            var copyButton = summaryContainer.querySelector(".copy-to-clipboard");
            if (copyButton && copyButton.parentNode === summaryContainer) {
                if (copyButton.nextSibling) {
                    summaryContainer.insertBefore(badge, copyButton.nextSibling);
                } else {
                    summaryContainer.appendChild(badge);
                }
            } else {
                summaryControl.appendChild(badge);
            }
            opblock.setAttribute(DECORATED_ATTR, "true");
        }
    }

    function watchAndDecorate(ipSafeMap) {
        decorateOperations(ipSafeMap);

        var root = document.querySelector(".swagger-ui");
        if (!root || !window.MutationObserver) {
            return;
        }

        var observer = new MutationObserver(function () {
            decorateOperations(ipSafeMap);
        });

        observer.observe(root, { childList: true, subtree: true });
    }

    function init() {
        var swaggerJsonUrl = getSwaggerJsonUrl();
        if (!swaggerJsonUrl) {
            return;
        }

        fetch(swaggerJsonUrl, { credentials: "same-origin" })
            .then(function (response) { return response.json(); })
            .then(function (swaggerDoc) {
                var ipSafeMap = buildIpSafeMap(swaggerDoc);
                watchAndDecorate(ipSafeMap);
            })
            .catch(function () {
            });
    }

    function waitForSwaggerUi() {
        var attempts = 0;
        var timer = setInterval(function () {
            attempts++;
            if (window.ui && document.querySelector(".swagger-ui")) {
                clearInterval(timer);
                init();
                return;
            }

            if (attempts > 100) {
                clearInterval(timer);
            }
        }, 100);
    }

    waitForSwaggerUi();
})();
