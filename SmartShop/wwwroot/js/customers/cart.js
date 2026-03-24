// Cart page — quantity stepper and auto-submit on change
(function () {
    'use strict';

    // Increment / decrement buttons submit the nearest update form
    document.querySelectorAll('.qty-stepper').forEach(function (stepper) {
        var input = stepper.querySelector('input[type="number"]');
        var form  = stepper.closest('form');
        var stock = parseInt(input.dataset.stock || '9999', 10);

        stepper.querySelector('.qty-minus').addEventListener('click', function () {
            var val = parseInt(input.value, 10) - 1;
            input.value = val < 1 ? 1 : val;
            form && form.submit();
        });

        stepper.querySelector('.qty-plus').addEventListener('click', function () {
            var val = parseInt(input.value, 10) + 1;
            input.value = val > stock ? stock : val;
            form && form.submit();
        });
    });
}());
