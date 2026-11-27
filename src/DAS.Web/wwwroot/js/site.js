(function (window, document) {
    'use strict';

    const defaultMessage = 'No fue posible completar la solicitud. Inténtalo de nuevo.';
    const statusMessages = {
        0: 'No hay conexión con el servidor. Revisa la red e inténtalo de nuevo.',
        400: 'Revisa los datos enviados.',
        401: 'Tu sesión terminó. Vuelve a iniciar sesión.',
        403: 'Tu rol no permite realizar esta acción.',
        404: 'El recurso solicitado ya no existe.',
        415: 'El formato de la solicitud no es compatible.',
        429: 'Demasiadas solicitudes. Espera un momento e inténtalo de nuevo.'
    };

    class ApiError extends Error {
        constructor(status, problem) {
            super(problem?.detail || problem?.title || statusMessages[status] || defaultMessage);
            this.name = 'ApiError';
            this.status = status;
            this.problem = problem ?? null;
            this.errors = problem?.errors ?? {};
        }
    }

    const metaContent = name => document.querySelector(`meta[name="${name}"]`)?.getAttribute('content') ?? '';
    const isSafeMethod = method => ['GET', 'HEAD', 'OPTIONS'].includes(method.toUpperCase());

    function csrfHeaders() {
        const token = metaContent('csrf-token');
        return token ? { [metaContent('csrf-header') || 'X-CSRF-TOKEN']: token } : {};
    }

    function parseJson(text) {
        try {
            return text ? JSON.parse(text) : null;
        } catch {
            return null;
        }
    }

    async function readProblem(response) {
        const type = response.headers.get('Content-Type') ?? '';
        return type.includes('json') ? parseJson(await response.text()) : null;
    }

    function sessionExpired() {
        notify('error', statusMessages[401]);
        window.setTimeout(() => window.location.reload(), 1500);
    }

    function isLoginRedirect(response) {
        return response.redirected && /\/cuenta\/ingresar/i.test(new URL(response.url).pathname);
    }

    async function send(url, { method = 'GET', body, accept = 'application/json' } = {}) {
        const headers = { Accept: accept, 'X-Requested-With': 'XMLHttpRequest' };
        if (!isSafeMethod(method)) {
            Object.assign(headers, csrfHeaders());
        }

        if (body !== undefined) {
            headers['Content-Type'] = 'application/json';
        }

        let response;
        try {
            response = await fetch(url, {
                method,
                headers,
                body: body === undefined ? undefined : JSON.stringify(body),
                credentials: 'same-origin',
                cache: 'no-store'
            });
        } catch {
            throw new ApiError(0, null);
        }

        if (response.status === 401 || isLoginRedirect(response)) {
            sessionExpired();
            throw new ApiError(401, await readProblem(response));
        }

        if (!response.ok) {
            throw new ApiError(response.status, await readProblem(response));
        }

        return response;
    }

    const api = {
        csrfHeaders,
        async getJson(url) {
            return (await send(url)).json();
        },
        async getHtml(url) {
            return (await send(url, { accept: 'text/html' })).text();
        },
        async postJson(url, body) {
            const response = await send(url, { method: 'POST', body });
            return response.status === 204 ? null : response.json();
        },
        fromXhr(xhr) {
            if (xhr.status === 401) {
                sessionExpired();
            }

            return new ApiError(xhr.status, xhr.responseJSON ?? parseJson(xhr.responseText));
        }
    };

    function notify(type, message) {
        const stack = document.getElementById('asyncNotices');
        if (!stack) {
            return;
        }

        const isError = type === 'error';
        const notice = document.createElement('div');
        notice.className = `notice ${isError ? 'notice-error' : 'notice-success'} toast-notice`;
        notice.setAttribute('role', isError ? 'alert' : 'status');

        const text = document.createElement('span');
        text.textContent = message;

        const close = document.createElement('button');
        close.type = 'button';
        close.className = 'toast-close';
        close.setAttribute('aria-label', 'Cerrar aviso');
        close.textContent = '×';
        close.addEventListener('click', () => notice.remove());

        notice.append(text, close);
        stack.append(notice);
        window.setTimeout(() => notice.remove(), isError ? 9000 : 5000);
    }

    const forms = {
        values(form) {
            const values = {};
            new FormData(form).forEach((value, key) => {
                if (key !== '__RequestVerificationToken' && key !== 'returnUrl') {
                    values[key] = typeof value === 'string' ? value.trim() : value;
                }
            });
            return values;
        },
        isValid(form) {
            const $ = window.jQuery;
            if ($ && $.fn.valid && $(form).data('validator')) {
                return $(form).valid();
            }

            return form.reportValidity();
        },
        clearErrors(form) {
            form.querySelectorAll('[data-valmsg-for]').forEach(span => {
                span.textContent = '';
                span.classList.remove('field-validation-error');
                span.classList.add('field-validation-valid');
            });
            form.querySelectorAll('.input-validation-error').forEach(input => input.classList.remove('input-validation-error'));
            const summary = form.querySelector('[data-role="form-errors"]');
            if (summary) {
                summary.textContent = '';
            }
        },
        showErrors(form, error) {
            forms.clearErrors(form);
            const general = [];
            const entries = Object.entries(error?.errors ?? {});
            const spans = [...form.querySelectorAll('[data-valmsg-for]')];

            for (const [key, messages] of entries) {
                const field = key.split('.').pop().replace(/^\$/, '').toLowerCase();
                const span = field ? spans.find(item => item.dataset.valmsgFor.toLowerCase() === field) : null;
                const text = [].concat(messages).join(' ');
                if (span) {
                    span.textContent = text;
                    span.classList.remove('field-validation-valid');
                    span.classList.add('field-validation-error');
                    form.querySelector(`[name="${span.dataset.valmsgFor}"]`)?.classList.add('input-validation-error');
                } else {
                    general.push(text);
                }
            }

            if (entries.length === 0) {
                general.push(error?.message ?? defaultMessage);
            }

            const summary = form.querySelector('[data-role="form-errors"]');
            if (general.length === 0) {
                return;
            }

            if (summary) {
                summary.textContent = general.join(' ');
            } else {
                notify('error', general.join(' '));
            }
        },
        setBusy(form, busy) {
            form.setAttribute('aria-busy', String(busy));
            form.querySelectorAll('button[type="submit"]').forEach(button => {
                button.disabled = busy;
                button.setAttribute('aria-busy', String(busy));
            });
        }
    };

    const format = {
        quantity: value => String(Math.round(Number(value) * 100) / 100),
        precise: value => String(Math.round(Number(value) * 10000) / 10000),
        count: value => String(value).padStart(2, '0')
    };

    document.addEventListener('submit', event => {
        const form = event.target;
        if (form instanceof HTMLFormElement && form.dataset.confirm && !window.confirm(form.dataset.confirm)) {
            event.preventDefault();
            event.stopImmediatePropagation();
        }
    }, true);

    window.pipiris = Object.freeze({ api, notify, forms, format, ApiError });
})(window, document);
