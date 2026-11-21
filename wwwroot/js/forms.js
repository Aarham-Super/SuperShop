/* ============================================================
   SUPERSHOP - FORM JAVASCRIPT
   ============================================================ */

"use strict";


/* ============================================================
   INITIALIZATION
   ============================================================ */

document.addEventListener("DOMContentLoaded", function () {

    initializePasswordToggles();

    initializeQuantityControls();

    initializeSubmitProtection();

    initializeFormValidationHelpers();

});


/* ============================================================
   PASSWORD VISIBILITY
   ============================================================ */

function initializePasswordToggles() {

    const toggles =
        document.querySelectorAll(
            "[data-password-toggle]"
        );


    toggles.forEach(
        function (toggle) {

            const targetId =
                toggle.getAttribute(
                    "data-password-toggle"
                );


            if (!targetId) {
                return;
            }


            const passwordInput =
                document.getElementById(
                    targetId
                );


            if (!passwordInput) {
                return;
            }


            toggle.addEventListener(
                "click",
                function () {

                    const showingPassword =
                        passwordInput.type ===
                        "password";


                    passwordInput.type =
                        showingPassword
                            ? "text"
                            : "password";


                    toggle.setAttribute(
                        "aria-label",
                        showingPassword
                            ? "Hide password"
                            : "Show password"
                    );


                    toggle.setAttribute(
                        "title",
                        showingPassword
                            ? "Hide password"
                            : "Show password"
                    );


                    if (
                        toggle.tagName ===
                        "BUTTON"
                    ) {

                        toggle.textContent =
                            showingPassword
                                ? "🙈"
                                : "👁️";

                    }

                }
            );

        }
    );

}


/* ============================================================
   QUANTITY CONTROLS
   ============================================================ */

function initializeQuantityControls() {

    const quantityGroups =
        document.querySelectorAll(
            "[data-quantity-control]"
        );


    quantityGroups.forEach(
        function (group) {

            const input =
                group.querySelector(
                    "input[type='number']"
                );


            const decreaseButton =
                group.querySelector(
                    "[data-quantity-decrease]"
                );


            const increaseButton =
                group.querySelector(
                    "[data-quantity-increase]"
                );


            if (!input) {
                return;
            }


            if (decreaseButton) {

                decreaseButton.addEventListener(
                    "click",
                    function () {

                        changeQuantity(
                            input,
                            -1
                        );

                    }
                );

            }


            if (increaseButton) {

                increaseButton.addEventListener(
                    "click",
                    function () {

                        changeQuantity(
                            input,
                            1
                        );

                    }
                );

            }

        }
    );

}


/* ============================================================
   CHANGE QUANTITY
   ============================================================ */

function changeQuantity(
    input,
    amount
) {

    if (!input) {
        return;
    }


    let current =
        parseInt(
            input.value || "1",
            10
        );


    if (Number.isNaN(current)) {
        current = 1;
    }


    let minimum =
        parseInt(
            input.getAttribute("min") || "1",
            10
        );


    let maximum =
        parseInt(
            input.getAttribute("max") || "999",
            10
        );


    if (Number.isNaN(minimum)) {
        minimum = 1;
    }


    if (Number.isNaN(maximum)) {
        maximum = 999;
    }


    current += amount;


    current =
        Math.max(
            minimum,
            Math.min(
                current,
                maximum
            )
        );


    input.value =
        current.toString();


    input.dispatchEvent(
        new Event(
            "change",
            {
                bubbles: true
            }
        )
    );

}


/* ============================================================
   SUBMIT PROTECTION
   ============================================================ */

function initializeSubmitProtection() {

    const forms =
        document.querySelectorAll(
            "form[data-prevent-double-submit='true']"
        );


    forms.forEach(
        function (form) {

            form.addEventListener(
                "submit",
                function () {

                    if (
                        form.dataset.submitted ===
                        "true"
                    ) {
                        return;
                    }


                    form.dataset.submitted =
                        "true";


                    const submitButtons =
                        form.querySelectorAll(
                            "button[type='submit'], " +
                            "input[type='submit']"
                        );


                    submitButtons.forEach(
                        function (button) {

                            button.disabled =
                                true;


                            button.dataset.originalText =
                                button.tagName ===
                                "INPUT"
                                    ? button.value
                                    : button.textContent;


                            if (
                                button.tagName ===
                                "INPUT"
                            ) {

                                button.value =
                                    "Please wait...";

                            }
                            else {

                                button.textContent =
                                    "Please wait...";

                            }

                        }
                    );

                }
            );

        }
    );

}


