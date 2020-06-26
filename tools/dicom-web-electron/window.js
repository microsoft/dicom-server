// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

$(() => {
    let files = []
    let displayServerEdit = false
    let serverAddress = $("#server-address")
    let serverAddressButton = $("#set-server-address")
    let selectFileButton = $("#select-file")
    let postFileButton = $("#post-file")
    let errorDisplay = $('#error-display')
    let errorMessage = $('#error-status')
    let successDisplay = $('#success-display')
    let fileNameTable = $('#file-name-table')
    let fileDisplay = $('#selected-files')

    let hideErrorSuccess = function() {
        errorDisplay.toggle(false)
        successDisplay.toggle(false)
    }

    serverAddressButton.click(() => {
        if (displayServerEdit) {
            serverAddress.prop('disabled', true);
            serverAddressButton.val("Change")
        } else {
            serverAddressButton.value = "Update"
            serverAddress.prop('disabled', false);
        }
        displayServerEdit = !displayServerEdit
    })

    selectFileButton.click(() => {
        window.api.send("selectFile");
    })

    postFileButton.click(() => {
        let url = serverAddress.val() + "/studies"

        hideErrorSuccess()

        window.api.send("postFile", { url, files });
    })

    window.api.receive("errorEncountered", (data) => {
        errorDisplay.toggle()
        errorMessage.html('Status code returned: ' + data)
    });

    window.api.receive("success", (data) => {
        successDisplay.toggle()
    });

    window.api.receive("fileSelected", (data) => {
        files = data
        var html = ''
        for (let file of files) {
            html += "<tr><td>" + file + "</td></tr>"
        }
        fileNameTable.html(html)

        if (html !== '') {
            fileDisplay.toggle(true)
            postFileButton.prop('disabled', false);
        } else {
            fileDisplay.toggle(false)
            postFileButton.prop('disabled', true);
        }

        hideErrorSuccess()
    })
})