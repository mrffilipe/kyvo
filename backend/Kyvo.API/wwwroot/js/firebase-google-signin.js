(function () {
    const form = document.getElementById('external-login-form');
    const aliasInput = document.getElementById('external-provider-alias');
    const tokenInput = document.getElementById('external-id-token');

    if (!form || !aliasInput || !tokenInput) {
        return;
    }

    function ensureApp(alias, apiKey, projectId, authDomain) {
        const appName = 'idp-' + alias;
        try {
            return firebase.app(appName);
        } catch (_) {
            return firebase.initializeApp({ apiKey, projectId, authDomain }, appName);
        }
    }

    function submitExternalSignIn(alias, idToken) {
        aliasInput.value = alias;
        tokenInput.value = idToken;
        form.submit();
    }

    function handleFirebaseAuthError(err, btn) {
        console.error(err);
        const code = err && err.code ? err.code : '';
        if (code === 'auth/popup-closed-by-user') {
            btn.disabled = false;
            return;
        }
        if (code === 'auth/popup-blocked') {
            alert('O navegador bloqueou a janela de login. Permita popups para este site e tente novamente.');
            btn.disabled = false;
            return;
        }
        alert('Não foi possível concluir o login com Google. Verifique se o provedor Google está habilitado no Firebase e se o authDomain está correto.');
        btn.disabled = false;
    }

    document.querySelectorAll('.firebase-login-btn').forEach(function (btn) {
        const alias = btn.getAttribute('data-alias');
        const apiKey = btn.getAttribute('data-api-key');
        const projectId = btn.getAttribute('data-project-id');
        const authDomain = btn.getAttribute('data-auth-domain');

        ensureApp(alias, apiKey, projectId, authDomain);

        btn.addEventListener('click', async function () {
            btn.disabled = true;
            try {
                const app = ensureApp(alias, apiKey, projectId, authDomain);
                const auth = firebase.auth(app);
                const provider = new firebase.auth.GoogleAuthProvider();
                const result = await auth.signInWithPopup(provider);
                if (result && result.user) {
                    const idToken = await result.user.getIdToken();
                    submitExternalSignIn(alias, idToken);
                } else {
                    btn.disabled = false;
                }
            } catch (err) {
                handleFirebaseAuthError(err, btn);
            }
        });
    });
})();
