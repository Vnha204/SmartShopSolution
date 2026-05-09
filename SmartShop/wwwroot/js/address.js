$(function () {

    function reloadAddressesTab(callback) {
        var $tab = $('.account-tab[data-tab="addresses"]');
        if ($tab.length && window.AccountModule) {
            window.AccountModule.loadContent($tab.data('url'), 'addresses', callback);
        }
    }

    function showAddressSuccess(message) {
        $('#addresses-alerts').html(
            '<div class="alert alert-success alert-dismissible py-2 small">' +
            '<button type="button" class="btn-close" data-bs-dismiss="alert"></button>' +
            message +
            '</div>'
        );
    }

    function showAddressError(message) {
        $('#addresses-alerts').html(
            '<div class="alert alert-danger alert-dismissible py-2 small">' +
            '<button type="button" class="btn-close" data-bs-dismiss="alert"></button>' +
            message +
            '</div>'
        );
    }

    $(document).on('submit', '#addAddressForm', function (e) {
        e.preventDefault();
        var $form = $(this);
        var $btn = $('#saveAddressBtn');

        $btn.prop('disabled', true);

        $.ajax({
            url: '/Account/AddAddressAjax',
            type: 'POST',
            data: $form.serialize(),
            success: function (response) {
                $btn.prop('disabled', false);
                if (response.success) {
                    $form[0].reset();
                    if (response.isDefault) {
                        $('#addAddressModal').one('hidden.bs.modal', function () {
                            reloadAddressesTab(function () {
                                showAddressSuccess(response.message);
                            });
                        });
                    } else {
                        $('#addAddressModal').one('hidden.bs.modal', function () {
                            $.get('/Account/AddressCard', { id: response.addressId }, function (cardHtml) {
                                var $list = $('.addresses-list');
                                if ($list.length === 0) {
                                    $('.address-empty-state').remove();
                                    $('#addresses-alerts').after('<div class="addresses-list"></div>');
                                    $list = $('.addresses-list');
                                }
                                $list.append(cardHtml);
                                showAddressSuccess(response.message);
                            });
                        });
                    }
                    $('#addAddressModal').modal('hide');
                } else {
                    showAddressError(response.message || 'Có lỗi xảy ra!');
                }
            },
            error: function () {
                $btn.prop('disabled', false);
                showAddressError('Có lỗi xảy ra. Vui lòng thử lại!');
            }
        });
    });

    $(document).on('click', '.btn-set-default', function () {
        var $btn = $(this);
        if ($btn.hasClass('is-default')) return;

        var id = $btn.data('id');
        var token = $('input[name="__RequestVerificationToken"]').first().val();

        $.ajax({
            url: '/Account/SetDefaultAddress',
            type: 'POST',
            data: { id: id, __RequestVerificationToken: token },
            success: function (response) {
                if (response.success) {
                    $('.address-item')
                        .removeClass('border-primary address-card-default');
                    $('.address-default-badge')
                        .addClass('d-none');
                    $('.btn-set-default').removeClass('is-default')
                        .find('i').removeClass('fa-check-circle').addClass('fa-check-circle-o');

                    $btn.closest('.address-item')
                        .addClass('border-primary address-card-default');
                    $btn.closest('.address-item').find('.address-default-badge')
                        .removeClass('d-none');
                    $btn.addClass('is-default')
                        .find('i').removeClass('fa-check-circle-o').addClass('fa-check-circle');

                    showAddressSuccess(response.message);
                } else {
                    showAddressError(response.message || 'Có lỗi xảy ra, vui lòng thử lại.');
                }
            },
            error: function () {
                showAddressError('Có lỗi xảy ra. Vui lòng thử lại!');
            }
        });
    });

    $(document).on('click', '.delete-address-btn', function () {
        if (!confirm('Bạn có chắc muốn xóa địa chỉ này không?')) return;

        var id = $(this).data('id');
        var token = $('input[name="__RequestVerificationToken"]').first().val();

        $.ajax({
            url: '/Account/DeleteAddressAjax',
            type: 'POST',
            data: { id: id, __RequestVerificationToken: token },
            success: function (response) {
                if (response.success) {
                    reloadAddressesTab();
                } else {
                    alert(response.message);
                }
            },
            error: function () {
                alert('Có lỗi xảy ra. Vui lòng thử lại!');
            }
        });
    });

});
