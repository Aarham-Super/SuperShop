/* ============================================================
   SUPERSHOP - GLOBAL SITE JAVASCRIPT
   ============================================================ */

"use strict";


/* ============================================================
   INITIALIZATION
   ============================================================ */

document.addEventListener("DOMContentLoaded", function () {

    initializeDismissibleAlerts();

    initializeAutoHideMessages();

    initializeGenericConfirmations();

    initializeSmoothScroll();

    initializeBackToTop();

});


/* ============================================================
   DISMISSIBLE ALERTS
   ============================================================ */

function initializeDismissibleAlerts() {

    const alerts =
        document.querySelectorAll(
            ".ss-alert[data-auto-dismiss='true']"
        );


    alerts.forEach(function (alert) {

        const delay =
            parseInt(
                alert.dataset.dismissDelay || "5000",
                10
            );


        if (Number.isNaN(delay) || delay <= 0) {
            return;
        }


        window.setTimeout(function () {

            hideElement(alert);

        }, delay);

    });

}


/* ============================================================
   AUTO-HIDE SUCCESS MESSAGES
   ============================================================ */

function initializeAutoHideMessages() {

    const messages =
        document.querySelectorAll(
            ".admin-alert-success, " +
            ".account-alert-success"
        );


    messages.forEach(function (message) {

        window.setTimeout(function () {

            hideElement(message);

        }, 5000);

    });

}


/* ============================================================
   HIDE ELEMENT
   ============================================================ */

function hideElement(element) {

    if (!element) {
        return;
    }


    element.classList.add(
        "ss-message-fade"
    );


    window.setTimeout(function () {

        if (element.parentNode) {
            element.remove();
        }

    }, 350);

}


/* ============================================================
   GENERIC CONFIRMATIONS
   ============================================================ */

function initializeGenericConfirmations() {

    const confirmationElements =
        document.querySelectorAll(
            "[data-confirm-message]"
        );


    confirmationElements.forEach(
        function (element) {

            element.addEventListener(
                "click",
                function (event) {

                    const message =
                        element.getAttribute(
                            "data-confirm-message"
                        );


                    if (!message) {
                        return;
                    }


                    if (!window.confirm(message)) {

                        event.preventDefault();

                    }

                }
            );

        }
    );

}


/* ============================================================
   GENERIC CONFIRM ACTION
   ============================================================ */

function confirmAction(message) {

    if (!message) {

        message =
            "Are you sure you want to continue?";

    }


    return window.confirm(message);

}


/* ============================================================
   DELETE CONFIRMATION
   ============================================================ */

function confirmDelete(itemName) {

    let message =
        "Are you sure you want to delete this item?";


    if (itemName) {

        message =
            "Are you sure you want to delete \"" +
            itemName +
            "\"?";

    }


    return window.confirm(message);

}


/* ============================================================
   SMOOTH SCROLL
   ============================================================ */

function initializeSmoothScroll() {

    const links =
        document.querySelectorAll(
            "[data-scroll-to]"
        );


    links.forEach(function (link) {

        link.addEventListener(
            "click",
            function (event) {

                const selector =
                    link.getAttribute(
                        "data-scroll-to"
                    );


                if (!selector) {
                    return;
                }


                const target =
                    document.querySelector(selector);


                if (!target) {
                    return;
                }


                event.preventDefault();


                target.scrollIntoView({
                    behavior: "smooth",
                    block: "start"
                });

            }
        );

    });

}


/* ============================================================
   BACK TO TOP
   ============================================================ */

function initializeBackToTop() {

    const button =
        document.querySelector(
            "[data-back-to-top]"
        );


    if (!button) {
        return;
    }


    function updateButton() {

        if (window.scrollY > 400) {

            button.classList.add(
                "back-to-top-visible"
            );

        }
        else {

            button.classList.remove(
                "back-to-top-visible"
            );

        }

    }


    window.addEventListener(
        "scroll",
        updateButton,
        { passive: true }
    );


    button.addEventListener(
        "click",
        function () {

            window.scrollTo({
                top: 0,
                behavior: "smooth"
            });

        }
    );


    updateButton();

}


/* ============================================================
   PAGE VISIBILITY
   ============================================================ */

document.addEventListener(
    "visibilitychange",
    function () {

        if (document.hidden) {
            return;
        }

        /*
         * Reserved for future lightweight page-refresh
         * or state-restoration functionality.
         */

    }
);