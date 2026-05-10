(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {

        document.querySelectorAll('.address-radio-item').forEach(function (item) {
            var radio = item.querySelector('input[type="radio"]');
            if (!radio) return;

            // phản ánh trạng thái ban đầu
            if (radio.checked) item.classList.add('selected');

            item.addEventListener('click', function () {
                // bỏ chọn tất cả
                document.querySelectorAll('.address-radio-item').forEach(function (i) {
                    i.classList.remove('selected');
                });
                radio.checked = true;
                item.classList.add('selected');

                if (radio.name === 'SelectedAddressId') {
                    setNewAddressVisible(false);
                }
            });
        });

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

        var toggleBtn   = document.getElementById('btnToggleNewAddress');
        var newAddrForm = document.getElementById('newAddressForm');
        var hiddenRadio = document.getElementById('radioNewAddress');

        function setNewAddressVisible(show) {
            if (!newAddrForm) return;
            newAddrForm.style.display = show ? '' : 'none';
            if (toggleBtn) toggleBtn.style.display = show ? 'none' : '';
            if (hiddenRadio) hiddenRadio.checked = show;

            // bỏ chọn địa chỉ cũ khi dùng địa chỉ mới
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

        if (newAddrForm) {
            var hasValues = newAddrForm.querySelector('input[value]:not([value=""])');
            var hasErrors = newAddrForm.querySelector('.text-danger:not(:empty)');
            if (hasValues || hasErrors) {
                setNewAddressVisible(true);
            }
        }
    });

}());