/* ============================================================
   BASIC FORM VALIDATION HELPERS
   ============================================================ */

function initializeFormValidationHelpers() {

    const forms =
        document.querySelectorAll(
            "form"
        );


    forms.forEach(
        function (form) {

            const inputs =
                form.querySelectorAll(
                    "input, textarea, select"
                );


            inputs.forEach(
                function (input) {

                    input.addEventListener(
                        "input",
                        function () {

                            clearClientValidationState(
                                input
                            );

                        }
                    );


                    input.addEventListener(
                        "change",
                        function () {

                            clearClientValidationState(
                                input
                            );

                        }
                    );

                }
            );

        }
    );

}


/* ============================================================
   CLEAR CLIENT VALIDATION STATE
   ============================================================ */

function clearClientValidationState(
    input
) {

    if (!input) {
        return;
    }


    input.classList.remove(
        "input-validation-error"
    );


    input.removeAttribute(
        "aria-invalid"
    );

}


/* ============================================================
   FOCUS FIRST INVALID FIELD
   ============================================================ */

function focusFirstInvalidField() {

    const invalidField =
        document.querySelector(
            ".input-validation-error, " +
            "[aria-invalid='true']"
        );


    if (!invalidField) {
        return;
    }


    invalidField.focus();

}


/* ============================================================
   FORM RESET HELPER
   ============================================================ */

function resetFormState(form) {

    if (!form) {
        return;
    }


    form.dataset.submitted =
        "false";


    const submitButtons =
        form.querySelectorAll(
            "button[type='submit'], " +
            "input[type='submit']"
        );


    submitButtons.forEach(
        function (button) {

            button.disabled =
                false;


            const originalText =
                button.dataset.originalText;


            if (!originalText) {
                return;
            }


            if (
                button.tagName ===
                "INPUT"
            ) {

                button.value =
                    originalText;

            }
            else {

                button.textContent =
                    originalText;

            }

        }
    );

}


/* ============================================================
   ESCAPE KEY
   ============================================================ */

document.addEventListener(
    "keydown",
    function (event) {

        if (event.key !== "Escape") {
            return;
        }


        const activeElement =
            document.activeElement;


        if (
            activeElement &&
            (
                activeElement.tagName ===
                "INPUT" ||
                activeElement.tagName ===
                "TEXTAREA"
            )
        ) {
            return;
        }

    }
);
/* ============================================================
   PRODUCT IMAGE DRAG AND DROP
   ============================================================ */

function initializeProductImageDropzones() {
    document.querySelectorAll(".admin-upload-area").forEach(function (zone) {
        const input = zone.querySelector("input[type=\"file\"]");
        let status = zone.querySelector("[data-image-upload-status]");
        if (!input) return;
        if (!status) {
            status = document.createElement("p");
            status.className = "admin-upload-status";
            status.dataset.imageUploadStatus = "";
            status.setAttribute("role", "status");
            status.setAttribute("aria-live", "polite");
            zone.appendChild(status);
        }
        const instruction = zone.querySelector(".admin-upload-content span");
        if (instruction) instruction.textContent = "Choose images or drag and drop them here. JPG, JPEG, PNG or WebP — maximum 5 MB per image.";

        function updateStatus() {
            const files = Array.from(input.files || []);
            if (status) {
                status.textContent = files.length
                    ? files.length + " image" + (files.length === 1 ? "" : "s") + " selected: " + files.map(file => file.name).join(", ")
                    : "No images selected.";
            }
        }

        input.addEventListener("change", updateStatus);
        ["dragenter", "dragover"].forEach(function (eventName) {
            zone.addEventListener(eventName, function (event) {
                event.preventDefault();
                event.stopPropagation();
                zone.classList.add("is-dragging");
            });
        });
        ["dragleave", "dragend"].forEach(function (eventName) {
            zone.addEventListener(eventName, function (event) {
                if (eventName === "dragleave" && zone.contains(event.relatedTarget)) return;
                zone.classList.remove("is-dragging");
            });
        });
        zone.addEventListener("drop", function (event) {
            event.preventDefault();
            event.stopPropagation();
            zone.classList.remove("is-dragging");
            const files = Array.from(event.dataTransfer?.files || []);
            if (!files.length) return;
            const transfer = new DataTransfer();
            Array.from(input.files || []).concat(files).forEach(function (file) {
                if (!Array.from(transfer.files).some(existing => existing.name === file.name && existing.size === file.size)) {
                    transfer.items.add(file);
                }
            });
            input.files = transfer.files;
            input.dispatchEvent(new Event("change", { bubbles: true }));
        });
    });
}

