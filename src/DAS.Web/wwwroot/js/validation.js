(function ($) {
    'use strict';

    if (!$ || !$.validator) {
        return;
    }

    $.extend($.validator.messages, {
        required: 'Este campo es obligatorio.',
        remote: 'Revisa este campo.',
        email: 'Escribe un correo electrónico válido.',
        url: 'Escribe una dirección web válida.',
        date: 'Escribe una fecha válida.',
        dateISO: 'Escribe una fecha válida (AAAA-MM-DD).',
        number: 'Escribe un número válido.',
        digits: 'Escribe solo dígitos.',
        equalTo: 'Escribe el mismo valor otra vez.',
        maxlength: $.validator.format('Escribe como máximo {0} caracteres.'),
        minlength: $.validator.format('Escribe al menos {0} caracteres.'),
        rangelength: $.validator.format('Escribe entre {0} y {1} caracteres.'),
        range: $.validator.format('Escribe un valor entre {0} y {1}.'),
        max: $.validator.format('Escribe un valor menor o igual a {0}.'),
        min: $.validator.format('Escribe un valor mayor o igual a {0}.'),
        step: $.validator.format('Escribe un múltiplo de {0}.')
    });

    $.validator.addMethod('plaintext', function (value, element) {
        return this.optional(element) || !/[<>]/.test(value);
    });

    if ($.validator.unobtrusive) {
        $.validator.unobtrusive.adapters.addBool('plaintext');
    }
}(window.jQuery));
