// VietQR payment page — copy-to-clipboard for transfer content
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var btn  = document.getElementById('btnCopyContent');
        var code = document.getElementById('transferContent');
        if (!btn || !code) return;

        btn.addEventListener('click', function () {
            navigator.clipboard.writeText(code.textContent.trim())
                .then(function () {
                    var orig = btn.textContent;
                    btn.textContent = 'Đã sao chép!';
                    setTimeout(function () { btn.textContent = orig; }, 1800);
                })
                .catch(function () {
                    // Fallback for older browsers
                    var ta = document.createElement('textarea');
                    ta.value = code.textContent.trim();
                    ta.style.position = 'fixed';
                    ta.style.opacity  = '0';
                    document.body.appendChild(ta);
                    ta.focus();
                    ta.select();
                    document.execCommand('copy');
                    document.body.removeChild(ta);
                    btn.textContent = 'Đã sao chép!';
                    setTimeout(function () { btn.textContent = 'Sao chép'; }, 1800);
                });
        });
    });
}());
