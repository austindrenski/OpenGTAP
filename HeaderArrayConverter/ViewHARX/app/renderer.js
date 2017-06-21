"use strict";

exports.SetCallback = SetCallback;
exports.SetDragAndDrop = SetDragAndDrop;


function SetCallback(google, elementId) {
    google.charts.load("current", { "packages": ["table"] });

    google.charts.setOnLoadCallback(() => DrawTable(google, elementId));
}

function DrawTable(google, elementId) {
    const Data = new google.visualization.DataTable();
    Data.addColumn("string", "Name");
    Data.addColumn("number", "Salary");
    Data.addColumn("boolean", "Full Time Employee");
    Data.addRows([
        ["Mike", { v: 10000, f: "$10,000" }, true],
        ["Jim", { v: 8000, f: "$8,000" }, false],
        ["Alice", { v: 12500, f: "$12,500" }, true],
        ["Bob", { v: 7000, f: "$7,000" }, true]
    ]);

    const Table = new google.visualization.Table(document.getElementById(elementId));

    Table.draw(Data, { showRowNumber: true, width: "100%", height: "100%" });
}

function SetDragAndDrop() {
    document.addEventListener("dragover", event => event.preventDefault());
    document.addEventListener("drop", event => event.preventDefault());
}