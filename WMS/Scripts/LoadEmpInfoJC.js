$(document).ready(function () {
    $('#buttonId').click(function () {
        var empNo = document.getElementById("JobEmpNo").value;
        //var URL = '/WMS/LvApp/GetEmpInfo';
        var URL = '/PPAF/Emp/GetEmpInfo';
        $.getJSON(URL, { empNo: empNo }, function (data) {
            var values = data.split('@');
            document.getElementById("EName").value = values[0];
            document.getElementById("EDesignation").value = values[1];
            document.getElementById("ESection").value = values[2];
            document.getElementById("ECL").value = values[3];
            document.getElementById("EAL").value = values[4];
            document.getElementById("ESL").value = values[5];
            document.getElementById("EFName").value = values[7];
            document.getElementById("EDOB").value = values[8];
        });
    });
});