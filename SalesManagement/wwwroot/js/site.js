// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// ---- AI chat bubble ----
(function () {
    var fab = document.getElementById('aiChatFab');
    var window_ = document.getElementById('aiChatWindow');
    var closeBtn = document.getElementById('aiChatClose');
    var form = document.getElementById('aiChatForm');
    var messages = document.getElementById('aiChatMessages');
    var question = document.getElementById('aiChatQuestion');

    if (!fab || !window_ || !closeBtn || !form || !messages || !question) {
        return;
    }

    function open() {
        window_.classList.add('open');
        window_.setAttribute('aria-hidden', 'false');
        question.focus();
    }

    function close() {
        window_.classList.remove('open');
        window_.setAttribute('aria-hidden', 'true');
    }

    fab.addEventListener('click', function () {
        if (window_.classList.contains('open')) {
            close();
        } else {
            open();
        }
    });

    closeBtn.addEventListener('click', close);

    function addMessage(role, text) {
        var div = document.createElement('div');
        div.className = 'msg ' + role;
        div.textContent = text;
        messages.appendChild(div);
        messages.scrollTop = messages.scrollHeight;
        return div;
    }

    form.addEventListener('submit', function (e) {
        e.preventDefault();

        var text = question.value.trim();
        if (!text) {
            return;
        }

        addMessage('user', text);
        question.value = '';

        var token = form.querySelector('input[name="__RequestVerificationToken"]');
        var body = new URLSearchParams();
        body.append('question', text);
        if (token) {
            body.append('__RequestVerificationToken', token.value);
        }

        var loading = addMessage('model', 'Đang trả lời...');

        fetch('/Ai/Ask', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: body.toString()
        })
            .then(function (resp) {
                return resp.json().catch(function () {
                    return { success: false, answer: 'Không thể đọc phản hồi từ máy chủ.' };
                });
            })
            .then(function (data) {
                loading.textContent = data.answer || 'Không nhận được câu trả lời.';
            })
            .catch(function () {
                loading.textContent = 'Lỗi kết nối. Vui lòng thử lại.';
            })
            .then(function () {
                messages.scrollTop = messages.scrollHeight;
            });
    });
})();