document.addEventListener("DOMContentLoaded", initializeProductImageDropzones);





/* Removable image previews for product editing. */
function initializeRemovableProductImages() {
    document.querySelectorAll("[data-existing-image]").forEach(function (item) {
        const button = item.querySelector("[data-remove-existing-image]");
        const flag = item.querySelector("[data-remove-image-flag]");
        if (!button || !flag) return;
        button.addEventListener("click", function () {
            flag.disabled = false;
            item.classList.add("is-removed");
            item.setAttribute("aria-hidden", "true");
        });
    });

    document.querySelectorAll("[data-image-dropzone]").forEach(function (zone) {
        const input = zone.querySelector("input[type='file']");
        const list = zone.querySelector("[data-new-image-list]");
        const status = zone.querySelector("[data-image-upload-status]");
        if (!input || !list) return;

        function renderFiles() {
            list.querySelectorAll("img[data-object-url]").forEach(function (image) {
                URL.revokeObjectURL(image.src);
            });
            list.replaceChildren();
            const files = Array.from(input.files || []);
            files.forEach(function (file, index) {
                const item = document.createElement("div");
                item.className = "admin-image-item";
                const image = document.createElement("img");
                image.className = "admin-image-thumb";
                image.alt = file.name;
                image.src = URL.createObjectURL(file);
                image.dataset.objectUrl = "";
                const name = document.createElement("span");
                name.className = "admin-image-name";
                name.textContent = file.name;
                const remove = document.createElement("button");
                remove.type = "button";
                remove.className = "admin-image-remove";
                remove.textContent = "×";
                remove.setAttribute("aria-label", "Remove " + file.name);
                remove.addEventListener("click", function () {
                    const transfer = new DataTransfer();
                    Array.from(input.files || []).forEach(function (currentFile, currentIndex) {
                        if (currentIndex !== index) transfer.items.add(currentFile);
                    });
                    input.files = transfer.files;
                    input.dispatchEvent(new Event("change", { bubbles: true }));
                });
                item.append(image, name, remove);
                list.appendChild(item);
            });
            if (status) status.textContent = files.length
                ? files.length + " new image" + (files.length === 1 ? "" : "s") + " selected."
                : "No new images selected.";
        }

        input.addEventListener("change", renderFiles);
        renderFiles();
    });
}

document.addEventListener("DOMContentLoaded", initializeRemovableProductImages);

/* Demo checkout clears card input before submitting; the field has no name and is never posted. */
document.addEventListener("submit", function (event) {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.matches("[data-demo-checkout]")) return;
    const cardNumber = form.querySelector("[data-demo-card-number]");
    if (cardNumber) cardNumber.value = "";
});

/* Demo payment fields are validated locally and are never submitted. */
document.addEventListener("DOMContentLoaded", function () {
    const form = document.querySelector("[data-demo-checkout]");
    if (!form) return;

    const card = form.querySelector("[data-demo-card-number]");
    const cvc = form.querySelector("[data-demo-cvc]");
    const holder = form.querySelector("[data-demo-cardholder]");
    const country = form.querySelector("[data-demo-country]");

    if (card) {
        card.addEventListener("input", function () {
            const digits = card.value.replace(/\D/g, "").slice(0, 19);
            card.value = digits.replace(/(\d{4})(?=\d)/g, "$1 ").trim();
            card.setCustomValidity(digits.length === 0 || (digits.length >= 12 && digits.length <= 19)
                ? ""
                : "Enter 12 to 19 digits.");
        });
    }

    if (cvc) {
        cvc.addEventListener("input", function () {
            cvc.value = cvc.value.replace(/\D/g, "").slice(0, 4);
            cvc.setCustomValidity(cvc.value.length === 0 || cvc.value.length >= 3
                ? ""
                : "Enter 3 or 4 digits.");
        });
    }

    form.addEventListener("submit", function (event) {
        const digits = card ? card.value.replace(/\D/g, "") : "";
        if (card) card.setCustomValidity(digits.length >= 12 && digits.length <= 19 ? "" : "Enter 12 to 19 digits.");
        if (cvc) cvc.setCustomValidity(/^\d{3,4}$/.test(cvc.value) ? "" : "Enter 3 or 4 digits.");
        if (!form.reportValidity()) {
            event.preventDefault();
            return;
        }
        if (card) card.value = "";
        if (cvc) cvc.value = "";
        if (holder) holder.value = "";
        if (country) country.value = "";
    });
});
