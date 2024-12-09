// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

const {
    app,
    BrowserWindow,
    dialog,
    ipcMain
} = require("electron");
const path = require("path");
const fs = require("fs");
const FormData = require('form-data')
const https = require('https')
const fetch = require('node-fetch')

// based on answer from https://stackoverflow.com/questions/57807459/how-to-use-preload-js-properly-in-electron
// Keep a global reference of the window object, if you don't, the window will
// be closed automatically when the JavaScript object is garbage collected.
let win;

async function createWindow() {

    // Create the browser window.
    win = new BrowserWindow({
        width: 1200,
        height: 800,
        webPreferences: {
            nodeIntegration: false, // is default value after Electron v5
            contextIsolation: true, // protect against prototype pollution
            enableRemoteModule: true, // turn off remote
            preload: path.join(__dirname, "preload.js") // use a preload script
        }
    });

    // Load app
    win.loadFile(path.join(__dirname, "index.html"));

    // Used to allow self signed certificates
    // CodeQL [SM03616]: Suppress warning for disabling certificate validation
    const httpsAgent = new https.Agent({ rejectUnauthorized: false });

    // Set the maximum content length size in bytes and megabytes
    const maxSizeMegabytes = 2048;
    const maxSizeBytes = maxSizeMegabytes * 1024 * 1024;

    ipcMain.on("postFile", (event, args) => {
        let form = new FormData();

        let fullContentLength = 0;
        for (let file of args.files) {
            const { size } = fs.statSync(file);

            // Check to see if this particular file is too large
            if (size > maxSizeBytes) {
                win.webContents.send("errorEncountered", "The file '" + file + "' exceeds the maximum content size of " + maxSizeMegabytes + " MB.");
                return;
            }

            fullContentLength += size;
            form.append('file', fs.createReadStream(file), {
                contentType: "application/dicom"
            });
        }

        // See if the sum of all the files is too large
        if (fullContentLength > maxSizeBytes) {
            win.webContents.send("errorEncountered", "The total size of the request exceeds the maximum content size of " + maxSizeMegabytes + " MB.");
            return;
        }

        let authorizationHeader = ''
        if (args.bearerToken !== '') {
            authorizationHeader = 'Bearer ' + args.bearerToken
        }

        fetch(
            args.url, 
            {
                method: `POST`,
                headers: {
                    'Content-Type': 'multipart/related; ' + 'boundary=' + form._boundary,
                    'Accept': 'application/dicom+json',
                    'Authorization': authorizationHeader
                },
                maxContentLength: maxSizeBytes,
                maxBodyLength: maxSizeBytes,
                body: form,
                agent: httpsAgent
            })
            .then(function(response) {
                if (response.ok) {
                    win.webContents.send("success", response.data);
                } else {
                    win.webContents.send("httpErrorEncountered", response.status);
                }
            })
            .catch(function(error) {
                if (error.response === undefined) {
                    win.webContents.send("httpErrorEncountered", error.code);
                } else {
                    win.webContents.send("httpErrorEncountered", error.response.status);
                }
            })
    });

    ipcMain.on("getChangeFeed", (event, args) => {
        let form = new FormData();

        let authorizationHeader = ''
        if (args.bearerToken !== '') {
            authorizationHeader = 'Bearer ' + args.bearerToken
        }

        fetch(
            args.url, 
            {
                headers: {
                    'Accept': 'application/json',
                    'Authorization': authorizationHeader
                },
                agent: httpsAgent
            })
            .then(function(res) {
                return res.json();
            }).then(function(json) {
                win.webContents.send("changeFeedRetrieved", json);
            })
            .catch(function(error) {
                if (error.response === undefined) {
                    win.webContents.send("httpErrorEncountered", error.code);
                } else {
                    win.webContents.send("httpErrorEncountered", error.response.status);
                }
            })

    });

    ipcMain.on("selectFile", (event, args) => {
        dialog.showOpenDialog({
            filters: [
                { name: 'DICOM Files', extensions: ['dcm'] },
            ],
            properties: ['openFile', 'multiSelections']
        }).then(result => {
            win.webContents.send("fileSelected", result.filePaths);
        });
    })
}

app.on("ready", createWindow);

