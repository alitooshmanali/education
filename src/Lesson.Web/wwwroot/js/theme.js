window.lessonTheme = {
  _storageKey: 'lesson.theme',

  getState: (defaultMode, offsetHours) => {
    const safeOffset = Math.max(1, Math.min(12, Number(offsetHours) || 6));
    const now = Date.now();

    const fallback = defaultMode === 'dark' || defaultMode === 'light' || defaultMode === 'system'
      ? defaultMode
      : 'system';

    const read = () => {
      try {
        const raw = localStorage.getItem(window.lessonTheme._storageKey);
        if (!raw) {
          return null;
        }

        const parsed = JSON.parse(raw);
        const updatedAt = Number(parsed?.updatedAt);
        if (!parsed?.preference || !parsed.updatedAt || Number.isNaN(updatedAt)) {
          return null;
        }

        const isFresh = now - updatedAt < safeOffset * 60 * 60 * 1000;
        if (!isFresh) {
          localStorage.removeItem(window.lessonTheme._storageKey);
          return null;
        }

        const preference = String(parsed.preference || '').toLowerCase();
        if (preference !== 'light' && preference !== 'dark' && preference !== 'system') {
          return null;
        }

        return { preference, isDark: window.lessonTheme._isDark(preference) };
      } catch {
        return null;
      }
    };

    return read() || { preference: fallback, isDark: window.lessonTheme._isDark(fallback) };
  },

  persist: (preference) => {
    try {
      const value = String(preference || '').toLowerCase();
      if (value !== 'light' && value !== 'dark' && value !== 'system') {
        return;
      }

      localStorage.setItem(window.lessonTheme._storageKey, JSON.stringify({
        preference: value,
        updatedAt: Date.now(),
      }));
    } catch {
      // ignore
    }
  },

  apply: (preference) => {
    const selected = String(preference || 'system').toLowerCase();
    const effective = selected === 'light' || selected === 'dark' || selected === 'system'
      ? selected
      : 'system';

    const isDark = window.lessonTheme._isDark(effective);
    document.documentElement.classList.toggle('theme-dark', isDark);
    return isDark;
  },

  _isDark: (preference) => {
    const value = String(preference || 'system').toLowerCase();
    if (value === 'dark') {
      return true;
    }

    if (value === 'light') {
      return false;
    }

    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
  }
};

(() => {
  try {
    const state = window.lessonTheme.getState('system', 6);
    window.lessonTheme.apply(state.preference);
  } catch {
    // ignore
  }
})();
