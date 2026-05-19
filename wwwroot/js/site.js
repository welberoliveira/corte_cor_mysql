// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", function () {
    function normalizeSmartSelectText(value) {
        return (value || "")
            .toString()
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .toLowerCase();
    }

    function applySmartSelectFilter(select, input) {
        const term = normalizeSmartSelectText(input.value);
        const selectedValue = select.value;
        Array.from(select.options).forEach(function (option) {
            const isPlaceholder = option.value === "";
            const isSelected = option.value === selectedValue;
            const matches = !term || normalizeSmartSelectText(option.textContent).includes(term);
            option.hidden = !isPlaceholder && !isSelected && !matches;
        });
    }

    function initSmartSelect(select) {
        if (!select || select.dataset.smartReady === "true") {
            return;
        }

        select.dataset.smartReady = "true";
        const input = document.createElement("input");
        input.type = "search";
        input.className = "form-control smart-select-filter mb-1";
        input.placeholder = select.dataset.smartPlaceholder || "Digite para pesquisar...";
        input.autocomplete = "off";
        input.disabled = select.disabled;
        input.setAttribute("aria-label", select.dataset.smartLabel || "Pesquisar opções");
        select.parentNode.insertBefore(input, select);

        input.addEventListener("input", function () {
            applySmartSelectFilter(select, input);
            if (input.value && select.options.length > 8) {
                select.size = Math.min(8, Array.from(select.options).filter(option => !option.hidden).length || 1);
            }
        });

        input.addEventListener("focus", function () {
            applySmartSelectFilter(select, input);
        });

        input.addEventListener("blur", function () {
            window.setTimeout(function () {
                select.size = 0;
            }, 150);
        });

        select.addEventListener("change", function () {
            input.value = "";
            applySmartSelectFilter(select, input);
            select.size = 0;
        });

        new MutationObserver(function () {
            input.disabled = select.disabled;
            applySmartSelectFilter(select, input);
        }).observe(select, { childList: true, subtree: true, attributes: true, attributeFilter: ["disabled"] });
    }

    document.querySelectorAll("select[data-smart-select]").forEach(initSmartSelect);

    document.querySelectorAll("form[data-confirm-submit], form[data-submit-feedback]").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            var message = form.getAttribute("data-confirm-submit");
            if (message && !window.confirm(message)) {
                event.preventDefault();
                return;
            }

            var button = form.querySelector("button[type='submit'], button:not([type])");
            if (!button) {
                return;
            }

            button.disabled = true;
            button.dataset.originalText = button.textContent || "";
            button.textContent = button.getAttribute("data-loading-text") || "Processando...";
        });
    });
});
