class AccessibilityWidget {
    constructor() {
        this.isOpen = false;
        this.settings = {
            fontSize: 100,
            highContrast: false,
            largeText: false,
            bigCursor: false,
            readingGuide: false,
            dyslexiaFont: false,
            grayscale: false,
            invertColors: false
        };

        this.init();
        this.loadSettings();
    }

    init() {
        this.bindEvents();
        this.setupReadingGuide();
        this.ensureProperPositioning();
    }

    ensureProperPositioning() {
        // Ensure widget is properly positioned
        const widget = document.querySelector('.accessibility-widget');
        if (widget) {
            // Force proper positioning
            widget.style.position = 'fixed';
            widget.style.bottom = '20px';
            widget.style.right = '20px';
            widget.style.zIndex = '99999';
            widget.style.transform = 'none';
            widget.style.margin = '0';
            widget.style.padding = '0';
        }

        // Check positioning every second for the first 5 seconds
        let checks = 0;
        const positionCheck = setInterval(() => {
            checks++;
            if (checks > 5) {
                clearInterval(positionCheck);
                return;
            }

            if (widget) {
                const computedStyle = window.getComputedStyle(widget);
                if (computedStyle.position !== 'fixed') {
                    widget.style.position = 'fixed';
                    widget.style.bottom = '20px';
                    widget.style.right = '20px';
                }
            }
        }, 1000);
    }

    bindEvents() {
        // Toggle panel
        document.getElementById('accessibilityToggle').addEventListener('click', (e) => {
            e.stopPropagation();
            this.togglePanel();
        });

        // Close panel when clicking outside - tapi jangan tutup saat klik di dalam panel
        document.addEventListener('click', (e) => {
            if (!e.target.closest('.accessibility-widget')) {
                this.closePanel();
            }
        });

        // Prevent panel from closing when clicking inside it
        document.getElementById('accessibilityPanel').addEventListener('click', (e) => {
            e.stopPropagation();
        });

        // Font size range
        const fontSizeRange = document.getElementById('fontSizeRange');
        if (fontSizeRange) {
            fontSizeRange.addEventListener('input', (e) => {
                e.stopPropagation();
                this.setFontSize(e.target.value);
            });
        }

        // Toggle switches
        this.bindToggle('contrastToggle', 'highContrast');
        this.bindToggle('largeTextToggle', 'largeText');
        this.bindToggle('bigCursorToggle', 'bigCursor');
        this.bindToggle('readingGuideToggle', 'readingGuide');
        this.bindToggle('dyslexiaFontToggle', 'dyslexiaFont');
        this.bindToggle('grayscaleToggle', 'grayscale');
        this.bindToggle('invertColorsToggle', 'invertColors');

        // Reset button
        const resetButton = document.getElementById('resetButton');
        if (resetButton) {
            resetButton.addEventListener('click', (e) => {
                e.stopPropagation();
                this.resetSettings();
            });
        }
    }

    bindToggle(toggleId, settingKey) {
        const toggleElement = document.getElementById(toggleId);
        if (toggleElement) {
            toggleElement.addEventListener('click', (e) => {
                e.stopPropagation(); // Prevent event bubbling
                this.toggleSetting(settingKey);
            });
        }
    }

    togglePanel() {
        this.isOpen = !this.isOpen;
        const panel = document.getElementById('accessibilityPanel');

        if (this.isOpen) {
            panel.classList.add('active');
        } else {
            panel.classList.remove('active');
        }
    }

    closePanel() {
        this.isOpen = false;
        document.getElementById('accessibilityPanel').classList.remove('active');
    }

    setFontSize(value) {
        this.settings.fontSize = parseInt(value);
        document.body.style.fontSize = `${value}%`;
        const fontSizeValue = document.getElementById('fontSizeValue');
        if (fontSizeValue) {
            fontSizeValue.textContent = `${value}%`;
        }
        this.saveSettings();

        // Show feedback
        this.showFeedback(`Ukuran teks diubah ke ${value}%`);
    }

    toggleSetting(settingKey) {
        this.settings[settingKey] = !this.settings[settingKey];
        this.applySetting(settingKey);
        this.updateToggleUI(settingKey);
        this.saveSettings();

        // Provide visual feedback
        this.showFeedback(`${this.getSettingName(settingKey)} ${this.settings[settingKey] ? 'diaktifkan' : 'dinonaktifkan'}`);
    }

    getSettingName(settingKey) {
        const names = {
            'highContrast': 'Kontras Tinggi',
            'largeText': 'Teks Besar',
            'bigCursor': 'Kursor Besar',
            'readingGuide': 'Panduan Baca',
            'dyslexiaFont': 'Font Disleksia',
            'grayscale': 'Mode Grayscale',
            'invertColors': 'Balik Warna'
        };
        return names[settingKey] || settingKey;
    }

