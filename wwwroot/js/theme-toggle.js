// Theme Toggle Script
(function () {
    // Initialize theme from localStorage or system preference
    function initializeTheme() {
        const savedTheme = localStorage.getItem('theme');
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        const theme = savedTheme || (prefersDark ? 'dark' : 'light');
        setTheme(theme);
    }

    // Set theme and persist preference
    window.setTheme = function (theme) {
        const root = document.documentElement;
        root.setAttribute('data-theme', theme);
        localStorage.setItem('theme', theme);
        updateThemeToggleButton(theme);
        updateSidebarTheme(theme);
    };

    // Toggle theme
    window.toggleTheme = function () {
        const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
        const newTheme = currentTheme === 'light' ? 'dark' : 'light';
        setTheme(newTheme);
    };

    // Update theme toggle button appearance
    function updateThemeToggleButton(theme) {
        const buttons = document.querySelectorAll('.theme-toggle-btn');
        buttons.forEach(button => {
            if (theme === 'dark') {
                button.innerHTML = '<i class="fas fa-sun"></i>';
                button.style.background = 'linear-gradient(135deg, #f59e0b 0%, #f97316 100%)';
                button.setAttribute('aria-label', 'Switch to light mode');
                button.title = 'Switch to light mode';
            } else {
                button.innerHTML = '<i class="fas fa-moon"></i>';
                button.style.background = 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)';
                button.setAttribute('aria-label', 'Switch to dark mode');
                button.title = 'Switch to dark mode';
            }
        });
    }

    // Update sidebar theme to match overall theme
    function updateSidebarTheme(theme) {
        const sidebar = document.querySelector('.sidebar');
        if (sidebar) {
            if (theme === 'dark') {
                sidebar.style.setProperty('--sidebar-bg', '#0f172a', 'important');
                sidebar.style.setProperty('--sidebar-color', '#cbd5e1', 'important');
                sidebar.style.setProperty('--sidebar-active-bg', '#3b82f6', 'important');
            } else {
                sidebar.style.setProperty('--sidebar-bg', '#f8f9fa', 'important');
                sidebar.style.setProperty('--sidebar-color', '#212529', 'important');
                sidebar.style.setProperty('--sidebar-active-bg', '#0d6efd', 'important');
            }
        }
    }

    // Initialize theme on page load
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeTheme);
    } else {
        initializeTheme();
    }

    // Listen for system theme preference changes
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
        const savedTheme = localStorage.getItem('theme');
        if (!savedTheme) {
            const newTheme = e.matches ? 'dark' : 'light';
            setTheme(newTheme);
        }
    });
})();
