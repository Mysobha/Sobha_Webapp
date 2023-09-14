$(document).ready(function ()
{
    $(".content").hide();
});

$('.toggle').on('click', function () {
    

    $(".content").toggle("slide");

});