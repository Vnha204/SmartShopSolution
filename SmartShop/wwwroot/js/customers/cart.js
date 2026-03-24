// Cart page — AJAX cart actions (no full page reloads)
(function () {
    'use strict';

    var CSRF_META = 'meta[name="csrf-token"]';

    function csrfToken() {
        var m = document.querySelector(CSRF_META);
        return m ? m.content : '';
    }

    // ── Vietnamese currency formatter ────────────────────────────────────────
    function vnd(amount) {
        return Number(amount).toLocaleString('vi-VN') + '₫';
    }

    // ── Bootstrap 5 toast ───────────────────────────────────────────────────
    function showToast(success, message) {
        var toastEl = document.getElementById('cartToast');
        var body    = document.getElementById('cartToastBody');
        if (!toastEl || !body) return;
        body.textContent = message;
        toastEl.className = 'toast align-items-center border-0 text-white '
            + (success ? 'bg-success' : 'bg-danger');
        bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 2500 }).show();
    }

    // ── Navbar cart badge ───────────────────────────────────────────────────
    function setBadge(count) {
        var badge = document.querySelector('.cart-nav-link .cart-badge');
        if (count <= 0) {
            if (badge) badge.remove();
            return;
        }
        if (badge) {
            badge.textContent = count;
        } else {
            var link = document.querySelector('.cart-nav-link');
            if (link) {
                var span = document.createElement('span');
                span.className   = 'cart-badge';
                span.textContent = count;
                link.appendChild(span);
            }
        }
    }

    // ── Apply cart data to DOM ───────────────────────────────────────────────
    function applyCart(cartData) {
        setBadge(cartData.badgeCount);

        var body = document.getElementById('cart-body');
        if (!body) return;

        if (cartData.isEmpty) {
            body.outerHTML =
                '<div id="cart-body" class="cart-empty">' +
                    '<i class="fa fa-shopping-cart"></i>' +
                    '<p>Giỏ hàng của bạn đang trống.</p>' +
                    '<a href="/Customers/Shop" class="btn btn-primary">Tiếp tục mua sắm</a>' +
                '</div>';
            return;
        }

        // Update each existing row; remove rows no longer present
        var tbody = body.querySelector('tbody');
        if (tbody) {
            // Remove rows whose IDs are no longer in the response
            var itemIds = cartData.items.map(function (i) { return String(i.cartItemId); });
            tbody.querySelectorAll('tr[data-item-id]').forEach(function (row) {
                if (itemIds.indexOf(row.dataset.itemId) === -1) row.remove();
            });

            // Update quantities and subtotals for remaining rows
            cartData.items.forEach(function (item) {
                var row = tbody.querySelector('tr[data-item-id="' + item.cartItemId + '"]');
                if (!row) return;
                var qtyInput = row.querySelector('.qty-input');
                if (qtyInput) qtyInput.value = item.quantity;
                var subtotal = row.querySelector('.cart-subtotal');
                if (subtotal) subtotal.textContent = vnd(item.subtotal);
            });
        }

        // Update summary
        var totalQtyEl = body.querySelector('[data-field="total-qty"]');
        if (totalQtyEl) totalQtyEl.textContent = cartData.totalQuantity;
        var totalEl = body.querySelector('[data-field="total"]');
        if (totalEl) totalEl.textContent = vnd(cartData.total);
    }

    // ── Generic cart POST helper ─────────────────────────────────────────────
    function cartPost(url, bodyStr, successMsg) {
        return fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': csrfToken()
            },
            body: bodyStr
        })
        .then(function (r) {
            if (r.redirected && r.url.indexOf('/auth/login') !== -1) {
                window.location.href = r.url;
                return null;
            }
            return r.ok ? r.json() : null;
        })
        .then(function (data) {
            if (!data) return;
            showToast(data.success, data.message || (data.success ? successMsg : 'Có lỗi xảy ra.'));
            if (data.success && data.cart) applyCart(data.cart);
        })
        .catch(function () {
            showToast(false, 'Có lỗi xảy ra. Vui lòng thử lại.');
        });
    }

    // ── Event delegation ─────────────────────────────────────────────────────
    document.addEventListener('click', function (e) {

        // Decrease quantity
        var minusBtn = e.target.closest('.qty-minus');
        if (minusBtn) {
            var itemId = minusBtn.dataset.itemId;
            var row    = document.querySelector('tr[data-item-id="' + itemId + '"]');
            var input  = row && row.querySelector('.qty-input');
            if (!input) return;
            var newQty = Math.max(1, parseInt(input.value, 10) - 1);
            input.value = newQty;
            cartPost('/Customers/Cart/Update',
                'cartItemId=' + encodeURIComponent(itemId) + '&quantity=' + newQty,
                'Đã cập nhật giỏ hàng.');
            return;
        }

        // Increase quantity
        var plusBtn = e.target.closest('.qty-plus');
        if (plusBtn) {
            var itemId = plusBtn.dataset.itemId;
            var stock  = parseInt(plusBtn.dataset.stock || '9999', 10);
            var row    = document.querySelector('tr[data-item-id="' + itemId + '"]');
            var input  = row && row.querySelector('.qty-input');
            if (!input) return;
            var newQty = Math.min(stock, parseInt(input.value, 10) + 1);
            input.value = newQty;
            cartPost('/Customers/Cart/Update',
                'cartItemId=' + encodeURIComponent(itemId) + '&quantity=' + newQty,
                'Đã cập nhật giỏ hàng.');
            return;
        }

        // Remove item
        var removeBtn = e.target.closest('.btn-remove-item');
        if (removeBtn) {
            var itemId = removeBtn.dataset.itemId;
            if (!confirm('Xóa sản phẩm này?')) return;
            cartPost('/Customers/Cart/Remove',
                'cartItemId=' + encodeURIComponent(itemId),
                'Đã xóa sản phẩm.');
            return;
        }

        // Clear all
        var clearBtn = e.target.closest('#btn-clear-cart');
        if (clearBtn) {
            if (!confirm('Xóa toàn bộ giỏ hàng?')) return;
            cartPost('/Customers/Cart/Clear', '', 'Đã xóa tất cả sản phẩm.');
            return;
        }
    });

}());
