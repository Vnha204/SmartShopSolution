(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var qtyInput = document.getElementById('detailQty');
        var maxStock = qtyInput ? (parseInt(qtyInput.max, 10) || 99) : 99;

        var minusBtn = document.querySelector('.qty-minus');
        var plusBtn  = document.querySelector('.qty-plus');

        if (minusBtn && qtyInput) {
            minusBtn.addEventListener('click', function () {
                var val = parseInt(qtyInput.value, 10) || 1;
                if (val > 1) qtyInput.value = val - 1;
            });
        }

        if (plusBtn && qtyInput) {
            plusBtn.addEventListener('click', function () {
                var val = parseInt(qtyInput.value, 10) || 1;
                if (val < maxStock) qtyInput.value = val + 1;
            });
        }

        if (qtyInput) {
            qtyInput.addEventListener('change', function () {
                var val = parseInt(qtyInput.value, 10);
                if (isNaN(val) || val < 1) {
                    qtyInput.value = 1;
                } else if (val > maxStock) {
                    qtyInput.value = maxStock;
                }
            });
        }

        var addCartBtn = document.querySelector('.btn-detail-cart');
        if (addCartBtn) {
            addCartBtn.addEventListener('click', function () {
                var productId = addCartBtn.dataset.productId;
                var qty       = qtyInput ? (parseInt(qtyInput.value, 10) || 1) : 1;
                var meta      = document.querySelector('meta[name="csrf-token"]');

                if (!productId || !meta) return;

                var originalHtml = addCartBtn.innerHTML;
                addCartBtn.disabled = true;
                addCartBtn.innerHTML = '<i class="fa fa-spinner fa-spin me-2"></i>Đang thêm...';

                fetch('/Customers/Cart/Add', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'X-Requested-With': 'XMLHttpRequest',
                        'RequestVerificationToken': meta.content
                    },
                    body: 'productId=' + encodeURIComponent(productId)
                        + '&quantity=' + encodeURIComponent(qty)
                })
                .then(function (r) {
                    var lowerUrl = r.url.toLowerCase();
                    if (r.redirected && (lowerUrl.indexOf('/auth/login') !== -1 || lowerUrl.indexOf('/account/login') !== -1)) {
                        window.location.href = r.url;
                        return null;
                    }
                    var ct = r.headers.get('content-type') || '';
                    if (ct.indexOf('text/html') !== -1) {
                        window.location.href = '/Auth/Login';
                        return null;
                    }
                    return r.ok ? r.json() : null;
                })
                .then(function (data) {
                    if (!data) return;
                    showToast(data.success,
                        data.message || (data.success ? 'Đã thêm vào giỏ hàng!' : 'Có lỗi xảy ra.'));
                    if (data.success) incrementCartBadge(qty);
                })
                .catch(function () {
                    showToast(false, 'Có lỗi xảy ra. Vui lòng thử lại.');
                })
                .finally(function () {
                    addCartBtn.disabled = false;
                    addCartBtn.innerHTML = originalHtml;
                });
            });
        }

        var buyNowBtn = document.querySelector('.btn-detail-buynow');
        if (buyNowBtn) {
            buyNowBtn.addEventListener('click', function () {
                var productId = buyNowBtn.dataset.productId;
                var qty       = qtyInput ? (parseInt(qtyInput.value, 10) || 1) : 1;
                var meta      = document.querySelector('meta[name="csrf-token"]');

                if (!productId || !meta) return;

                var originalHtml = buyNowBtn.innerHTML;
                buyNowBtn.disabled = true;
                buyNowBtn.innerHTML = '<i class="fa fa-spinner fa-spin me-2"></i>Đang xử lý...';

                fetch('/Customers/Cart/Add', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'X-Requested-With': 'XMLHttpRequest',
                        'RequestVerificationToken': meta.content
                    },
                    body: 'productId=' + encodeURIComponent(productId)
                        + '&quantity=' + encodeURIComponent(qty)
                })
                .then(function (r) {
                    var lowerUrl = r.url.toLowerCase();
                    if (r.redirected && (lowerUrl.indexOf('/auth/login') !== -1 || lowerUrl.indexOf('/account/login') !== -1)) {
                        window.location.href = r.url;
                        return null;
                    }
                    var ct = r.headers.get('content-type') || '';
                    if (ct.indexOf('text/html') !== -1) {
                        window.location.href = '/Auth/Login';
                        return null;
                    }
                    return r.ok ? r.json() : null;
                })
                .then(function (data) {
                    if (!data) return;
                    if (data.success) {
                        window.location.href = '/Customers/Checkout?payment=payos';
                    } else {
                        showToast(false, data.message || 'Có lỗi xảy ra.');
                        buyNowBtn.disabled = false;
                        buyNowBtn.innerHTML = originalHtml;
                    }
                })
                .catch(function () {
                    showToast(false, 'Có lỗi xảy ra. Vui lòng thử lại.');
                    buyNowBtn.disabled = false;
                    buyNowBtn.innerHTML = originalHtml;
                });
            });
        }
    });

    function showToast(success, message) {
        var toastEl = document.getElementById('cartToast');
        var body    = document.getElementById('cartToastBody');
        if (!toastEl || !body) return;

        body.textContent = message;
        toastEl.className = 'toast align-items-center border-0 text-white '
            + (success ? 'bg-success' : 'bg-danger');

        bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 2500 }).show();
    }

    function incrementCartBadge(qty) {
        var badge = document.querySelector('.cart-nav-link .cart-badge');
        if (badge) {
            badge.textContent = parseInt(badge.textContent || '0', 10) + qty;
        } else {
            var link = document.querySelector('.cart-nav-link');
            if (link) {
                var span = document.createElement('span');
                span.className   = 'cart-badge';
                span.textContent = qty;
                link.appendChild(span);
            }
        }
    }

})();
