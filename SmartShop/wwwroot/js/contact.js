/**
 * contact.js - Contact form AJAX handler
 * SmartShop - dùng chung cho trang chủ và trang liên hệ
 */

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var form = document.getElementById('request');
        if (!form) return;

        form.addEventListener('submit', handleSubmit);
    });

    async function handleSubmit(e) {
        e.preventDefault();

        var form = e.currentTarget;
        var btn  = form.querySelector('[type="submit"]');

        clearMessages(form);

        var originalText = btn.textContent;
        btn.disabled     = true;
        btn.textContent  = 'Đang gửi...';

        var csrfToken = (document.querySelector('meta[name="csrf-token"]') || {}).content || '';

        var body = new URLSearchParams({
            __RequestVerificationToken: csrfToken,
            FullName: (form.querySelector('[name="Name"]')        || {}).value || '',
            Phone:    (form.querySelector('[name="PhoneNumber"]') || {}).value || '',
            Email:    (form.querySelector('[name="Email"]')       || {}).value || '',
            Message:  (form.querySelector('[name="Message"]')     || {}).value || ''
        });

        try {
            var res  = await fetch('/Contact/Submit', {
                method:  'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body:    body.toString()
            });
            var data = await res.json();

            if (data.success) {
                showSuccess(form, data.message || 'Gửi thành công! Chúng tôi sẽ phản hồi sớm nhất.');
                form.reset();
            } else if (data.errors && Object.keys(data.errors).length > 0) {
                showFieldErrors(form, data.errors);
            } else {
                showGeneralError(form, data.message || 'Có lỗi xảy ra, vui lòng thử lại sau.');
            }
        } catch {
            showGeneralError(form, 'Có lỗi xảy ra, vui lòng thử lại sau.');
        } finally {
            btn.disabled    = false;
            btn.textContent = originalText;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    function clearMessages(form) {
        form.querySelectorAll('.contact-msg').forEach(function (el) { el.remove(); });
    }

    function showSuccess(form, message) {
        var el = createMessage(message, '#2e7d32', '#e8f5e9');
        form.appendChild(el);
    }

    function showGeneralError(form, message) {
        var el = createMessage(message, '#c62828', '#ffebee');
        form.appendChild(el);
    }

    function showFieldErrors(form, errors) {
        // Map ViewModel property names → form input name attributes
        var fieldMap = {
            FullName: 'Name',
            Phone:    'PhoneNumber',
            Email:    'Email',
            Message:  'Message'
        };

        var unmatched = [];

        Object.keys(errors).forEach(function (key) {
            var inputName = fieldMap[key] || key;
            var input     = form.querySelector('[name="' + inputName + '"]');

            if (input) {
                var err = createMessage(errors[key], '#c62828', '#ffebee');
                err.style.fontSize  = '12px';
                err.style.marginTop = '4px';
                input.insertAdjacentElement('afterend', err);
            } else {
                unmatched.push(errors[key]);
            }
        });

        if (unmatched.length > 0) {
            showGeneralError(form, unmatched.join(' '));
        }
    }

    function createMessage(text, color, background) {
        var el            = document.createElement('p');
        el.className      = 'contact-msg';
        el.textContent    = text;
        el.style.color    = color;
        el.style.background = background;
        el.style.padding  = '8px 12px';
        el.style.borderRadius = '4px';
        el.style.margin   = '8px 0 0';
        return el;
    }

}());
