$(function () {

    var $digits = $('.otp-digit');
    var $hidden = $('#otpCode');

    function syncOtpCode() {
        var code = $digits.map(function () { return $(this).val(); }).get().join('');
        $hidden.val(code);
    }

    $digits.on('input', function () {
        var $self = $(this);
        var val = $self.val().replace(/\D/g, '').slice(-1);
        $self.val(val);
        syncOtpCode();
        if (val && $self.next('.otp-digit').length) {
            $self.next('.otp-digit').trigger('focus');
        }
    });

    $digits.on('keydown', function (e) {
        var $self = $(this);
        if (e.key === 'Backspace' && !$self.val() && $self.prev('.otp-digit').length) {
            $self.prev('.otp-digit').trigger('focus').val('');
            syncOtpCode();
        }
    });

    $digits.on('paste', function (e) {
        e.preventDefault();
        var pasted = (e.originalEvent.clipboardData || window.clipboardData)
            .getData('text').replace(/\D/g, '').slice(0, 6);
        $digits.each(function (i) {
            $(this).val(pasted[i] || '');
        });
        syncOtpCode();
        $digits.last().trigger('focus');
    });

    $(document).on('click', '[data-pw-toggle]', function () {
        var $btn = $(this);
        var $input = $('#' + $btn.data('pw-toggle'));
        var $icon = $btn.find('i');
        if ($input.attr('type') === 'password') {
            $input.attr('type', 'text');
            $icon.removeClass('fa-eye').addClass('fa-eye-slash');
        } else {
            $input.attr('type', 'password');
            $icon.removeClass('fa-eye-slash').addClass('fa-eye');
        }
    });

    $('.forgot-password-form').on('submit', function () {
        var $btn = $(this).find('[type=submit]');
        $btn.prop('disabled', true);
        var orig = $btn.html();
        $btn.html('<i class="fa fa-spinner fa-spin me-1"></i> Đang xử lý...');
        setTimeout(function () { $btn.prop('disabled', false).html(orig); }, 8000);
    });
});
