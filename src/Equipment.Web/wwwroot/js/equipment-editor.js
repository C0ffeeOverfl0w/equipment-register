import { ApiError, requestJson } from "./api.js";

const { $, DevExpress } = window;
const fields = ["inventoryNumber", "equipmentTypeId", "name", "room"];

export function createEquipmentEditor({ onSaved }) {
    let form, formHost, errorElement, statusElement, reloadElement, reloadButton, saveButton, cancelButton;
    let itemId = null, originalVersion = null, loading = false, saving = false, closingAfterSave = false, loadSequence = 0, loadController;
    const serverInvalidFields = new Set();

    function updateButtons() {
        form?.option("readOnly", saving);
        saveButton?.option("disabled", loading || saving || !form);
        saveButton?.option("text", saving ? "Сохранение…" : "Сохранить");
        cancelButton?.option("disabled", saving);
        popup.option("showCloseButton", !saving);
    }

    function showError(message) {
        errorElement.textContent = message;
        errorElement.hidden = !message;
    }

    function applyServerErrors(errors) {
        for (const [key, messages] of Object.entries(errors)) {
            const field = fields.find(candidate => candidate.toLowerCase() === key.replace(/^\$\./, "").toLowerCase());
            if (field && Array.isArray(messages)) {
                form.getEditor(field)?.option({ validationStatus: "invalid", validationErrors: messages.map(message => ({ message })) });
                serverInvalidFields.add(field);
            }
        }
    }

    const popup = $("<div>").appendTo(document.body).dxPopup({
        title: "Карточка техники",
        width: "calc(100vw - 24px)",
        maxWidth: 560,
        height: "auto",
        maxHeight: "90vh",
        showTitle: true,
        showCloseButton: true,
        dragEnabled: false,
        hideOnOutsideClick: false,
        contentTemplate(container) {
            const content = document.createElement("div");
            content.className = "equipment-editor";
            statusElement = document.createElement("p");
            statusElement.className = "editor-status";
            errorElement = document.createElement("div");
            errorElement.className = "editor-error";
            errorElement.setAttribute("role", "alert");
            errorElement.hidden = true;
            reloadElement = document.createElement("div");
            reloadElement.className = "editor-reload";
            reloadElement.hidden = true;
            formHost = document.createElement("div");
            content.append(statusElement, errorElement, reloadElement, formHost);
            $(container).append(content);
            reloadButton = $(reloadElement).dxButton({
                text: "Перечитать карточку",
                stylingMode: "outlined",
                onClick: () => open(itemId)
            }).dxButton("instance");
        },
        toolbarItems: [
            {
                widget: "dxButton", toolbar: "bottom", location: "before",
                options: {
                    text: "Отмена", stylingMode: "text",
                    onInitialized: event => { cancelButton = event.component; },
                    onClick: () => popup.hide()
                }
            },
            {
                widget: "dxButton", toolbar: "bottom", location: "after",
                options: {
                    text: "Сохранить", type: "default", stylingMode: "contained", disabled: true,
                    onInitialized: event => { saveButton = event.component; },
                    onClick: save
                }
            }
        ],
        onHiding(event) {
            if (saving && !closingAfterSave) {
                event.cancel = true;
                return;
            }

            loadSequence++;
            loadController?.abort();
        },
        onHidden() {
            form?.dispose();
            form = null;
            formHost?.replaceChildren();
            serverInvalidFields.clear();
        }
    }).dxPopup("instance");

    async function open(id = null) {
        if (saving) return;
        const sequence = ++loadSequence;
        loadController?.abort();
        loadController = new AbortController();
        itemId = id;
        originalVersion = null;
        loading = true;
        form?.dispose();
        form = null;
        formHost?.replaceChildren();
        serverInvalidFields.clear();
        popup.option("title", id === null ? "Добавление техники" : "Редактирование техники");
        await popup.show();
        if (sequence !== loadSequence) return;
        showError("");
        reloadElement.hidden = true;
        statusElement.textContent = "Загрузка карточки…";
        statusElement.hidden = false;
        updateButtons();

        try {
            const [types, card] = await Promise.all([
                requestJson("/api/equipment-types", { signal: loadController.signal }),
                id === null ? Promise.resolve(null) : requestJson(`/api/equipment/${id}`, { signal: loadController.signal })
            ]);
            if (sequence !== loadSequence) return;
            originalVersion = card?.version ?? null;
            const data = {
                inventoryNumber: card?.inventoryNumber ?? "",
                equipmentTypeId: card?.equipmentTypeId ?? null,
                name: card?.name ?? "",
                room: card?.room ?? null
            };
            form = $(formHost).dxForm({
                formData: data,
                labelLocation: "top",
                colCount: 1,
                showValidationSummary: true,
                onFieldDataChanged(event) {
                    if (serverInvalidFields.delete(event.dataField)) {
                        form.getEditor(event.dataField)?.option({ validationStatus: "valid", validationErrors: null });
                    }
                },
                items: [
                    {
                        dataField: "inventoryNumber", label: { text: "Учётный номер" },
                        editorOptions: { maxLength: 32, valueChangeEvent: "input change", inputAttr: { "aria-label": "Учётный номер", autocomplete: "off", spellcheck: "false" } },
                        validationRules: [
                            { type: "required", message: "Укажите учётный номер." },
                            { type: "pattern", pattern: /^[A-Za-z0-9]{1,32}$/, message: "От 1 до 32 латинских букв и цифр." }
                        ]
                    },
                    {
                        dataField: "equipmentTypeId", label: { text: "Тип техники" }, editorType: "dxSelectBox",
                        editorOptions: { dataSource: types, valueExpr: "id", displayExpr: "name", searchEnabled: true, inputAttr: { "aria-label": "Тип техники" } },
                        validationRules: [{ type: "required", message: "Выберите тип техники." }]
                    },
                    {
                        dataField: "name", label: { text: "Наименование" }, editorType: "dxTextArea",
                        editorOptions: { height: 84, valueChangeEvent: "input change", inputAttr: { "aria-label": "Наименование" } },
                        validationRules: [
                            { type: "required", message: "Укажите наименование." },
                            { type: "custom", validationCallback: event => typeof event.value === "string" && [...event.value].length <= 256 && !event.value.includes("\0"), message: "Допустимо не более 256 символов без нулевого символа." }
                        ]
                    },
                    {
                        dataField: "room", label: { text: "Комната размещения" }, editorType: "dxNumberBox",
                        editorOptions: { min: 1, max: 1000, step: 1, valueChangeEvent: "input change", showSpinButtons: true, inputAttr: { "aria-label": "Комната размещения" } },
                        validationRules: [
                            { type: "required", message: "Укажите комнату размещения." },
                            { type: "range", min: 1, max: 1000, message: "Комната должна быть от 1 до 1000." },
                            { type: "custom", validationCallback: event => Number.isInteger(event.value), message: "Укажите целое число." }
                        ]
                    }
                ]
            }).dxForm("instance");
            statusElement.hidden = true;
        } catch (error) {
            if (sequence !== loadSequence || error.name === "AbortError") return;
            showError(error instanceof ApiError && error.code === "equipment_not_found" ? "Карточка больше не существует." : "Не удалось загрузить карточку. Повторите загрузку.");
            statusElement.hidden = true;
            reloadButton.option("text", "Повторить загрузку");
            reloadElement.hidden = false;
        } finally {
            if (sequence === loadSequence) {
                loading = false;
                updateButtons();
            }
        }
    }

    async function save() {
        if (saving || loading || !form || !form.validate().isValid) return;
        saving = true;
        showError("");
        reloadElement.hidden = true;
        updateButtons();
        const data = form.option("formData");
        const body = {
            inventoryNumber: data.inventoryNumber,
            equipmentTypeId: data.equipmentTypeId,
            name: data.name,
            room: data.room,
            ...(itemId === null ? {} : { version: originalVersion })
        };
        let card;
        const creating = itemId === null;
        try {
            card = await requestJson(creating ? "/api/equipment" : `/api/equipment/${itemId}`, {
                method: creating ? "POST" : "PUT", body
            });
        } catch (error) {
            if (error instanceof ApiError && error.status < 500) {
                showError(error.message);
                applyServerErrors(error.errors);
                if (error.code === "equipment_version_conflict") {
                    showError("Карточка изменена другим пользователем. Перечитывание заменит введённые значения актуальными.");
                    reloadButton.option("text", "Перечитать карточку");
                    reloadElement.hidden = false;
                }
            } else {
                showError("Не удалось получить подтверждение сохранения. Проверьте список перед повторной отправкой; введённые данные сохранены в форме.");
            }
        } finally {
            if (!card) {
                saving = false;
                updateButtons();
            }
        }

        if (card) {
            closingAfterSave = true;
            try { await popup.hide(); }
            finally {
                closingAfterSave = false;
                saving = false;
                updateButtons();
            }
            await onSaved(card, creating);
        }
    }

    return { openCreate: () => open(null), openEdit: id => open(id) };
}
