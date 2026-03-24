// Checkout page — address radio highlighting + new-address form toggle
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {

        // ── Address radio selection highlight ────────────────────────────────
        document.querySelectorAll('.address-radio-item').forEach(function (item) {
            var radio = item.querySelector('input[type="radio"]');
            if (!radio) return;

            // Reflect initial state
            if (radio.checked) item.classList.add('selected');

            item.addEventListener('click', function () {
                // Deselect all
                document.querySelectorAll('.address-radio-item').forEach(function (i) {
                    i.classList.remove('selected');
                });
                radio.checked = true;
                item.classList.add('selected');

                // If an existing address is selected, hide the new-address form
                if (radio.name === 'SelectedAddressId') {
                    setNewAddressVisible(false);
                }
            });
        });

        // ── Payment option highlight ─────────────────────────────────────────
        document.querySelectorAll('.payment-option').forEach(function (opt) {
            var radio = opt.querySelector('input[type="radio"]');
            if (!radio) return;
            if (radio.checked) opt.classList.add('selected');
            opt.addEventListener('click', function () {
                document.querySelectorAll('.payment-option').forEach(function (o) {
                    o.classList.remove('selected');
                });
                radio.checked = true;
                opt.classList.add('selected');
            });
        });

        // ── New address form toggle ──────────────────────────────────────────
        var toggleBtn   = document.getElementById('btnToggleNewAddress');
        var newAddrForm = document.getElementById('newAddressForm');
        var hiddenRadio = document.getElementById('radioNewAddress');

        function setNewAddressVisible(show) {
            if (!newAddrForm) return;
            newAddrForm.style.display = show ? '' : 'none';
            if (toggleBtn) toggleBtn.style.display = show ? 'none' : '';
            if (hiddenRadio) hiddenRadio.checked = show;

            // Clear SelectedAddressId selection when using new address
            if (show) {
                document.querySelectorAll('.address-radio-item').forEach(function (i) {
                    i.classList.remove('selected');
                    var r = i.querySelector('input[type="radio"]');
                    if (r && r.name === 'SelectedAddressId') r.checked = false;
                });
            }
        }

        if (toggleBtn) {
            toggleBtn.addEventListener('click', function () {
                setNewAddressVisible(true);
            });
        }

        // If form was shown due to validation error (fields have values), keep it open
        if (newAddrForm) {
            var hasValues = newAddrForm.querySelector('input[value]:not([value=""])');
            var hasErrors = newAddrForm.querySelector('.text-danger:not(:empty)');
            if (hasValues || hasErrors) {
                setNewAddressVisible(true);
            }
        }
    });

}());
