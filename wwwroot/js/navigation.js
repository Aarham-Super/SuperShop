(() => {
    function initializeNavigation() {
        const toggle = document.getElementById("navbarMobileToggle");
        const menu = document.getElementById("navbarMenu");

        if (!toggle || !menu) {
            return;
        }

        const setMenuOpen = (isOpen) => {
            menu.classList.toggle("navbar-menu-open", isOpen);
            toggle.setAttribute("aria-expanded", String(isOpen));
            toggle.setAttribute(
                "aria-label",
                isOpen ? "Close navigation menu" : "Open navigation menu"
            );
        };

        toggle.addEventListener("click", () => {
            setMenuOpen(!menu.classList.contains("navbar-menu-open"));
        });

        menu.addEventListener("click", (event) => {
            if (event.target.closest("a, button")) {
                setMenuOpen(false);
            }
        });

        document.addEventListener("click", (event) => {
            if (!menu.contains(event.target) && !toggle.contains(event.target)) {
                setMenuOpen(false);
            }
        });

        document.addEventListener("keydown", (event) => {
            if (event.key === "Escape") {
                setMenuOpen(false);
                toggle.focus();
            }
        });

        window.matchMedia("(min-width: 901px)").addEventListener("change", () => {
            setMenuOpen(false);
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initializeNavigation);
    } else {
        initializeNavigation();
    }
})();
