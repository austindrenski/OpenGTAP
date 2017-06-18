"use strict";
const Electron = require("electron");
const App = Electron.app;
const BrowserWindow = Electron.BrowserWindow;
const Menu = Electron.Menu;
const Shell = Electron.shell;
const Dialog = Electron.dialog;
const GlobalShortcut = Electron.globalShortcut;
const Path = require("path");
const Url = require("url");

exports.OpenFile = OpenFile;

// Global reference to the main window object.
let MainWindow;
let BackgroundColor = "#FCFCFC";
App.showExitPrompt = true;

// Drag and drop file handling.
//process.argv.forEach(OnOpen);


App.on(
    "ready",
    function() {
        CreateMainWindow();
        GlobalShortcut.register("CmdOrCtrl+O", () => OpenFile());
        GlobalShortcut.register("CmdOrCtrl+Q", () => MainWindow.close());
        MainWindow.on("open-file", event => event.preventDefault());
        MainWindow.on("open-url", event => event.preventDefault());
    });

App.on(
    "window-all-closed",
    function() {
        if (process.platform !== "darwin") {
            App.quit();
        }
    });

App.on(
    "activate",
    function () {
        if (MainWindow === null) {
            CreateMainWindow();
        }
    });

function CreateMainWindow() {
    MainWindow =
        new BrowserWindow(
        {
            backgroundColor: BackgroundColor,
            icon: Path.join(__dirname, "build", "OpenGTAP.ico")
        });

    Menu.setApplicationMenu(Menu.buildFromTemplate(Template));
    
    MainWindow.loadURL(
        Url.format(
            {
                pathname: Path.join(__dirname, "index.html"),
                protocol: "file:",
                slashes: true
            }));
    
    MainWindow.on(
        "closed",
        function () {
            MainWindow = null;
        });

    MainWindow.on(
        "close",
        function (event) {
            if (App.showExitPrompt) {
                event.preventDefault();
                Dialog.showMessageBox(
                    {
                        type: "question",
                        buttons: ["Yes", "No"],
                        title: "Confirm",
                        message: "Are you sure you want to quit?"
                    },
                    function(response) {
                        if (response === 0) {
                            App.showExitPrompt = false;
                            MainWindow.close();
                        }
                    });
            }
        });
}

// Opens the file dialog.
function OpenFile() {
    const Files =
        Dialog.showOpenDialog(
            MainWindow,
            {
                properties: ["openFile"],
                filters: [ { name: "", extensions: ["har", "sl4", "harx"] } ]
            });

    if (!Files) {
        return;
    }

    const File = Files[0];

    console.log(File);
};

// Template for the menu bar.
const Template = [
    {
        label: "File",
        submenu: [
            { label: "Open", click() { OpenFile(); } },
            { role: "quit" }
        ]
    },
    {
        label: "Edit",
        submenu: [
            //{ role: "undo" },
            //{ role: "redo" },
            //{ type: "separator" },
            //{ role: "cut" },
            { role: "copy" }
            //{ role: "paste" },
            //{ role: "pasteandmatchstyle" },
            //{ role: "delete" },
            //{ role: "selectall" }
        ]
    },
    {
        label: "View",
        submenu: [
            { role: "reload" },
            //{ role: "forcereload" },
            { role: "toggledevtools" },
            { type: "separator" },
            { role: "resetzoom" },
            { role: "zoomin" },
            { role: "zoomout" },
            { type: "separator" },
            { role: "togglefullscreen" },
            { role: "minimize" }
        ]
    },
    {
        role: "help",
        submenu: [
            {
                label: "OpenGTAP",
                click() {
                    Shell.openExternal("https://austindrenski.github.io/OpenGTAP");
                }
            },
            {
                label: "Electron",
                click() {
                    Shell.openExternal("https://electron.atom.io");
                }
            }
        ]
    }
];
