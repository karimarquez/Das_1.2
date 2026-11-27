(function (window, document, $) {
    'use strict';

    const app = window.pipiris;
    const page = document.querySelector('[data-page="inventory"]');
    if (!page || !app) {
        return;
    }

    const rows = [...page.querySelectorAll('.inventory-table tbody tr[data-product-id]')];
    const search = document.getElementById('productSearch');
    const category = document.getElementById('categoryFilter');
    const emptySearch = document.getElementById('emptySearch');
    const activity = document.getElementById('recentActivity');
    const lastUpdated = page.querySelector('[data-role="last-updated"]');
    const refreshButton = page.querySelector('[data-action="refresh-dashboard"]');
    const movementForm = document.getElementById('movementForm');
    const countForm = document.getElementById('countForm');
    let refreshing = null;

    function filterProducts() {
        const query = search.value.trim().toLocaleLowerCase();
        const selected = category.value;
        let visible = 0;
        for (const row of rows) {
            const matches = row.dataset.product.includes(query) && (!selected || row.dataset.category === selected);
            row.hidden = !matches;
            visible += matches ? 1 : 0;
        }

        emptySearch.hidden = visible > 0;
    }

    function stockLevelClass(stock, minimum) {
        const percent = minimum <= 0 ? 100 : Math.min(Math.max((stock / (minimum * 2)) * 100, 4), 100);
        return `stock-w-${Math.round(percent / 5) * 5}`;
    }

    function flash(row) {
        row.classList.remove('row-updated');
        void row.offsetWidth;
        row.classList.add('row-updated');
    }

    function applyProduct(product) {
        const row = page.querySelector(`tr[data-product-id="${product.id}"]`);
        if (!row) {
            return;
        }

        const stockText = app.format.quantity(product.stock);
        const stock = row.querySelector('[data-role="stock"]');
        const changed = stock.textContent !== stockText;
        stock.textContent = stockText;
        row.querySelector('[data-role="stock-fill"]').className =
            `stock-fill${product.isLowStock ? ' stock-fill-low' : ''} ${stockLevelClass(product.stock, product.minimumStock)}`;

        const status = row.querySelector('[data-role="status"]');
        status.className = `status-pill ${product.isLowStock ? 'status-low' : 'status-ok'}`;
        status.textContent = product.isLowStock ? 'Reponer' : 'Disponible';

        const countButton = row.querySelector('.count-button');
        if (countButton) {
            countButton.dataset.currentStock = stockText;
        }

        const option = document.querySelector(`#movementProduct option[value="${product.id}"]`);
        if (option) {
            option.textContent = `${product.name} · ${stockText} ${product.unit}`;
        }

        if (changed) {
            flash(row);
        }
    }

    function applyMetrics(dashboard) {
        page.querySelectorAll('[data-metric]').forEach(element => {
            const value = dashboard[element.dataset.metric];
            if (typeof value === 'number') {
                element.textContent = app.format.count(value);
            }
        });
    }

    function replaceActivity(html) {
        const fragment = new DOMParser().parseFromString(html, 'text/html');
        activity.replaceChildren(...fragment.body.childNodes);
    }

    function markUpdated() {
        const time = new Date().toLocaleTimeString('es-SV', { hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23' });
        lastUpdated.textContent = `Actualizado a las ${time}`;
    }

    function refreshDashboard() {
        if (refreshing) {
            return refreshing;
        }

        refreshButton?.setAttribute('aria-busy', 'true');
        refreshing = Promise.all([
            app.api.getJson(page.dataset.dashboardUrl),
            app.api.getHtml(page.dataset.activityUrl)
        ])
            .then(([dashboard, html]) => {
                applyMetrics(dashboard);
                dashboard.products.forEach(applyProduct);
                replaceActivity(html);
                markUpdated();
            })
            .finally(() => {
                refreshButton?.removeAttribute('aria-busy');
                refreshing = null;
            });

        return refreshing;
    }

    function hideModal(id) {
        const element = document.getElementById(id);
        if (element && window.bootstrap) {
            window.bootstrap.Modal.getOrCreateInstance(element).hide();
        }
    }

    function resetForm(form) {
        form.reset();
        app.forms.clearErrors(form);
        if ($ && $(form).data('validator')) {
            $(form).validate().resetForm();
        }
    }

    function describeCount(line) {
        if (line.difference === 0) {
            return `Conteo de ${line.productName} sin diferencias.`;
        }

        const kind = line.difference > 0 ? 'sobrante' : 'faltante';
        return `Conteo de ${line.productName}: ${kind} de ${app.format.quantity(Math.abs(line.difference))} ${line.unit}. Se registró el ajuste.`;
    }

    search?.addEventListener('input', filterProducts);
    category?.addEventListener('change', filterProducts);

    refreshButton?.addEventListener('click', () => {
        refreshDashboard().catch(error => app.notify('error', error.message));
    });

    window.setInterval(() => {
        if (document.visibilityState === 'visible' && !document.querySelector('.modal.show')) {
            refreshDashboard().catch(() => { });
        }
    }, 60000);

    document.getElementById('movementModal')?.addEventListener('show.bs.modal', () => resetForm(movementForm));

    movementForm?.addEventListener('submit', async event => {
        event.preventDefault();
        if (!app.forms.isValid(movementForm)) {
            return;
        }

        const values = app.forms.values(movementForm);
        app.forms.clearErrors(movementForm);
        app.forms.setBusy(movementForm, true);
        try {
            const result = await app.api.postJson(movementForm.dataset.asyncUrl, {
                productId: Number(values.ProductId),
                type: values.Type,
                quantity: Number(values.Quantity),
                reason: values.Reason,
                date: values.Date || null
            });

            const movement = result.movement;
            applyProduct(result.product);
            hideModal('movementModal');
            app.notify('success', `Movimiento registrado: ${movement.type} de ${app.format.quantity(movement.quantity)} ${movement.unit} · ${movement.productName}.`);
            refreshDashboard().catch(error => app.notify('error', error.message));
        } catch (error) {
            app.forms.showErrors(movementForm, error);
        } finally {
            app.forms.setBusy(movementForm, false);
        }
    });

    page.querySelectorAll('.count-button').forEach(button => button.addEventListener('click', () => {
        resetForm(countForm);
        document.getElementById('countProductId').value = button.dataset.productId;
        document.getElementById('countProductLabel').textContent =
            `${button.dataset.productName} · saldo actual ${button.dataset.currentStock} ${button.dataset.unit}`;
        document.getElementById('actualStock').value = button.dataset.currentStock;
    }));

    if (countForm && $) {
        $(countForm).on('submit', event => {
            event.preventDefault();
            if (!app.forms.isValid(countForm)) {
                return;
            }

            const values = app.forms.values(countForm);
            app.forms.clearErrors(countForm);
            app.forms.setBusy(countForm, true);
            $.ajax({
                url: countForm.dataset.asyncUrl,
                method: 'POST',
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                headers: app.api.csrfHeaders(),
                data: JSON.stringify({ productId: Number(values.ProductId), actualStock: Number(values.ActualStock) })
            })
                .done(result => {
                    applyProduct(result.product);
                    hideModal('countModal');
                    app.notify('success', describeCount(result.count.lines[0]));
                    refreshDashboard().catch(error => app.notify('error', error.message));
                })
                .fail(xhr => app.forms.showErrors(countForm, app.api.fromXhr(xhr)))
                .always(() => app.forms.setBusy(countForm, false));
        });
    }
})(window, document, window.jQuery);
