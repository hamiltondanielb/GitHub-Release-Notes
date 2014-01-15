$('#repo_table').on('click', 'tbody tr', function(event) {
    $(this).addClass('highlight').siblings().removeClass('highlight');
});

$(function setupDeselectEvent() {
    var selected = {};
    $('input[type="radio"]').on('click', function () {
        if (this.name in selected && this !== selected[this.name])
            $(selected[this.name]).trigger("deselect");
        selected[this.name] = this;
    }).filter(':checked').each(function () {
        selected[this.name] = this;
    });
});

$('input[type="radio"]').bind('click', function(e){
    // Processing only those that match the name attribute of the currently clicked button...
    if ($(this).parent().parent().parent().prop('checked') === 'false')
        $(this).parent().parent().parent().toggleClass("highlight");
    e.stopPropagation();
});

$('input[type="radio"]').bind('deselect', function (e) {
    if ($(this).parent().parent().parent().prop('checked') === 'true')
        $(this).parent().parent().parent().toggleClass("highlight");
    e.stopPropagation();

});

$('tr').click(function() {
    $(this).children().children().children().children().prop('checked', true).trigger('click');
});
