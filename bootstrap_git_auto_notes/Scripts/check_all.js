function checkCheckboxes(id, pID) {

    $('#' + pID).find(':checkbox').each(function () {

        jQuery(this).attr('checked', $('#' + id).is(':checked'));

    });

}


$('#master_checkbox').change(function (event) {
    console.log(event);
    $("#commit_log input:checkbox").each(function () {
        if (this.id != 'master_checkbox') {
            this.checked = $('#master_checkbox').prop('checked');
        }
    });
});


