(function () {
    function applyMutualExclusion(changedCheckbox) {
        var attrId = changedCheckbox.dataset.attributeId;
        var section = document.getElementById('attributeSection');
        if (!section || !attrId) return;

        var siblings = section.querySelectorAll(
            'input[type="checkbox"][data-attribute-id="' + attrId + '"]'
        );

        if (changedCheckbox.checked) {
            siblings.forEach(function (cb) {
                if (cb === changedCheckbox) return;
                cb.checked = false;
                cb.disabled = true;
                var wrap = cb.closest('.form-check');
                if (wrap) wrap.classList.add('attribute-value-disabled');
            });
        } else {
            siblings.forEach(function (cb) {
                cb.disabled = false;
                var wrap = cb.closest('.form-check');
                if (wrap) wrap.classList.remove('attribute-value-disabled');
            });
        }
    }

    function applyInitialState() {
        var section = document.getElementById('attributeSection');
        if (!section) return;

        var checked = section.querySelectorAll(
            'input[type="checkbox"][data-attribute-id]:checked'
        );
        checked.forEach(function (cb) {
            applyMutualExclusion(cb);
        });
    }

    window.initAttributeMutualExclusion = applyInitialState;

    document.addEventListener('DOMContentLoaded', function () {
        applyInitialState();

        var section = document.getElementById('attributeSection');
        if (!section) return;

        section.addEventListener('change', function (e) {
            var cb = e.target;
            if (cb.tagName !== 'INPUT' || cb.type !== 'checkbox' || !cb.dataset.attributeId) return;
            applyMutualExclusion(cb);
        });
    });
})();
