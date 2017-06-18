"use strict";

const Electron = require("electron");
const App = Electron.app;
const BrowserWindow = Electron.BrowserWindow;
const Dialog = Electron.dialog;
const Path = require("path");
const Url = require("url");

exports.OpenFile = OpenFile;

// Global reference to the main window object.
let MainWindow;

// Initializes a BrowserWindow
function CreateMainWindow() {

    MainWindow = new BrowserWindow();

    MainWindow.loadURL(
        Url.format(
            {
                pathname: Path.join(__dirname, "index.html"),
                protocol: "file:",
                slashes: true
            }));

    // Open the DevTools.
    MainWindow.webContents.openDevTools();

    // Emitted when the window is closed.
    MainWindow.on(
        "closed",
        function() {
            MainWindow = null;
        });
}

// This method will be called when Electron has finished initialization and is ready to create browser windows.
App.on(
    "ready",
    CreateMainWindow);

// Quit when all windows are closed.
App.on(
    "window-all-closed",
    function() {
        // On OS X it is common for applications and their menu bar to stay active until the user quits explicitly with Cmd + Q
        if (process.platform !== "darwin") {
            App.quit();
        }
    });

App.on("activate", function () {
    // On OS X it's common to re-create a window in the app when the dock icon is clicked and there are no other windows open.
    if (MainWindow === null) {
        CreateMainWindow();
    }
});

// In this file you can include the rest of your app's specific main process code. You can also put them in separate files and require them here.

function OpenFile() {
    const Files =
        Dialog.showOpenDialog(
            MainWindow,
            {
                properties: ["openFile"],
                filters:
                [
                    {
                        name: "",
                        extensions: ["har", "sl4", "harx"]
                    }
                ]
            });

    if (!Files) {
        return;
    }

    const File = Files[0];

    console.log(File);
};