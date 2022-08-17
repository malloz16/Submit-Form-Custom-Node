var processFields = [];
$(function () {

	// Store all Process Field values.
	processFields = getAllProcessFieldNames();

	// Field Mapping tab event binding
	$("#addRowButton").click(duplicateListSettingRow);
	var allListElements = document.querySelectorAll("[repeat-list]");
	$(allListElements).find("#removeRowButton").click(removeParentListSettingRow);
});

function duplicateListSettingRow() {

	// Get all the repeated elements
	var allListElements = document.querySelectorAll("[repeat-list]");
	var lastListElement = allListElements[allListElements.length - 1];
	// Create the clone
	lastListElement.parentNode.appendChild(lastListElement.cloneNode(true));

	// Get the new input elements and clear them.
	var newListElements = document.querySelectorAll("[repeat-list]");
	var newListElement = newListElements[newListElements.length - 1];
	$(newListElement).find("#processFields").val("");
	$(newListElement).find("#formKeys").val("");

	// Bind the remove event.
	$(newListElement).find("#removeRowButton").click(removeParentListSettingRow);

}

function getAllProcessFieldNames() {
	var allListElements = document.querySelectorAll("[repeat-list]");
	var firstListElement = allListElements[0];
	var options = $(firstListElement).find("#processFields option");

	var values = $.map(options, function (option) {
		if (option.value != '') {
			return option.value;
		}
	});

	return values;
}

function removeParentListSettingRow() {
	var allListElements = document.querySelectorAll("[repeat-list]");
	if (allListElements.length > 1) {
		var currentRowElement = this.parentNode.parentNode;
		currentRowElement.parentNode.removeChild(currentRowElement);
	}
}

window.onload = function () {
	var fileCheckBox = document.getElementById('checkbox');
	var fileRow = document.getElementById('fileRow');
	var attachmentField = document.getElementById('attachment');
	var showFileAttachment = function () {
		if (fileCheckBox.checked) {
			fileRow.style['display'] = 'block';
		} else {
			attachmentField.value = '';
			fileRow.style['display'] = 'none';
		}
	}
	fileCheckBox.onclick = showFileAttachment;
	showFileAttachment();
}