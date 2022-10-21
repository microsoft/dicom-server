// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

$(() => {
    let files = []
    let displayServerEdit = false
    let serverAddressInput = $("#server-address-input")
    let bearerTokenInput = $('#bearer-token-input')
    let serverVersionInput = $("[name='version']")
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
    let selectedFilesSection = $('#selected-files-section')
    let serverSettingsMenu = $('#server-settings-menu')
    let serverSettingsSection = $('#server-settings-section')
    let serverAddressDisplay = $('#server-address-display')
    let changeFeedSection = $('#change-feed-section')
    let changeFeedMenu = $('#change-feed-menu')
    let changeFeedButton = $('#change-feed-button')
    let changeFeedTable = $('#change-feed-table')
    let changeFeedResults = $('#change-feed-results')
    let offsetInput = $('#offset-input')
    let partitionContainer = $('#partition-container')
    let partitionInput = $('#partitions-input')
    let partitionNameInput = $('#partition-name-input')
    let offset = 0;

    let isPartitionEnabled = () => {
        return partitionInput[0].checked
    }

    let baseUrl = () => {
        var version = stripSpecialCharacters($("[name='version']:checked").val())
        return stripSpecialCharacters(serverAddressInput.val()) + "/" + version
    }

    let partitionUrl = () => {
        return 'partitions/' + stripSpecialCharacters($('#partition-name-input').val())
    }

    let getPostUrl = () => {
        if (isPartitionEnabled()) {
            return baseUrl() + "/" + partitionUrl() + "/studies"
        } else {
            return baseUrl() + "/studies"
        }
    }

    let stripSpecialCharacters = (str) => {
        return str.replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/'/g, '&#39;')
            .replace(/"/g, '&#34;')
    }

    serverAddressDisplay.html(getPostUrl())

    let hideErrorSuccess = function() {
        errorDisplay.toggle(false)
        successDisplay.toggle(false)
    }

    partitionInput.on("change",(value) => {
        let checked = value.target.checked;
        serverAddressDisplay.html(getPostUrl())
        if (checked) {
            partitionContainer.show()
        } else {
            partitionContainer.hide()
        }
    })

    fileUploadMenu.on("click",() => {
        fileUploadSection.toggle(true)
        selectedFilesSection.toggle(true)
        serverSettingsSection.toggle(false)
        changeFeedSection.toggle(false)
        fileUploadMenu.toggleClass('is-active', true)
        serverSettingsMenu.toggleClass('is-active', false)
        changeFeedMenu.toggleClass('is-active', false)

        hideErrorSuccess()
    })

    serverSettingsMenu.on("click",() => {
        fileUploadSection.toggle(false)
        selectedFilesSection.toggle(false)
        serverSettingsSection.toggle(true)
        changeFeedSection.toggle(false)
        fileUploadMenu.toggleClass('is-active', false)
        serverSettingsMenu.toggleClass('is-active', true)
        changeFeedMenu.toggleClass('is-active', false)

        hideErrorSuccess()
    })

    changeFeedMenu.on("click",() => {
        fileUploadSection.toggle(false)
        selectedFilesSection.toggle(false)
        serverSettingsSection.toggle(false)
        changeFeedSection.toggle(true)
        fileUploadMenu.toggleClass('is-active', false)
        serverSettingsMenu.toggleClass('is-active', false)
        changeFeedMenu.toggleClass('is-active', true)

        hideErrorSuccess()
    })

    serverAddressButton.on("click",() => {
        if (displayServerEdit) {
            serverAddressDisplay.html(getPostUrl())
            serverAddressInput.prop('disabled', true);
            serverVersionInput.prop('disabled', true);
            bearerTokenInput.prop('disabled', true)
            serverAddressButton.val("Change")
            serverAddressButton.toggleClass("is-primary", false)
            partitionNameInput.prop('disabled', true)
        } else {
            serverAddressButton.val("Update")
            serverAddressButton.toggleClass("is-primary", true)
            serverAddressInput.prop('disabled', false);
            serverVersionInput.prop('disabled', false);
            bearerTokenInput.prop('disabled', false)
            partitionNameInput.prop('disabled', false)
        }
        displayServerEdit = !displayServerEdit
    })

    serverVersionInput.on("change", () => {
        serverAddressDisplay.html(getPostUrl())
    })

    selectFileButton.on("click", () => {
        window.api.send("selectFile");
    })

    postFileButton.on("click",() => {
        let url = getPostUrl()
        let bearerToken = bearerTokenInput.val()

        hideErrorSuccess()

        window.api.send("postFile", { url, bearerToken, files });
    })

    window.api.receive("httpErrorEncountered", (status) => {
        errorDisplay.toggle()
        errorMessage.html('Status code returned: ' + status)
    });

    window.api.receive("errorEncountered", (message) => {
        errorDisplay.toggle()
        errorMessage.html(message)
    });

    window.api.receive("success", (data) => {
        successDisplay.toggle()
    });

    window.api.receive("fileSelected", (data) => {
        files = data
        let html = ''
        for (let file of files) {
            html += "<tr><td>" + file + "</td></tr>"
        }
        fileNameTable.html(html)

        if (html !== '') {
            fileDisplay.toggle(true)
            postFileButton.prop('disabled', false);
            postFileButton.toggleClass("is-primary", true)
        } else {
            fileDisplay.toggle(false)
            postFileButton.prop('disabled', true);
            postFileButton.toggleClass("is-primary", false)
        }

        hideErrorSuccess()
    })

    changeFeedButton.on("click", () => {
        errorDisplay.toggle(false)

        offset = offsetInput.val()

        let url = baseUrl() + "/changefeed?includemetadata=false&offset=" + offset
        let bearerToken = bearerTokenInput.val()

        hideErrorSuccess()

        window.api.send("getChangeFeed", { url, bearerToken });
    })

    window.api.receive("changeFeedRetrieved", (data) => {

        console.log(data)
        let html = ''
        if (!Array.isArray(data) || !data.length) {
            html = "<p>No results</p>"
        } else {
            for (let [i, item] of data.entries()) {
                let backgroundClass = ''
                if (i % 2 == 0) {
                    backgroundClass = 'has-background-white-ter'
                }
                html += "<div class='card " + backgroundClass + "'><div class='card-content'><div class='columns'><div class='column is-1'><h1>" + item.sequence + "</h1></div><div class='column'><div class='level'><div class='level-left'><div class='level-item'>" + item.action + "</div><div class='level-item'>" + item.state + "</div></div><div class='level-right'><div class='level-item'>" + item.timestamp + "</div></div></div><p>StudyInstanceUid: " + item.studyInstanceUid + "<br />SeriesInstanceUid: " + item.seriesInstanceUid + "<br />SopInstanceUid: " + item.sopInstanceUid + "</p></div></div></div></div>"
                offset = item.sequence
            }
        }

        offsetInput.val(offset)
        changeFeedTable.html(html)
        changeFeedResults.toggle(true)
    })
})