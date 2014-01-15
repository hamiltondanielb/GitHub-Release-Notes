DropDownTextToBox = function(objDropdown, strTextboxId) {
    document.getElementById(strTextboxId).value = objDropdown.options[objDropdown.selectedIndex].value;
    DropDownIndexClear(objDropdown.id);
    document.getElementById(strTextboxId).focus();
};

DropDownIndexClear = function(strDropdownId) {
    if (document.getElementById(strDropdownId) !== null) {
        document.getElementById(strDropdownId).selectedIndex = -1;
    }
};

$(DropDownIndexClear("ddl_FromRelease"));
$(DropDownIndexClear("ddl_ToRelease"));
