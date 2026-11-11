import { requestJson } from "./api.js";
import { createEquipmentEditor } from "./equipment-editor.js";

const { $, DevExpress } = window;
DevExpress.localization.locale("ru");
const errorElement = document.getElementById("load-error");
const countElement = document.getElementById("record-count");
const errorMessage = "Не удалось загрузить список. Проверьте подключение и нажмите «Обновить».";
let latestLoad = 0;

function showError(message) {
    errorElement.textContent = message;
    errorElement.hidden = !message;
}

const store = new DevExpress.data.CustomStore({
    key: "id",
    loadMode: "processed",
    async load(options) {
        const load = ++latestLoad;
        const sort = options.sort?.[0];
        const parameters = new URLSearchParams({
            skip: String(options.skip ?? 0), take: String(options.take ?? 20),
            sortBy: sort?.selector ?? "inventoryNumber", sortDirection: sort?.desc ? "desc" : "asc"
        });
        try {
            const page = await requestJson(`/api/equipment?${parameters}`);
            if (load === latestLoad) {
                showError("");
                countElement.textContent = `Всего: ${page.totalCount}`;
            }
            return { data: page.items, totalCount: page.totalCount };
        } catch (error) {
            if (load === latestLoad) showError(errorMessage);
            throw new Error(errorMessage, { cause: error });
        }
    }
});

let grid;
const editor = createEquipmentEditor({
    async onSaved(_card, creating) {
        DevExpress.ui.notify(creating ? "Карточка создана" : "Изменения сохранены", "success", 2200);
        try {
            await grid.refresh();
        } catch {
            showError("Карточка сохранена, но список обновить не удалось. Нажмите «Обновить».");
        }
    }
});

grid = $("#equipment-grid").dxDataGrid({
    dataSource: store,
    remoteOperations: { paging: true, sorting: true },
    height: "min(650px, 70vh)",
    showBorders: false,
    showColumnLines: false,
    showRowLines: true,
    hoverStateEnabled: true,
    columnAutoWidth: true,
    wordWrapEnabled: true,
    noDataText: "Техника пока не добавлена",
    loadPanel: { enabled: true, text: "Загрузка…" },
    sorting: { mode: "single" },
    paging: { pageSize: 20 },
    pager: { visible: true, allowedPageSizes: [20, 50, 100], showPageSizeSelector: true, showInfo: true, showNavigationButtons: true },
    onCellPrepared(event) {
        if (event.rowType === "data" && event.column.type === "buttons") {
            $(event.cellElement).find("a").attr("aria-label", `Изменить карточку ${event.data.inventoryNumber}`);
        }
    },
    columns: [
        { dataField: "inventoryNumber", caption: "Учётный номер", minWidth: 170, sortOrder: "asc", encodeHtml: true },
        { dataField: "equipmentTypeName", caption: "Тип техники", minWidth: 170, encodeHtml: true },
        { dataField: "name", caption: "Наименование", minWidth: 250, encodeHtml: true },
        { dataField: "room", caption: "Комната размещения", dataType: "number", minWidth: 170, alignment: "left" },
        {
            type: "buttons", width: 56, fixed: true, fixedPosition: "right",
            buttons: [{ text: "Изменить", hint: "Изменить карточку", icon: "edit", onClick: event => editor.openEdit(event.row.data.id) }]
        }
    ]
}).dxDataGrid("instance");

$("#add-button").dxButton({ text: "Добавить", icon: "add", type: "default", stylingMode: "contained", onClick: editor.openCreate });
$("#refresh-button").dxButton({
    text: "Обновить", icon: "refresh", stylingMode: "outlined",
    async onClick() {
        try {
            await grid.refresh();
        } catch {
        }
    }
});
