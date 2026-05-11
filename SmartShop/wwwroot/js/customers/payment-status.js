(function () {
    'use strict';

    var section = document.querySelector('.payos-section[data-order-id]');
    if (!section) return;

    var orderId = section.dataset.orderId;
    if (!orderId) return;

    var statusBanner = document.getElementById('payos-status-banner');
    var statusText   = document.getElementById('payos-status-text');
    var statusBadge  = document.getElementById('payos-status-badge');

    var POLL_INTERVAL_MS = 4000;
    var intervalId;

    function poll() {
        fetch('/Customers/Payments/PaymentStatus?orderId=' + orderId, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            credentials: 'same-origin'
        })
        .then(function (res) {
            if (!res.ok) throw new Error('HTTP ' + res.status);
            return res.json();
        })
        .then(function (data) {
            if (data.status === 'paid') {
                stopPolling();
                showPaid();
            }
        })
        .catch(function (err) {
            console.warn('PayOS status poll error:', err);
        });
    }

    function stopPolling() {
        if (intervalId) {
            clearInterval(intervalId);
            intervalId = null;
        }
    }

    function showPaid() {
        if (statusBanner) {
            statusBanner.className = 'payos-status-banner payos-status-paid';
            statusBanner.innerHTML =
                '<i class="fa fa-check-circle"></i> ' +
                '<span>Đơn hàng đã được xác nhận thanh toán. Cảm ơn bạn!</span>';
        }
        if (statusBadge) {
            statusBadge.className   = 'payos-badge payos-badge-paid';
            statusBadge.textContent = 'Đã thanh toán';
        }

        setTimeout(function () {
            window.location.href = '/account/OrderDetail?orderId=' + orderId;
        }, 2500);
    }

    poll();
    intervalId = setInterval(poll, POLL_INTERVAL_MS);
}());
