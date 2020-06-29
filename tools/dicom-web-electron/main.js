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
const axios = require('axios').default;
const FormData = require('form-data')
const https = require('https')

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
    const httpsAgent = new https.Agent({ rejectUnauthorized: false });

    ipcMain.on("postFile", (event, args) => {
        let form = new FormData();

        for (let file of args.files) {
            form.append('file', fs.createReadStream(file), {
                contentType: "application/dicom"
            });
        }

        let authorizationHeader = ''
        if (args.bearerToken !== '') {
            authorizationHeader = 'Bearer ' + args.bearerToken
        }

        axios.post(args.url, form, {
                headers: {
                    'Content-Type': 'multipart/related; ' + 'boundary=' + form._boundary,
                    'Accept': 'application/dicom+json',
                    'Authorization': authorizationHeader
                },
                httpsAgent: httpsAgent
            })
            .then(function(response) {
                win.webContents.send("success", response.data);
            })
            .catch(function(error) {
                if (error.response === undefined) {
                    win.webContents.send("errorEncountered", error.code);
                } else {
                    win.webContents.send("errorEncountered", error.response.status);
                }
            })

    });

    ipcMain.on("getChangeFeed", (event, args) => {
        let form = new FormData();

        let authorizationHeader = ''
        if (args.bearerToken !== '') {
            authorizationHeader = 'Bearer ' + args.bearerToken
        }

        axios.get(args.url, {
                headers: {
                    'Accept': 'application/json',
                    'Authorization': authorizationHeader
                },
                httpsAgent: httpsAgent
            })
            .then(function(response) {
                win.webContents.send("changeFeedRetrieved", response.data);
            })
            .catch(function(error) {
                console.log(error)
                if (error.response === undefined) {
                    win.webContents.send("errorEncountered", error.code);
                } else {
                    win.webContents.send("errorEncountered", error.response.status);
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