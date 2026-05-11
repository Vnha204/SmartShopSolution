(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var input = document.getElementById('thumbnailFileInput');
        if (!input) return;

        input.addEventListener('change', function () {
            var file   = this.files && this.files[0];
            var wrap   = document.getElementById('imgPreview');
            var img    = document.getElementById('previewImg');
            if (!wrap || !img) return;

            if (file) {
                var reader = new FileReader();
                reader.onload = function (e) {
                    img.src              = e.target.result;
                    wrap.style.display   = 'block';
                };
                reader.readAsDataURL(file);
            } else {
                wrap.style.display = 'none';
                img.src = '';
            }
        });
    });
})();
