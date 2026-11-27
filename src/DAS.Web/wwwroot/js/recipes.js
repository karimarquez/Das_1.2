(function (window, document) {
    'use strict';

    const app = window.pipiris;
    const page = document.querySelector('[data-page="recipes"]');
    if (!page || !app) {
        return;
    }

    function describe(preparation) {
        const detail = preparation.deductions
            .map(item => `${item.productName} ${app.format.precise(item.quantity)} ${item.unit}`)
            .join(', ');
        return `Preparación registrada: ${preparation.servings} porción(es) de ${preparation.recipeName}. Se descontó ${detail}.`;
    }

    page.querySelectorAll('form[data-async-url]').forEach(form => form.addEventListener('submit', async event => {
        event.preventDefault();
        if (!app.forms.isValid(form)) {
            return;
        }

        const output = form.closest('.recipe-card').querySelector('[data-role="recipe-result"]');
        const values = app.forms.values(form);
        app.forms.clearErrors(form);
        app.forms.setBusy(form, true);
        try {
            const preparation = await app.api.postJson(form.dataset.asyncUrl, { servings: Number(values.Servings) });
            const message = describe(preparation);
            output.textContent = message;
            output.hidden = false;
            app.notify('success', message);
        } catch (error) {
            output.hidden = true;
            app.forms.showErrors(form, error);
        } finally {
            app.forms.setBusy(form, false);
        }
    }));
})(window, document);
