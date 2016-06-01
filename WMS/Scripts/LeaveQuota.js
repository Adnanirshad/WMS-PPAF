//$(document).ready(function () {

//    ////var URL = '/WMS/Emp/SectionList';
//    //var URL = '/Home/TestData';
//    //$.getJSON(URL, function (data) {
//    //    console.log(data);
//    //});


//});
//$(document).ready(function () {

//    $("#ByEmpDiv").show();
//    $("#ByEmpTypeDiv").hide();
//    $("input[name$='CreateLeaveCriteria']").click(function () {
//        var test = $(this).val();
//        if (test == "ByEmployee") {
//            $("#ByEmpTypeDiv").hide();
//            $("#ByEmpDiv").show();
//        }
//        if (test == "ByEmpType") {
//            $("#ByEmpDiv").hide();
//            $("#ByEmpTypeDiv").show();
//        }
//    });
//});EmpInformition

$('#byAll').change(function () {
    if ($(this).is(":checked")) {
        $("#ByEmpTypeDiv").hide();
        $("#ByEmpNoDiv").hide();
        $("#EmpInformition").hide();
        $("#radioValue").val("byAll");
    }
});
$('#byEmp').change(function () {
    if ($(this).is(":checked")) {
        $("#ByEmpNoDiv").show();
        $("#EmpInformition").show();
        $("#ByEmpTypeDiv").hide();
        $("#radioValue").val("byEmp");
        
    }
});
$('#byEmpType').change(function () {
    if ($(this).is(":checked")) {
        $("#ByEmpTypeDiv").show();
        $("#ByEmpNoDiv").hide();
        $("#EmpInformition").hide();
        $("#radioValue").val("byEmpType");
    }
});
$(document).ready(function () {
    $('#buttonId').click(function () {
        var empNo = document.getElementById("EmpNo").value;
        //var URL = '/WMS/LvApp/GetEmpInfo';
        var URL = '/Emp/GetEmpInfo';
        $.getJSON(URL, { empNo: empNo }, function (data) {
            var values = data.split('@');
            document.getElementById("EName").value = values[0];
            document.getElementById("EDesignation").value = values[1];
            document.getElementById("ESection").value = values[2];
            document.getElementById("EFName").value = values[7];
            
        });
    });
});