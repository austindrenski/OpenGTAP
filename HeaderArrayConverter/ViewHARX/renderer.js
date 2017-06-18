"use strict";

const Electron = require("electron");
const MainWindow = Electron.remote.require("./main");

exports.Assign = Assign;

function Assign(element) {
    element.addEventListener(
        "click",
        function() {
             MainWindow.OpenFile();
        });
}