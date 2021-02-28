$(document).ready(function () {
    $("#timeZone").html(getLocalTimeZone());
});

function getLocalTimeZone() {
    var datetime = new Date();
    var dateTimeString = datetime.toString();
    var timezone = dateTimeString.substring(dateTimeString.indexOf("(") - 1);
    return timezone;
}

function convertUtcToLocalDateTime(utcDateTime) {
    // This will turn UTC to local time
    var formattedLocalDateTime = kendo.toString(new Date(utcDateTime), "G");
    return new Date(formattedLocalDateTime);
}

