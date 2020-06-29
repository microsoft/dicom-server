# DICOM Web Electron
This tool is an Electron application used to interact with the DICOM server. It currently has the ability to upload one or many `.dcm` files to a given server.

## Getting Started
To get started with the code you must first have Node and NPM installed. The installer is available at [https://nodejs.org/en/download/](https://nodejs.org/en/download/).

Once installed you may follow the steps below:

1. Run `npm install`
2. Run `npm start`

## Packaging
The application is packed with [`electron-builder`](https://www.electron.build/)

To package the application you can run the following command

```
electron-builder build --win
```