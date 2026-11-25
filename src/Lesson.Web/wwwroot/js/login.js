(function () {
  if (window.asatirLogin) return;
  window.asatirLogin = {
    initialize: function () {
      var form = document.querySelector('[data-asatir-login]');
      if (!form || form.dataset.bound === 'true') return;
      form.dataset.bound = 'true';
      form.addEventListener('submit', async function (event) {
        event.preventDefault();
        var button = form.querySelector('.auth-submit');
        var label = form.querySelector('.auth-submit__label');
        var status = form.querySelector('[data-login-status]');
        var username = form.querySelector('[name="username"]');
        var password = form.querySelector('[name="password"]');
        if (!username.value.trim() || !password.value) {
          status.textContent = 'نام کاربری و گذرواژه را وارد کنید.';
          (!username.value.trim() ? username : password).focus();
          return;
        }
        if (button.disabled) return;
        button.disabled = true;
        button.classList.add('is-loading');
        button.setAttribute('aria-busy', 'true');
        label.textContent = 'در حال ورود به پلتفرم…';
        status.textContent = 'در حال بررسی اطلاعات و آماده‌سازی فضای کاربری';
        try {
          var response = await fetch(form.action, { method: 'POST', body: new FormData(form), credentials: 'same-origin', redirect: 'follow' });
          if (!response.ok) throw new Error('در ارتباط با سرور خطایی رخ داد.');
          var destination = new URL(response.url || window.location.href, window.location.origin);
          window.location.assign(destination.href);
        } catch (error) {
          status.textContent = error && error.message ? error.message : 'ورود انجام نشد؛ دوباره تلاش کنید.';
          button.disabled = false;
          button.classList.remove('is-loading');
          button.removeAttribute('aria-busy');
          label.textContent = 'ورود';
        }
      });
    }
  };
})();