    showFeedback(message) {
        // Create or update feedback element
        let feedback = document.getElementById('accessibility-feedback');
        if (!feedback) {
            feedback = document.createElement('div');
            feedback.id = 'accessibility-feedback';
            feedback.style.cssText = `
                position: fixed;
                top: 20px;
                right: 20px;
                background: #28a745;
                color: white;
                padding: 10px 15px;
                border-radius: 5px;
                font-size: 14px;
                z-index: 10000;
                opacity: 0;
                transition: opacity 0.3s ease;
                pointer-events: none;
            `;
            document.body.appendChild(feedback);
        }

        feedback.textContent = message;
        feedback.style.opacity = '1';

        // Hide after 2 seconds
        setTimeout(() => {
            feedback.style.opacity = '0';
        }, 2000);
    }

    applySetting(settingKey) {
        const isActive = this.settings[settingKey];

        switch (settingKey) {
            case 'highContrast':
                document.body.classList.toggle('high-contrast', isActive);
                break;
            case 'largeText':
                document.body.classList.toggle('large-text', isActive);
                break;
            case 'bigCursor':
                document.body.classList.toggle('big-cursor', isActive);
                break;
            case 'readingGuide':
                this.toggleReadingGuide(isActive);
                break;
            case 'dyslexiaFont':
                document.body.classList.toggle('dyslexia-font', isActive);
                break;
            case 'grayscale':
                document.body.classList.toggle('grayscale', isActive);
                break;
            case 'invertColors':
                document.body.classList.toggle('invert-colors', isActive);
                break;
        }
    }

    updateToggleUI(settingKey) {
        const toggleMap = {
            'highContrast': 'contrastToggle',
            'largeText': 'largeTextToggle',
            'bigCursor': 'bigCursorToggle',
            'readingGuide': 'readingGuideToggle',
            'dyslexiaFont': 'dyslexiaFontToggle',
            'grayscale': 'grayscaleToggle',
            'invertColors': 'invertColorsToggle'
        };

        const toggleElement = document.getElementById(toggleMap[settingKey]);
        if (toggleElement) {
            toggleElement.classList.toggle('active', this.settings[settingKey]);
        }
    }

    setupReadingGuide() {
        document.addEventListener('mousemove', (e) => {
            if (this.settings.readingGuide) {
                const guide = document.getElementById('readingGuide');
                if (guide) {
                    guide.style.top = `${e.clientY}px`;
                }
            }
        });
    }

    toggleReadingGuide(isActive) {
        const guide = document.getElementById('readingGuide');
        if (guide) {
            guide.style.display = isActive ? 'block' : 'none';
        }
    }

    resetSettings() {
        // Reset all settings to default
        this.settings = {
            fontSize: 100,
            highContrast: false,
            largeText: false,
            bigCursor: false,
            readingGuide: false,
            dyslexiaFont: false,
            grayscale: false,
            invertColors: false
        };

        // Apply defaults
        this.applyAllSettings();
        this.updateAllToggles();

        // Reset font size
        const fontSizeRange = document.getElementById('fontSizeRange');
        const fontSizeValue = document.getElementById('fontSizeValue');

        if (fontSizeRange) fontSizeRange.value = 100;
        if (fontSizeValue) fontSizeValue.textContent = '100%';
        document.body.style.fontSize = '100%';

        this.saveSettings();

        // Show feedback and keep panel open
        this.showFeedback('Semua pengaturan direset ke default');
    }

    applyAllSettings() {
        Object.keys(this.settings).forEach(key => {
            if (key !== 'fontSize') {
                this.applySetting(key);
            }
        });
    }

    updateAllToggles() {
        Object.keys(this.settings).forEach(key => {
            if (key !== 'fontSize') {
                this.updateToggleUI(key);
            }
        });
    }

    saveSettings() {
        try {
            localStorage.setItem('accessibilitySettings', JSON.stringify(this.settings));
        } catch (e) {
            console.warn('Could not save accessibility settings:', e);
        }
    }

    loadSettings() {
        try {
            const saved = localStorage.getItem('accessibilitySettings');
            if (saved) {
                this.settings = { ...this.settings, ...JSON.parse(saved) };
            }

            // Apply loaded settings
            this.applyAllSettings();
            this.updateAllToggles();

            // Set font size
            const fontSizeRange = document.getElementById('fontSizeRange');
            const fontSizeValue = document.getElementById('fontSizeValue');

            if (fontSizeRange) fontSizeRange.value = this.settings.fontSize;
            if (fontSizeValue) fontSizeValue.textContent = `${this.settings.fontSize}%`;
            document.body.style.fontSize = `${this.settings.fontSize}%`;
        } catch (e) {
            console.warn('Could not load accessibility settings:', e);
        }
    }
}

// Initialize the accessibility widget when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new AccessibilityWidget();
});