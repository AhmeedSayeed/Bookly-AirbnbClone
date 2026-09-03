const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/notifications")
    .withAutomaticReconnect()
    .build();

let unreadCount = 0;

function resolveNotificationText(notification) {
    if (notification.messageKey && window.booklyNotificationTemplates) {
        const template = window.booklyNotificationTemplates[notification.messageKey];

        if (template) {
            let text = template;

            if (notification.messageArgsJson) {
                try {
                    const args = JSON.parse(notification.messageArgsJson);

                    args.forEach((arg, index) => {
                        text = text.replace(
                            new RegExp("\\{" + index + "\\}", "g"),
                            arg ?? ""
                        );
                    });
                } catch {
                    // Keep the template unchanged if args cannot be parsed
                }
            }

            return text;
        }
    }

    return notification.legacyMessage || "";
}

function getLocalizedViewText() {
    return window.booklyI18n?.view || "View";
}

async function initNotifications() {
    const res = await fetch("/Notifications/UnreadCount");
    unreadCount = await res.json();
    updateBadge();

    connection.on("ReceiveNotification", (notification) => {
        unreadCount++;
        updateBadge();
        showToast(notification);
    });

    await connection.start();
}

function updateBadge() {
    const badge = document.getElementById("notificationBadge");
    badge.style.display = unreadCount > 0 ? "flex" : "none";
    badge.textContent = unreadCount > 9 ? "9+" : unreadCount;
}

function showToast(notification) {
    const container = document.getElementById("toastContainer");
    const toast = document.createElement("div");
    toast.className = "bookly-toast";

    const message = resolveNotificationText(notification);
    const viewText = getLocalizedViewText();

    toast.innerHTML = `
        <span class="bookly-toast__message">${message}</span>
        ${notification.link ? `<a href="${notification.link}" class="bookly-toast__link">${viewText}</a>` : ""}
    `;

    container.appendChild(toast);

    requestAnimationFrame(() => toast.classList.add("bookly-toast--visible"));

    setTimeout(() => {
        toast.classList.remove("bookly-toast--visible");
        toast.addEventListener("transitionend", () => toast.remove(), { once: true });
    }, 5000);
}

document.addEventListener("DOMContentLoaded", initNotifications);

function toggleNotifications(event) {
    event.stopPropagation();
    const dropdown = document.getElementById('notificationDropdown');

    if (dropdown.classList.contains('show')) {
        dropdown.classList.remove('show');
    } else {
        dropdown.classList.add('show');
        loadDropdownNotifications();
    }
}

window.addEventListener('click', function (e) {
    const dropdown = document.getElementById('notificationDropdown');
    const bell = document.getElementById('notificationBellToggle');
    if (dropdown && bell && !dropdown.contains(e.target) && !bell.contains(e.target)) {
        dropdown.classList.remove('show');
    }
});

function loadDropdownNotifications() {
    const container = document.getElementById('notificationListBody');
    container.innerHTML = `<div style="padding: 20px; text-align: center; color: gray; font-size: 13px;">Loading...</div>`;

    fetch('/Notifications/GetDropdownList')
        .then(response => response.json())
        .then(data => {
            if (!data || data.length === 0) {
                container.innerHTML = `<div style="padding: 20px; text-align: center; color: gray; font-size: 13px;">No notifications found.</div>`;
                return;
            }

            let html = '';
            data.forEach(n => {
                let unreadClass = n.isRead ? '' : 'unread';
                let link = n.link ? n.link : 'javascript:void(0);';

                html += `
                    <a href="${link}" onclick="markAsReadAndNavigate(event, ${n.id}, '${n.link || ''}')" class="notification-item-pop ${unreadClass}">
                        <p style="margin: 0 0 4px 0; font-size: 13px; color: var(--color-navy); font-weight: ${n.isRead ? '400' : '600'}; line-height: 1.4;">
                            ${n.message}
                        </p>
                        <span style="font-size: 11px; color: var(--color-text-muted);">
                            ${new Date(n.createdAt).toLocaleDateString()}
                        </span>
                    </a>
                `;
            });
            container.innerHTML = html;
        })
        .catch(err => {
            container.innerHTML = `<div style="padding: 20px; text-align: center; color: red; font-size: 13px;">Failed to load.</div>`;
        });
}

function markAsReadAndNavigate(event, id, link) {
    event.preventDefault();

    fetch(`/Notifications/MarkAsReadAjax?id=${id}`, {
        method: 'POST',
        headers: {
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        }
    }).finally(() => {
        if (link && link !== 'javascript:void(0);') {
            window.location.href = link;
        } else {
            loadDropdownNotifications();
        }
    });
}

// ----------------------------------------------------
// Chat Notifications (Integrated using existing Toast)
// ----------------------------------------------------
const chatConnection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/chat")
    .build();

chatConnection.on("ReceiveChatNotification", function (notification) {
    // 1. Update the bell counter correctly once
    unreadCount++;
    updateBadge();

    // 2. Show the toast using the exact same style as system notifications
    showToast({
        legacyMessage: `New message from ${notification.senderName}: ${notification.messageSnippet}`,
        link: `/Chat/Inbox?conversationId=${notification.conversationId}`
    });
});

chatConnection.start().catch(function (err) {
    console.error("SignalR Chat Connection Error: ", err.toString());
});