// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

$(() => {
    let files = []
    let displayServerEdit = false
    let serverAddress = $("#server-address")
    let bearerTokenInput = $('#bearer-token-input')
    let serverAddressButton = $("#set-server-address")
    let selectFileButton = $("#select-file")
    let postFileButton = $("#post-file")
    let errorDisplay = $('#error-display')
    let errorMessage = $('#error-status')
    let successDisplay = $('#success-display')
    let fileNameTable = $('#file-name-table')
    let fileDisplay = $('#selected-files')
    let fileUploadMenu = $('#file-upload-menu')
    let fileUploadSection = $('#file-upload-section')
    let serverSettingsMenu = $('#server-settings-menu')
    let serverSettingsSection = $('#server-settings-section')
    let serverAddressDisplay = $('#server-address-display')
    let postUrl = serverAddress.val() + "/studies"

    serverAddressDisplay.html(postUrl)

    let hideErrorSuccess = function() {
        errorDisplay.toggle(false)
        successDisplay.toggle(false)
    }

    fileUploadMenu.click(() => {
        fileUploadSection.toggle(true)
        serverSettingsSection.toggle(false)
        fileUploadMenu.toggleClass('is-active', true)
        serverSettingsMenu.toggleClass('is-active', false)
    })

    serverSettingsMenu.click(() => {
        fileUploadSection.toggle(false)
        serverSettingsSection.toggle(true)
        fileUploadMenu.toggleClass('is-active', false)
        serverSettingsMenu.toggleClass('is-active', true)
    })

    serverAddressButton.click(() => {
        if (displayServerEdit) {
            postUrl = serverAddress.val() + "/studies"
            serverAddressDisplay.html(postUrl)
            serverAddress.prop('disabled', true);
            bearerTokenInput.prop('disabled', true)
            serverAddressButton.val("Change")
            serverAddressButton.toggleClass("is-primary", false)
        } else {
            serverAddressButton.val("Update")
            serverAddressButton.toggleClass("is-primary", true)
            serverAddress.prop('disabled', false);
            bearerTokenInput.prop('disabled', false)
        }
        displayServerEdit = !displayServerEdit
    })

    selectFileButton.click(() => {
        window.api.send("selectFile");
    })

    postFileButton.click(() => {
        let url = serverAddress.val() + "/studies"
        let bearerToken = bearerTokenInput.val()

        hideErrorSuccess()

        window.api.send("postFile", { url, bearerToken, files });
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